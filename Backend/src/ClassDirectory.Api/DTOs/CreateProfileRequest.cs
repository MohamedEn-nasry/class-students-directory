using System.Text.Json.Serialization;

namespace ClassDirectory.Api.DTOs;

/// <summary>POST /api/profiles request body.</summary>
public sealed class CreateProfileRequest
{
    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonPropertyName("githubUrl")]
    public string? GithubUrl { get; set; }

    [JsonPropertyName("linkedinUrl")]
    public string? LinkedinUrl { get; set; }

    [JsonPropertyName("track")]
    public string? Track { get; set; }

    /// <summary>
    /// Honeypot field. The frontend never renders it; leave it empty.
    /// Any non-empty value is treated as a bot and silently dropped.
    /// </summary>
    [JsonPropertyName("website")]
    public string? Website { get; set; }

    /// <summary>Cloudflare Turnstile token. Required only when Turnstile:Enabled=true.</summary>
    [JsonPropertyName("turnstileToken")]
    public string? TurnstileToken { get; set; }
}
