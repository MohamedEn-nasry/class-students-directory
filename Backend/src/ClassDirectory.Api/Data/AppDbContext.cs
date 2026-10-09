using ClassDirectory.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClassDirectory.Api.Data;

/// <summary>
/// Database-first DbContext for the pre-existing <c>public.profiles</c> table.
/// No migrations are used to create the table (see supabase/profiles.sql for DDL).
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Profile> Profiles => Set<Profile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Profile>(entity =>
        {
            entity.ToTable("profiles", "public", tb => tb.HasCheckConstraint(
                "ck_profiles_at_least_one_link",
                "github_url IS NOT NULL OR linkedin_url IS NOT NULL"));

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.FullName)
                .HasColumnName("full_name")
                .HasMaxLength(60)
                .IsRequired();

            entity.Property(e => e.GithubUrl)
                .HasColumnName("github_url")
                .HasMaxLength(200);

            entity.Property(e => e.LinkedinUrl)
                .HasColumnName("linkedin_url")
                .HasMaxLength(300);

            entity.Property(e => e.Track)
                .HasColumnName("track")
                .HasMaxLength(50);

            entity.Property(e => e.IsApproved)
                .HasColumnName("is_approved")
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("now()");

            entity.Property(e => e.IpHash)
                .HasColumnName("ip_hash")
                .HasMaxLength(64);

            // Unique constraints already exist in the DB; declaring them here
            // gives EF a truthful model without managing them via migrations.
            entity.HasIndex(e => e.GithubUrl).IsUnique();
            entity.HasIndex(e => e.LinkedinUrl).IsUnique();
        });
    }
}
