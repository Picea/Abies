using System.Buffers;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Picea.Abies.Conduit.Api.Authentication;

namespace Picea.Abies.Conduit.Api.Infrastructure;

/// <summary>
/// In-process idempotency for HTTP requests keyed by Idempotency-Key.
/// </summary>
/// <remarks>
/// This implementation is intentionally local to the API process for this phase.
/// A distributed cache-backed implementation can replace this class later without
/// changing endpoint code.
/// </remarks>
public sealed class RequestIdempotencyStore
{
    public const string HeaderName = "Idempotency-Key";
    public const string ReplayHeaderName = "X-Idempotency-Replayed";
    private const string AnonymousClientIdCookie = "abies-idempotency-client";
    private const int MaxStoredResponses = 2048;
    private static readonly TimeSpan ResponseTtl = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, StoredResponse> _responses = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<string> _responseOrder = new();

    public async Task<IResult> ExecuteAsync(
        HttpContext context,
        Func<CancellationToken, Task<IResult>> execute,
        string? payloadFingerprintInput = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetKey(context.Request, out var idempotencyKey))
            return await execute(cancellationToken).ConfigureAwait(false);

        var anonymousClientId = GetOrCreateAnonymousClientId(context);
        var scope = BuildScope(context, idempotencyKey, anonymousClientId);
        var fingerprint = string.IsNullOrWhiteSpace(payloadFingerprintInput)
            ? await ComputeRequestFingerprint(context.Request, cancellationToken).ConfigureAwait(false)
            : ComputePayloadFingerprint(payloadFingerprintInput);

        var gate = _locks.GetOrAdd(scope, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_responses.TryGetValue(scope, out var stored))
            {
                if (stored.ExpiresAtUtc <= DateTimeOffset.UtcNow)
                {
                    _responses.TryRemove(scope, out _);
                }
                else
                {
                if (!string.Equals(stored.Fingerprint, fingerprint, StringComparison.Ordinal))
                    return ApiErrors.Validation(
                        "Idempotency key cannot be reused with a different request payload.");

                context.Response.Headers[ReplayHeaderName] = "true";
                return stored.Response;
                }
            }

            var response = await execute(cancellationToken).ConfigureAwait(false);

            // Only cache successful outcomes so transient failures can be retried normally.
            if (response is IStatusCodeHttpResult { StatusCode: >= 200 and < 300 })
            {
                _responses[scope] = new StoredResponse(
                    fingerprint,
                    response,
                    DateTimeOffset.UtcNow.Add(ResponseTtl));
                _responseOrder.Enqueue(scope);
                TrimResponseCache();
            }

            return response;
        }
        finally
        {
            gate.Release();

            // Keep lock dictionary bounded by evicting only idle lock entries.
            if (!_responses.ContainsKey(scope)
                && gate.CurrentCount == 1
                && _locks.TryGetValue(scope, out var existing)
                && ReferenceEquals(existing, gate))
            {
                _locks.TryRemove(scope, out _);
            }
        }
    }

    private static bool TryGetKey(HttpRequest request, out string key)
    {
        key = string.Empty;

        if (!request.Headers.TryGetValue(HeaderName, out var headerValue))
            return false;

        var candidate = headerValue.ToString().Trim();
        if (string.IsNullOrWhiteSpace(candidate))
            return false;

        key = candidate;
        return true;
    }

    private static string BuildScope(HttpContext context, string idempotencyKey, string? anonymousClientId)
    {
        var userId = JwtTokenService.GetUserId(context.User);
        var actorScope = userId?.ToString() ?? BuildAnonymousScope(context.Request, anonymousClientId);
        var path = context.Request.Path.Value ?? string.Empty;

        return $"{context.Request.Method}:{path}:{actorScope}:{idempotencyKey}";
    }

    private static string BuildAnonymousScope(HttpRequest request, string? anonymousClientId)
    {
        if (!string.IsNullOrWhiteSpace(anonymousClientId))
            return $"anon:{anonymousClientId}";

        var ip = request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "no-ip";
        var userAgent = request.Headers.UserAgent.ToString();
        var userAgentHash = string.IsNullOrWhiteSpace(userAgent)
            ? "no-ua"
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(userAgent)))[..16];

        return $"anon:{ip}:{userAgentHash}";
    }

    private static string? GetOrCreateAnonymousClientId(HttpContext context)
    {
        if (JwtTokenService.GetUserId(context.User) is not null)
            return null;

        if (context.Request.Cookies.TryGetValue(AnonymousClientIdCookie, out var existing)
            && !string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var generated = Guid.NewGuid().ToString("N");
        context.Response.Cookies.Append(AnonymousClientIdCookie, generated, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Secure = context.Request.IsHttps,
            MaxAge = TimeSpan.FromDays(7)
        });

        return generated;
    }

    private static async Task<string> ComputeRequestFingerprint(HttpRequest request, CancellationToken cancellationToken)
    {
        request.EnableBuffering();

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendString(hash, request.Path.Value);
        AppendString(hash, request.QueryString.Value);

        if (request.Body.CanRead)
        {
            request.Body.Position = 0;

            var buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
            try
            {
                while (true)
                {
                    var bytesRead = await request.Body
                        .ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)
                        .ConfigureAwait(false);

                    if (bytesRead == 0)
                        break;

                    hash.AppendData(buffer, 0, bytesRead);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                request.Body.Position = 0;
            }
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static void AppendString(IncrementalHash hash, string? value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        hash.AppendData(Encoding.UTF8.GetBytes(value));
    }

    private static string ComputePayloadFingerprint(string payload)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private void TrimResponseCache()
    {
        var now = DateTimeOffset.UtcNow;

        while (_responseOrder.TryPeek(out var scope)
               && _responses.TryGetValue(scope, out var stored)
               && stored.ExpiresAtUtc <= now)
        {
            _responseOrder.TryDequeue(out _);
            _responses.TryRemove(scope, out _);
            _locks.TryRemove(scope, out _);
        }

        while (_responses.Count > MaxStoredResponses && _responseOrder.TryDequeue(out var oldestScope))
        {
            _responses.TryRemove(oldestScope, out _);
            _locks.TryRemove(oldestScope, out _);
        }
    }

    private sealed record StoredResponse(string Fingerprint, IResult Response, DateTimeOffset ExpiresAtUtc);
}