using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace ClassDirectory.Api.Services;

public sealed class IpHashingOptions
{
    /// <summary>Salt for the SHA-256 IP hash. Set via env var IpHashing__Salt.</summary>
    public string Salt { get; set; } = string.Empty;
}

public interface IIpHashService
{
    /// <summary>Computes salted SHA-256 hex (64 chars) for an IP. Returns null for empty input.</summary>
    string? Hash(string? ip);

    /// <summary>Resolves the client IP, honoring X-Forwarded-For when behind a proxy.</summary>
    string? GetClientIp(HttpContext context);
}

public sealed class IpHashService : IIpHashService
{
    private readonly string _salt;

    public IpHashService(IOptions<IpHashingOptions> options)
    {
        _salt = options.Value.Salt ?? string.Empty;
    }

    public string? Hash(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return null;
        }

        var input = $"{_salt}|{ip.Trim()}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public string? GetClientIp(HttpContext context)
    {
        // Prefer X-Forwarded-For (Azure App Service / proxies set this).
        // ForwardedHeadersMiddleware already populates RemoteIpAddress when
        // configured, but we still check the header explicitly for rate
        // limiting + hashing so behavior is stable behind Supabase/Front Door.
        var forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var first = forwarded.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
            {
                return first;
            }
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }
}
