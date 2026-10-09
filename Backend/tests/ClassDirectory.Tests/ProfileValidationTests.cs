using ClassDirectory.Api.Services;

namespace ClassDirectory.Tests;

public sealed class ProfileValidationTests
{
    [Fact]
    public void Requires_At_Least_One_Link()
    {
        var ok = ProfileValidator.TryValidate("Ada Lovelace", null, null, "Backend",
            out var result, out var errors);

        Assert.False(ok);
        Assert.Null(result);
        Assert.True(errors.ContainsKey("githubUrl"));
        Assert.True(errors.ContainsKey("linkedinUrl"));
    }

    [Fact]
    public void Accepts_Github_Only_And_Normalizes()
    {
        var ok = ProfileValidator.TryValidate(
            "  Ada Lovelace  ",
            "https://WWW.github.com/Ada/?tab=repos",
            null,
            " Backend ",
            out var result,
            out var errors);

        Assert.True(ok, string.Join("; ", errors.SelectMany(kv => kv.Value)));
        Assert.NotNull(result);
        Assert.Equal("Ada Lovelace", result!.FullName);
        Assert.Equal("https://github.com/Ada", result.GithubUrl);
        Assert.Null(result.LinkedinUrl);
        Assert.Equal("Backend", result.Track);
    }

    [Fact]
    public void Accepts_Linkedin_Only()
    {
        var ok = ProfileValidator.TryValidate(
            "Grace Hopper",
            null,
            "https://www.linkedin.com/in/gracehopper/",
            null,
            out var result,
            out _);

        Assert.True(ok);
        Assert.Equal("https://www.linkedin.com/in/gracehopper", result!.LinkedinUrl);
        Assert.Null(result.Track);
    }

    [Fact]
    public void Rejects_Blank_Name()
    {
        var ok = ProfileValidator.TryValidate("   ", "https://github.com/ada", null, null,
            out _, out var errors);

        Assert.False(ok);
        Assert.True(errors.ContainsKey("fullName"));
    }

    [Fact]
    public void Rejects_Too_Long_Name_And_Track()
    {
        var ok = ProfileValidator.TryValidate(
            new string('a', 61),
            "https://github.com/ada",
            null,
            new string('b', 51),
            out _, out var errors);

        Assert.False(ok);
        Assert.True(errors.ContainsKey("fullName"));
        Assert.True(errors.ContainsKey("track"));
    }

    [Fact]
    public void Empty_Track_Becomes_Null()
    {
        var ok = ProfileValidator.TryValidate("Ada", "https://github.com/ada", null, "   ",
            out var result, out _);

        Assert.True(ok);
        Assert.Null(result!.Track);
    }

    [Fact]
    public void Surfaces_Per_Field_Url_Errors()
    {
        var ok = ProfileValidator.TryValidate("Ada", "http://github.com/ada", "not-a-url", null,
            out _, out var errors);

        Assert.False(ok);
        Assert.True(errors.ContainsKey("githubUrl"));
        Assert.True(errors.ContainsKey("linkedinUrl"));
    }

    [Fact]
    public void Http_Scheme_Rejected_With_Https_Message()
    {
        ProfileValidator.TryValidate("Ada", "http://github.com/ada", null, null,
            out _, out var errors);

        Assert.Contains(errors["githubUrl"], m => m.Contains("https"));
    }
}
