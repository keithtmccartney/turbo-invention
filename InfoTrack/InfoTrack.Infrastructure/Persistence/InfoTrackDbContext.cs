using InfoTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InfoTrack.Infrastructure.Persistence;

public sealed class InfoTrackDbContext(DbContextOptions<InfoTrackDbContext> options) : DbContext(options)
{
    public DbSet<Location> Locations => Set<Location>();

    public DbSet<Solicitor> Solicitors => Set<Solicitor>();

    public DbSet<ScrapeSnapshot> ScrapeSnapshots => Set<ScrapeSnapshot>();

    public DbSet<SnapshotEntry> SnapshotEntries => Set<SnapshotEntry>();

    public DbSet<InsightSummary> InsightSummaries => Set<InsightSummary>();

    public DbSet<DiscoveryRun> DiscoveryRuns => Set<DiscoveryRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Solicitor>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalKey).HasMaxLength(64).IsRequired();
            entity.Property(x => x.FirmName).HasMaxLength(300).IsRequired();
            entity.HasIndex(x => x.ExternalKey).IsUnique();
            entity.HasOne(x => x.Location).WithMany(x => x.Solicitors).HasForeignKey(x => x.LocationId);
        });

        modelBuilder.Entity<ScrapeSnapshot>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ScrapedAt);
        });

        modelBuilder.Entity<SnapshotEntry>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.ScrapeSnapshot).WithMany(x => x.Entries).HasForeignKey(x => x.ScrapeSnapshotId);
            entity.HasOne(x => x.Solicitor).WithMany().HasForeignKey(x => x.SolicitorId);
            entity.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId);
        });

        modelBuilder.Entity<InsightSummary>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PayloadJson).IsRequired();
        });

        modelBuilder.Entity<DiscoveryRun>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Source).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.HasIndex(x => x.StartedAt);
            entity.HasIndex(x => x.CompletedAt);
        });
    }
}
