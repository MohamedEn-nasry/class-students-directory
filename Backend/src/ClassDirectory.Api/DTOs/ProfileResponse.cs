namespace ClassDirectory.Api.DTOs;

/// <summary>Public profile shape returned by GET and POST.</summary>
public sealed record ProfileResponse(
    Guid Id,
    string FullName,
    string? GithubUrl,
    string? LinkedinUrl,
    string? Track,
    DateTimeOffset CreatedAt);
