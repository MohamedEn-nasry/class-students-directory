using System.Text.RegularExpressions;

namespace ClassDirectory.Api.Services;

/// <summary>
/// Validates and normalizes GitHub / LinkedIn URLs.
/// The regexes below are the C# counterparts of the PostgreSQL CHECK
/// constraints in <c>supabase/profiles.sql</c> (POSIX equivalents there —
/// <c>(?:…)</c> becomes <c>(…)</c> — semantically identical).
///
/// Normalization (applied before validation):
///  - trim surrounding whitespace
///  - require absolute URI with https scheme only
///  - lowercase host, strip default ports, drop query string and fragment
///  - trim trailing slash (except root)
///  - github: www.github.com → github.com
/// </summary>
public static partial class ProfileUrls
{
    // GitHub profile (or repo) URL. Username follows GitHub rules:
    // 1–39 chars, alphanumeric or hyphen, cannot start/end with hyphen.
    public const string GithubPattern =
        @"^https://github\.com/[A-Za-z0-9](?:[A-Za-z0-9-]{0,37}[A-Za-z0-9])?(?:/[A-Za-z0-9_.\-]+)*/?$";

    // LinkedIn person/company URL, e.g. https://www.linkedin.com/in/jdoe
    // Subdomain (www, de, …) is allowed but must end in linkedin.com.
    public const string LinkedinPattern =
        @"^https://([a-z0-9-]+\.)?linkedin\.com/(in|company)/[A-Za-z0-9_.\-%]+(?:/[A-Za-z0-9_.\-%]*)*/?$";

    public const int GithubMaxLength = 200;
    public const int LinkedinMaxLength = 300;

    [GeneratedRegex(GithubPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex GithubRegex();

    [GeneratedRegex(LinkedinPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex LinkedinRegex();

    public static bool IsValidGithub(string normalizedUrl) => GithubRegex().IsMatch(normalizedUrl);

    public static bool IsValidLinkedin(string normalizedUrl) => LinkedinRegex().IsMatch(normalizedUrl);

    /// <summary>
    /// Try to normalize a raw GitHub URL. Empty input yields null (missing).
    /// </summary>
    public static bool TryNormalizeGithub(string? raw, out string? normalized, out string? error)
        => TryNormalize(raw, GithubMaxLength, "githubUrl", isGithub: true, out normalized, out error);

    /// <summary>
    /// Try to normalize a raw LinkedIn URL. Empty input yields null (missing).
    /// </summary>
    public static bool TryNormalizeLinkedin(string? raw, out string? normalized, out string? error)
        => TryNormalize(raw, LinkedinMaxLength, "linkedinUrl", isGithub: false, out normalized, out error);

    private static bool TryNormalize(
        string? raw,
        int maxLength,
        string field,
        bool isGithub,
        out string? normalized,
        out string? error)
    {
        normalized = null;
        error = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return true; // missing — caller enforces "at least one link"
        }

        var trimmed = raw.Trim();

        if (trimmed.Length > maxLength + 128)
        {
            // Allow some slack for query/fragment that will be stripped,
            // but reject absurdly long input early.
            error = $"{field} is too long (max {maxLength} characters).";
            return false;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            error = $"{field} must be a valid absolute URL.";
            return false;
        }

        if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            error = $"{field} must use https.";
            return false;
        }

        if (!uri.IsDefaultPort)
        {
            error = $"{field} must not specify a custom port.";
            return false;
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            error = $"{field} must not contain credentials.";
            return false;
        }

        var host = uri.Host.ToLowerInvariant();

        if (isGithub)
        {
            if (host is not ("github.com" or "www.github.com"))
            {
                error = $"{field} host must be github.com.";
                return false;
            }

            host = "github.com"; // canonicalize www away for dedup
        }
        else
        {
            if (host != "linkedin.com" && !host.EndsWith(".linkedin.com", StringComparison.Ordinal))
            {
                error = $"{field} host must be linkedin.com.";
                return false;
            }
        }

        // AbsolutePath excludes query + fragment by construction.
        var path = uri.AbsolutePath;
        if (string.IsNullOrEmpty(path))
        {
            path = "/";
        }

        // Trim trailing slash except for root (root will fail regex anyway).
        if (path.Length > 1)
        {
            path = path.TrimEnd('/');
            if (path.Length == 0)
            {
                path = "/";
            }
        }

        var candidate = $"https://{host}{path}";

        if (candidate.Length > maxLength)
        {
            error = $"{field} is too long (max {maxLength} characters).";
            return false;
        }

        var valid = isGithub ? IsValidGithub(candidate) : IsValidLinkedin(candidate);
        if (!valid)
        {
            error = isGithub
                ? $"{field} must look like https://github.com/username."
                : $"{field} must look like https://www.linkedin.com/in/username.";
            return false;
        }

        normalized = candidate;
        return true;
    }
}
