using System.Text.Json.Serialization;

namespace ClassDirectory.Api.Services;

public sealed class TurnstileOptions
{
    public bool Enabled { get; set; }
    public string SecretKey { get; set; } = string.Empty;
    public string VerifyUrl { get; set; } = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
}

public interface ITurnstileService
{
    /// <summary>
    /// Verifies a Cloudflare Turnstile token. Returns true when verification
    /// is disabled via config (open directory, optional bot protection).
    /// </summary>
    Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken ct);
}

public sealed class TurnstileService : ITurnstileService
{
    private readonly TurnstileOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TurnstileService> _logger;

    public TurnstileService(
        Microsoft.Extensions.Options.IOptions<TurnstileOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<TurnstileService> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            _logger.LogError("Turnstile is enabled but no secret key is configured.");
            return false;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("turnstile");
            using var form = new FormUrlEncodedContent(new Dictionary<string, string?>
            {
                ["secret"] = _options.SecretKey,
                ["response"] = token,
                ["remoteip"] = remoteIp,
            });

            using var res = await client.PostAsync(_options.VerifyUrl, form, ct);
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("Turnstile verify HTTP {Status}", (int)res.StatusCode);
                return false;
            }

            var payload = await res.Content.ReadFromJsonAsync<TurnstileVerifyResponse>(cancellationToken: ct);
            return payload?.Success is true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Turnstile verification failed (fail-closed).");
            return false; // fail closed when enabled
        }
    }

    private sealed record TurnstileVerifyResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("error-codes")] string[]? ErrorCodes);
}

/// <summary>Fallback used only in tests — always succeeds.</summary>
public sealed class NoOpTurnstileService : ITurnstileService
{
    public Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken ct)
        => Task.FromResult(true);
}
