using System;
using System.Collections.Generic;
using System.Text;

namespace Abies.Benchmarks;

public static class UrlGenerator
{
    private static readonly Random Random = new Random();
    private static readonly string[] Schemes = { "http", "https" };
    private static readonly string[] TopLevelDomains = { "com", "org", "net", "io", "dev" };
    private static readonly int[] CommonPorts = { 80, 443 };

    public static string GenerateRandomUrl()
    {
        var scheme = GetRandomScheme();
        var host = GenerateRandomHost();
        var port = GetRandomPort();
        var path = GenerateRandomPath();
        var query = GenerateRandomQuery();
        var fragment = GenerateRandomFragment();

        var urlBuilder = new StringBuilder();
        urlBuilder.Append($"{scheme}://{host}");

        if (port != 80 && port != 443)
        {
            urlBuilder.Append($":{port}");
        }

        urlBuilder.Append(path);
        if (!string.IsNullOrEmpty(query))
        {
            urlBuilder.Append($"?{query}");
        }

        if (!string.IsNullOrEmpty(fragment))
        {
            urlBuilder.Append($"#{fragment}");
        }

        return urlBuilder.ToString();
    }

    private static string GetRandomScheme()
    {
        return Schemes[Random.Next(Schemes.Length)];
    }

    private static string GenerateRandomHost()
    {
        var subdomain = GenerateRandomString(3, 10);
        var domain = GenerateRandomString(3, 10);
        var tld = TopLevelDomains[Random.Next(TopLevelDomains.Length)];
        return $"{subdomain}.{domain}.{tld}";
    }

    private static int GetRandomPort()
    {
        if (Random.NextDouble() < 0.5)
        {
            return CommonPorts[Random.Next(CommonPorts.Length)];
        }
        return Random.Next(1024, 65535);
    }

    private static string GenerateRandomPath()
    {
        var pathDepth = Random.Next(1, 5);
        var pathBuilder = new StringBuilder();
        for (int i = 0; i < pathDepth; i++)
        {
            pathBuilder.Append($"/{GenerateRandomString(3, 10)}");
        }
        return pathBuilder.ToString();
    }

    private static string GenerateRandomQuery()
    {
        if (Random.NextDouble() < 0.5)
        {
            return string.Empty;
        }

        var queryParams = Random.Next(1, 5);
        var queryBuilder = new StringBuilder();
        for (int i = 0; i < queryParams; i++)
        {
            if (i > 0)
            {
                queryBuilder.Append("&");
            }
            queryBuilder.Append($"{GenerateRandomString(3, 10)}={GenerateRandomString(3, 10)}");
        }
        return queryBuilder.ToString();
    }

    private static string GenerateRandomFragment()
    {
        if (Random.NextDouble() < 0.5)
        {
            return string.Empty;
        }
        return GenerateRandomString(3, 10);
    }

    private static string GenerateRandomString(int minLength, int maxLength)
    {
        var length = Random.Next(minLength, maxLength + 1);
        var chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var stringChars = new char[length];
        for (int i = 0; i < length; i++)
        {
            stringChars[i] = chars[Random.Next(chars.Length)];
        }
        return new string(stringChars);
    }
}