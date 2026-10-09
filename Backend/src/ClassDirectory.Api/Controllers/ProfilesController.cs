using ClassDirectory.Api.DTOs;
using ClassDirectory.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClassDirectory.Api.Controllers;

[ApiController]
[Route("api/profiles")]
[Produces("application/json")]
public sealed class ProfilesController : ControllerBase
{
    private readonly IProfileService _profiles;
    private readonly IIpHashService _ipHash;
    private readonly ILogger<ProfilesController> _logger;

    public ProfilesController(
        IProfileService profiles,
        IIpHashService ipHash,
        ILogger<ProfilesController> logger)
    {
        _profiles = profiles;
        _ipHash = ipHash;
        _logger = logger;
    }

    /// <summary>
    /// List approved profiles, newest first, paged with total count.
    /// </summary>
    /// <param name="search">Substring filter over name + URLs (case-insensitive).</param>
    /// <param name="track">Exact track filter (case-insensitive).</param>
    /// <param name="page">1-based page (default 1).</param>
    /// <param name="pageSize">Items per page, max 100 (default 24).</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProfileResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProfileResponse>>> Get(
        [FromQuery] string? search,
        [FromQuery] string? track,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken ct = default)
    {
        var result = await _profiles.SearchAsync(
            new ProfileListQuery(search, track, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>Add your own profile (no login). At least one link required.</summary>
    /// <remarks>
    /// Sample body:
    ///
    ///     POST /api/profiles
    ///     { "fullName": "Ada Lovelace", "githubUrl": "https://github.com/ada", "track": "Backend" }
    /// </remarks>
    /// <response code="201">Created profile.</response>
    /// <response code="400">Field validation errors.</response>
    /// <response code="409">A URL is already registered.</response>
    /// <response code="429">Rate limit exceeded (5 POSTs / IP / hour).</response>
    [HttpPost]
    [EnableRateLimiting("post-profiles")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProfileResponse>> Post(
        [FromBody] CreateProfileRequest request,
        CancellationToken ct)
    {
        var clientIp = _ipHash.GetClientIp(HttpContext);

        try
        {
            var created = await _profiles.CreateAsync(request, clientIp, ct);
            return CreatedAtAction(nameof(Get), new { }, created);
        }
        catch (ProfileValidationException ex)
        {
            _logger.LogInformation("Profile rejected: {Errors}", ex.Errors);
            var problem = new HttpValidationProblemDetails(
                ex.Errors.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value,
                    StringComparer.OrdinalIgnoreCase))
            {
                Title = "Profile validation failed.",
                Status = StatusCodes.Status400BadRequest,
            };
            return BadRequest(problem);
        }
        catch (DuplicateUrlException ex)
        {
            _logger.LogInformation("Duplicate {Field}: {Url}", ex.Field, ex.Url);
            return Conflict(new ProblemDetails
            {
                Title = "URL already registered.",
                Detail = $"This {TrimField(ex.Field)} is already in the directory.",
                Status = StatusCodes.Status409Conflict,
                Extensions = { ["field"] = ex.Field },
            });
        }
    }

    private static string TrimField(string field)
        => field switch
        {
            "githubUrl" => "GitHub URL",
            "linkedinUrl" => "LinkedIn URL",
            _ => field,
        };
}
