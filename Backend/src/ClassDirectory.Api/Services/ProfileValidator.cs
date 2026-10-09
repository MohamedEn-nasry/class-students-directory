namespace ClassDirectory.Api.Services;

/// <summary>Normalized, validated profile input ready for persistence.</summary>
public sealed record NormalizedProfile(
    string FullName,
    string? GithubUrl,
    string? LinkedinUrl,
    string? Track);

/// <summary>
/// Validates + normalizes profile input. Pure static logic so it is
/// trivially unit-testable without a database.
/// </summary>
public static class ProfileValidator
{
    public const int FullNameMaxLength = 60;
    public const int TrackMaxLength = 50;

    /// <summary>
    /// Validates raw input. Returns true on success; otherwise fills
    /// <paramref name="errors"/> with per-field messages.
    /// </summary>
    public static bool TryValidate(
        string? fullName,
        string? githubUrl,
        string? linkedinUrl,
        string? track,
        out NormalizedProfile? result,
        out Dictionary<string, string[]> errors)
    {
        errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        result = null;

        var fieldErrors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        void Add(string field, string message)
        {
            if (!fieldErrors.TryGetValue(field, out var list))
            {
                list = new List<string>();
                fieldErrors[field] = list;
            }

            list.Add(message);
        }

        // ---- full name: trim + length check ----
        var name = (fullName ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            Add("fullName", "Full name is required.");
        }
        else if (name.Length > FullNameMaxLength)
        {
            Add("fullName", $"Full name must be at most {FullNameMaxLength} characters.");
        }

        // ---- track: optional, trim, length check, empty → null ----
        string? cleanTrack = null;
        if (!string.IsNullOrWhiteSpace(track))
        {
            cleanTrack = track.Trim();
            if (cleanTrack.Length > TrackMaxLength)
            {
                Add("track", $"Track must be at most {TrackMaxLength} characters.");
            }
            else if (cleanTrack.Length == 0)
            {
                cleanTrack = null;
            }
        }

        // ---- URLs: normalize (https only, lowercase host, strip ?/#, trim /) ----
        if (!ProfileUrls.TryNormalizeGithub(githubUrl, out var cleanGithub, out var githubError))
        {
            Add("githubUrl", githubError!);
        }

        if (!ProfileUrls.TryNormalizeLinkedin(linkedinUrl, out var cleanLinkedin, out var linkedinError))
        {
            Add("linkedinUrl", linkedinError!);
        }

        // ---- at least one link required (DB CHECK mirrors this) ----
        if (cleanGithub is null && cleanLinkedin is null
            && !fieldErrors.ContainsKey("githubUrl")
            && !fieldErrors.ContainsKey("linkedinUrl"))
        {
            Add("githubUrl", "At least one of githubUrl or linkedinUrl is required.");
            Add("linkedinUrl", "At least one of githubUrl or linkedinUrl is required.");
        }

        if (fieldErrors.Count > 0)
        {
            foreach (var kv in fieldErrors)
            {
                errors[kv.Key] = kv.Value.ToArray();
            }

            return false;
        }

        result = new NormalizedProfile(name, cleanGithub, cleanLinkedin, cleanTrack);
        return true;
    }
}
