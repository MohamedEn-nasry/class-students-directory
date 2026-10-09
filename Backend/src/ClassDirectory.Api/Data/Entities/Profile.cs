using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassDirectory.Api.Data.Entities;

/// <summary>
/// Database-first mapping for the existing table <c>public.profiles</c>.
/// The table is NOT created by EF migrations — it already exists in Supabase.
/// </summary>
[Table("profiles", Schema = "public")]
public sealed class Profile
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("full_name")]
    [MaxLength(60)]
    public string FullName { get; set; } = string.Empty;

    [Column("github_url")]
    [MaxLength(200)]
    public string? GithubUrl { get; set; }

    [Column("linkedin_url")]
    [MaxLength(300)]
    public string? LinkedinUrl { get; set; }

    [Column("track")]
    [MaxLength(50)]
    public string? Track { get; set; }

    [Column("is_approved")]
    public bool IsApproved { get; set; } = true;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Salted SHA-256 hex of the submitter IP. Never stores the raw IP.</summary>
    [Column("ip_hash")]
    [MaxLength(64)]
    public string? IpHash { get; set; }
}
