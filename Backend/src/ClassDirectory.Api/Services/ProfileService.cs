using ClassDirectory.Api.Data;
using ClassDirectory.Api.Data.Entities;
using ClassDirectory.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ClassDirectory.Api.Services;

public sealed record ProfileListQuery(
    string? Search,
    string? Track,
    int Page,
    int PageSize);

public interface IProfileService
{
    Task<PagedResult<ProfileResponse>> SearchAsync(ProfileListQuery query, CancellationToken ct);
    Task<ProfileResponse> CreateAsync(CreateProfileRequest request, string? clientIp, CancellationToken ct);
}

public sealed class ProfileService : IProfileService
{
    private readonly AppDbContext _db;
    private readonly IIpHashService _ipHash;
    private readonly ITurnstileService _turnstile;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        AppDbContext db,
        IIpHashService ipHash,
        ITurnstileService turnstile,
        ILogger<ProfileService> logger)
    {
        _db = db;
        _ipHash = ipHash;
        _turnstile = turnstile;
        _logger = logger;
    }

    public async Task<PagedResult<ProfileResponse>> SearchAsync(ProfileListQuery query, CancellationToken ct)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 24 : query.PageSize;

        IQueryable<Profile> q = _db.Profiles.AsNoTracking().Where(p => p.IsApproved);

        var search = query.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var like = $"%{search.Trim()}%";
            q = q.Where(p =>
                EF.Functions.ILike(p.FullName, like) ||
                (p.GithubUrl != null && EF.Functions.ILike(p.GithubUrl, like)) ||
                (p.LinkedinUrl != null && EF.Functions.ILike(p.LinkedinUrl, like)));
        }

        var track = query.Track?.Trim();
        if (!string.IsNullOrWhiteSpace(track))
        {
            // Exact track match, case-insensitive.
            q = q.Where(p => p.Track != null && EF.Functions.ILike(p.Track, track));
        }

        q = q.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProfileResponse(
                p.Id,
                p.FullName,
                p.GithubUrl,
                p.LinkedinUrl,
                p.Track,
                p.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<ProfileResponse>(items, page, pageSize, total);
    }

    public async Task<ProfileResponse> CreateAsync(CreateProfileRequest request, string? clientIp, CancellationToken ct)
    {
        // ---- honeypot: silently "succeed" without persisting ----
        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            _logger.LogWarning("Honeypot triggered; dropping submission.");
            return HoneypotResponse(request);
        }

        // ---- optional Turnstile (behind config flag) ----
        var human = await _turnstile.VerifyAsync(request.TurnstileToken, clientIp, ct);
        if (!human)
        {
            throw new ProfileValidationException(new Dictionary<string, string[]>
            {
                ["turnstileToken"] = ["Bot verification failed. Please try again."],
            });
        }

        // ---- validate + normalize ----
        if (!ProfileValidator.TryValidate(
                request.FullName,
                request.GithubUrl,
                request.LinkedinUrl,
                request.Track,
                out var clean,
                out var errors))
        {
            throw new ProfileValidationException(errors);
        }

        // ---- pre-check duplicates for precise 409 field errors ----
        if (clean!.GithubUrl is not null &&
            await _db.Profiles.AnyAsync(p => p.GithubUrl == clean.GithubUrl, ct))
        {
            throw new DuplicateUrlException("githubUrl", clean.GithubUrl);
        }

        if (clean.LinkedinUrl is not null &&
            await _db.Profiles.AnyAsync(p => p.LinkedinUrl == clean.LinkedinUrl, ct))
        {
            throw new DuplicateUrlException("linkedinUrl", clean.LinkedinUrl);
        }

        var entity = new Profile
        {
            FullName = clean.FullName,
            GithubUrl = clean.GithubUrl,
            LinkedinUrl = clean.LinkedinUrl,
            Track = clean.Track,
            IsApproved = true, // public directory: auto-approved; moderate reactively
            IpHash = _ipHash.Hash(clientIp),
            // Id + CreatedAt use DB defaults (gen_random_uuid(), now())
        };

        _db.Profiles.Add(entity);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _logger.LogWarning(ex, "Duplicate URL race on insert.");
            // Re-resolve which field collided for an accurate 409.
            var field = await ResolveConflictFieldAsync(clean, ct);
            throw new DuplicateUrlException(field, field == "githubUrl" ? clean.GithubUrl! : clean.LinkedinUrl!);
        }

        // Re-read defaults (id, created_at) generated by the DB.
        await _db.Entry(entity).ReloadAsync(ct);

        _logger.LogInformation("Profile created {ProfileId}", entity.Id);
        return new ProfileResponse(
            entity.Id,
            entity.FullName,
            entity.GithubUrl,
            entity.LinkedinUrl,
            entity.Track,
            entity.CreatedAt);
    }

    private static ProfileResponse HoneypotResponse(CreateProfileRequest request)
    {
        // Return a plausible-but-unsaved payload so bots can't tell they were dropped.
        var name = (request.FullName ?? "Thanks").Trim();
        if (name.Length == 0)
        {
            name = "Thanks";
        }

        return new ProfileResponse(
            Guid.Empty,
            name,
            null,
            null,
            request.Track?.Trim(),
            DateTimeOffset.UtcNow);
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation;

    private async Task<string> ResolveConflictFieldAsync(NormalizedProfile clean, CancellationToken ct)
    {
        if (clean.GithubUrl is not null &&
            await _db.Profiles.AnyAsync(p => p.GithubUrl == clean.GithubUrl, ct))
        {
            return "githubUrl";
        }

        return "linkedinUrl";
    }
}
