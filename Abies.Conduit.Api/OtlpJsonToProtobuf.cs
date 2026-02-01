using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;

namespace Abies.Conduit.Api;

/// <summary>
/// Converts OTLP JSON trace data to protobuf binary format.
/// This is needed because Aspire 13.1.0 only accepts application/x-protobuf for OTLP/HTTP.
/// </summary>
public static class OtlpJsonToProtobuf
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    /// <summary>
    /// Converts OTLP JSON trace payload to protobuf binary.
    /// </summary>
    public static byte[] ConvertTracesToProtobuf(string json)
    {
        var jsonDoc = JsonDocument.Parse(json);
        var request = ParseExportTraceServiceRequest(jsonDoc.RootElement);
        return request.ToByteArray();
    }

    private static ExportTraceServiceRequest ParseExportTraceServiceRequest(JsonElement root)
    {
        var request = new ExportTraceServiceRequest();

        if (root.TryGetProperty("resourceSpans", out var resourceSpansElement) && resourceSpansElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var rsElement in resourceSpansElement.EnumerateArray())
            {
                request.ResourceSpans.Add(ParseResourceSpans(rsElement));
            }
        }

        return request;
    }

    private static ResourceSpans ParseResourceSpans(JsonElement element)
    {
        var rs = new ResourceSpans();

        if (element.TryGetProperty("resource", out var resourceElement))
        {
            rs.Resource = ParseResource(resourceElement);
        }

        if (element.TryGetProperty("scopeSpans", out var scopeSpansElement) && scopeSpansElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var ssElement in scopeSpansElement.EnumerateArray())
            {
                rs.ScopeSpans.Add(ParseScopeSpans(ssElement));
            }
        }

        return rs;
    }

    private static Resource ParseResource(JsonElement element)
    {
        var resource = new Resource();

        if (element.TryGetProperty("attributes", out var attributesElement) && attributesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var attrElement in attributesElement.EnumerateArray())
            {
                resource.Attributes.Add(ParseKeyValue(attrElement));
            }
        }

        return resource;
    }

    private static ScopeSpans ParseScopeSpans(JsonElement element)
    {
        var ss = new ScopeSpans();

        if (element.TryGetProperty("scope", out var scopeElement))
        {
            ss.Scope = ParseInstrumentationScope(scopeElement);
        }

        if (element.TryGetProperty("spans", out var spansElement) && spansElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var spanElement in spansElement.EnumerateArray())
            {
                ss.Spans.Add(ParseSpan(spanElement));
            }
        }

        return ss;
    }

    private static InstrumentationScope ParseInstrumentationScope(JsonElement element)
    {
        var scope = new InstrumentationScope();

        if (element.TryGetProperty("name", out var nameElement))
        {
            scope.Name = nameElement.GetString() ?? "";
        }

        if (element.TryGetProperty("version", out var versionElement))
        {
            scope.Version = versionElement.GetString() ?? "";
        }

        return scope;
    }

    private static Span ParseSpan(JsonElement element)
    {
        var span = new Span();

        if (element.TryGetProperty("traceId", out var traceIdElement))
        {
            var traceIdStr = traceIdElement.GetString();
            if (!string.IsNullOrEmpty(traceIdStr))
            {
                span.TraceId = ByteString.CopyFrom(HexToBytes(traceIdStr));
            }
        }

        if (element.TryGetProperty("spanId", out var spanIdElement))
        {
            var spanIdStr = spanIdElement.GetString();
            if (!string.IsNullOrEmpty(spanIdStr))
            {
                span.SpanId = ByteString.CopyFrom(HexToBytes(spanIdStr));
            }
        }

        if (element.TryGetProperty("parentSpanId", out var parentSpanIdElement))
        {
            var parentSpanIdStr = parentSpanIdElement.GetString();
            if (!string.IsNullOrEmpty(parentSpanIdStr))
            {
                span.ParentSpanId = ByteString.CopyFrom(HexToBytes(parentSpanIdStr));
            }
        }

        if (element.TryGetProperty("name", out var nameElement))
        {
            span.Name = nameElement.GetString() ?? "";
        }

        if (element.TryGetProperty("kind", out var kindElement))
        {
            span.Kind = (Span.Types.SpanKind)kindElement.GetInt32();
        }

        if (element.TryGetProperty("startTimeUnixNano", out var startTimeElement))
        {
            span.StartTimeUnixNano = ParseNanoseconds(startTimeElement);
        }

        if (element.TryGetProperty("endTimeUnixNano", out var endTimeElement))
        {
            span.EndTimeUnixNano = ParseNanoseconds(endTimeElement);
        }

        if (element.TryGetProperty("attributes", out var attributesElement) && attributesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var attrElement in attributesElement.EnumerateArray())
            {
                span.Attributes.Add(ParseKeyValue(attrElement));
            }
        }

        if (element.TryGetProperty("status", out var statusElement))
        {
            span.Status = ParseStatus(statusElement);
        }

        if (element.TryGetProperty("events", out var eventsElement) && eventsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var eventElement in eventsElement.EnumerateArray())
            {
                span.Events.Add(ParseSpanEvent(eventElement));
            }
        }

        return span;
    }

    private static KeyValue ParseKeyValue(JsonElement element)
    {
        var kv = new KeyValue();

        if (element.TryGetProperty("key", out var keyElement))
        {
            kv.Key = keyElement.GetString() ?? "";
        }

        if (element.TryGetProperty("value", out var valueElement))
        {
            kv.Value = ParseAnyValue(valueElement);
        }

        return kv;
    }

    private static AnyValue ParseAnyValue(JsonElement element)
    {
        var anyValue = new AnyValue();

        if (element.TryGetProperty("stringValue", out var stringValueElement))
        {
            anyValue.StringValue = stringValueElement.GetString() ?? "";
        }
        else if (element.TryGetProperty("intValue", out var intValueElement))
        {
            anyValue.IntValue = ParseLong(intValueElement);
        }
        else if (element.TryGetProperty("doubleValue", out var doubleValueElement))
        {
            anyValue.DoubleValue = doubleValueElement.GetDouble();
        }
        else if (element.TryGetProperty("boolValue", out var boolValueElement))
        {
            anyValue.BoolValue = boolValueElement.GetBoolean();
        }
        else if (element.TryGetProperty("arrayValue", out var arrayValueElement))
        {
            var arrayValue = new ArrayValue();
            if (arrayValueElement.TryGetProperty("values", out var valuesElement) && valuesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var itemElement in valuesElement.EnumerateArray())
                {
                    arrayValue.Values.Add(ParseAnyValue(itemElement));
                }
            }
            anyValue.ArrayValue = arrayValue;
        }

        return anyValue;
    }

    private static Status ParseStatus(JsonElement element)
    {
        var status = new Status();

        if (element.TryGetProperty("code", out var codeElement))
        {
            status.Code = (Status.Types.StatusCode)codeElement.GetInt32();
        }

        if (element.TryGetProperty("message", out var messageElement))
        {
            status.Message = messageElement.GetString() ?? "";
        }

        return status;
    }

    private static Span.Types.Event ParseSpanEvent(JsonElement element)
    {
        var evt = new Span.Types.Event();

        if (element.TryGetProperty("name", out var nameElement))
        {
            evt.Name = nameElement.GetString() ?? "";
        }

        if (element.TryGetProperty("timeUnixNano", out var timeElement))
        {
            evt.TimeUnixNano = ParseNanoseconds(timeElement);
        }

        if (element.TryGetProperty("attributes", out var attributesElement) && attributesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var attrElement in attributesElement.EnumerateArray())
            {
                evt.Attributes.Add(ParseKeyValue(attrElement));
            }
        }

        return evt;
    }

    private static ulong ParseNanoseconds(JsonElement element)
    {
        // JSON may encode as string (since nanoseconds can exceed JS max safe integer)
        if (element.ValueKind == JsonValueKind.String)
        {
            var str = element.GetString();
            return ulong.TryParse(str, out var val) ? val : 0;
        }
        return element.GetUInt64();
    }

    private static long ParseLong(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            var str = element.GetString();
            return long.TryParse(str, out var val) ? val : 0;
        }
        return element.GetInt64();
    }

    private static byte[] HexToBytes(string hex)
    {
        // Remove any dashes or spaces
        hex = hex.Replace("-", "").Replace(" ", "");
        
        if (hex.Length % 2 != 0)
        {
            // Pad with leading zero if odd length
            hex = "0" + hex;
        }

        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
    }
}
