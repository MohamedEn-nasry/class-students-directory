namespace ClassDirectory.Api.Services;

/// <summary>Raised when input fails field validation. Carries per-field errors for 400 responses.</summary>
public sealed class ProfileValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ProfileValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("Profile validation failed.")
    {
        Errors = errors;
    }
}

/// <summary>Raised when a normalized URL already exists. Carries the conflicting field for 409 responses.</summary>
public sealed class DuplicateUrlException : Exception
{
    public string Field { get; }
    public string Url { get; }

    public DuplicateUrlException(string field, string url)
        : base($"The {field} is already registered.")
    {
        Field = field;
        Url = url;
    }
}
