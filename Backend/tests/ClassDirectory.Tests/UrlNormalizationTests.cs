using ClassDirectory.Api.Services;

namespace ClassDirectory.Tests;

public sealed class UrlNormalizationTests
{
    [Theory]
    [InlineData("https://github.com/octocat", "https://github.com/octocat")]
    [InlineData("https://github.com/octocat/", "https://github.com/octocat")]
    [InlineData("https://www.github.com/octocat", "https://github.com/octocat")]
    [InlineData("https://GITHUB.COM/Octocat", "https://github.com/Octocat")]
    [InlineData("https://github.com/octocat?tab=repositories", "https://github.com/octocat")]
    [InlineData("https://github.com/octocat#readme", "https://github.com/octocat")]
    [InlineData("https://github.com/octocat/Hello-World/", "https://github.com/octocat/Hello-World")]
    [InlineData("  https://github.com/octocat  ", "https://github.com/octocat")]
    public void Github_Normalizes_Valid_Urls(string raw, string expected)
    {
        var ok = ProfileUrls.TryNormalizeGithub(raw, out var normalized, out var error);

        Assert.True(ok, error);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("http://github.com/octocat")]           // https only
    [InlineData("https://gitlab.com/octocat")]          // wrong host
    [InlineData("https://github.com/")]                 // missing username
    [InlineData("https://github.com")]                  // missing path
    [InlineData("https://github.com/-bad")]             // leading hyphen
    [InlineData("https://github.com/bad-/repo")]        // trailing hyphen in username
    [InlineData("not a url")]
    [InlineData("https://github.com/octocat:8080/x")]   // custom port — TryCreate fails or port check
    public void Github_Rejects_Invalid_Urls(string raw)
    {
        var ok = ProfileUrls.TryNormalizeGithub(raw, out var normalized, out var error);

        Assert.False(ok);
        Assert.Null(normalized);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Theory]
    [InlineData("https://www.linkedin.com/in/jdoe", "https://www.linkedin.com/in/jdoe")]
    [InlineData("https://www.linkedin.com/in/jdoe/", "https://www.linkedin.com/in/jdoe")]
    [InlineData("https://LINKEDIN.COM/in/jdoe", "https://linkedin.com/in/jdoe")]
    [InlineData("https://www.linkedin.com/in/jdoe?trk=public_profile", "https://www.linkedin.com/in/jdoe")]
    [InlineData("https://www.linkedin.com/in/jdoe#about", "https://www.linkedin.com/in/jdoe")]
    [InlineData("https://www.linkedin.com/company/acme", "https://www.linkedin.com/company/acme")]
    [InlineData("https://de.linkedin.com/in/jdoe", "https://de.linkedin.com/in/jdoe")]
    public void Linkedin_Normalizes_Valid_Urls(string raw, string expected)
    {
        var ok = ProfileUrls.TryNormalizeLinkedin(raw, out var normalized, out var error);

        Assert.True(ok, error);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("http://www.linkedin.com/in/jdoe")]     // https only
    [InlineData("https://www.linkedin.com/jdoe")]       // missing /in|company
    [InlineData("https://www.linkedin.com/in/")]        // missing slug
    [InlineData("https://www.linkedin.com/")]           // missing section
    [InlineData("https://fake-linkedin.com/in/jdoe")]   // wrong host
    [InlineData("not a url")]
    public void Linkedin_Rejects_Invalid_Urls(string raw)
    {
        var ok = ProfileUrls.TryNormalizeLinkedin(raw, out var normalized, out var error);

        Assert.False(ok);
        Assert.Null(normalized);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void Empty_Input_Yields_Null_Missing()
    {
        Assert.True(ProfileUrls.TryNormalizeGithub(null, out var g, out _));
        Assert.Null(g);
        Assert.True(ProfileUrls.TryNormalizeGithub("   ", out g, out _));
        Assert.Null(g);
        Assert.True(ProfileUrls.TryNormalizeLinkedin("", out var l, out _));
        Assert.Null(l);
    }

    [Fact]
    public void Normalized_Urls_Match_Db_Regexes()
    {
        // These constants must stay in sync with supabase/profiles.sql CHECKs.
        Assert.True(ProfileUrls.TryNormalizeGithub("https://github.com/octocat", out var g, out _));
        Assert.Matches(ProfileUrls.GithubPattern, g!);

        Assert.True(ProfileUrls.TryNormalizeLinkedin("https://www.linkedin.com/in/jdoe", out var l, out _));
        Assert.Matches(ProfileUrls.LinkedinPattern, l!);
    }
}
