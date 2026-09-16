using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Takwene.Application.Interfaces;
using Takwene.Domain.Entities;
using Takwene.Domain.Enums;

namespace Takwene.Infrastructure.Persistence;

public class TakweneDbContext : DbContext, IApplicationDbContext
{
    public TakweneDbContext(DbContextOptions<TakweneDbContext> options) : base(options)
    {
    }

    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<Dsp> Dsps => Set<Dsp>();
    public DbSet<TrackDistribution> TrackDistributions => Set<TrackDistribution>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Artist configuration
        modelBuilder.Entity<Artist>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Name).IsRequired().HasMaxLength(150);
            entity.Property(a => a.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(a => a.Email).IsUnique();
            entity.Property(a => a.Country).IsRequired().HasMaxLength(100);

            entity.HasMany(a => a.Tracks)
                .WithOne(t => t.Artist)
                .HasForeignKey(t => t.ArtistId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Track configuration
        modelBuilder.Entity<Track>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
            entity.Property(t => t.Isrc).IsRequired().HasMaxLength(20);
            entity.HasIndex(t => t.Isrc).IsUnique();
            entity.Property(t => t.Genre).IsRequired().HasMaxLength(80);
            entity.Property(t => t.ReleaseDate).IsRequired();

            entity.Property(t => t.Status)
                .HasConversion(new EnumToStringConverter<TrackStatus>())
                .HasMaxLength(30)
                .IsRequired();

            entity.HasMany(t => t.Distributions)
                .WithOne(d => d.Track)
                .HasForeignKey(d => d.TrackId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DSP configuration
        modelBuilder.Entity<Dsp>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(d => d.Name).IsUnique();

            entity.HasMany(d => d.Distributions)
                .WithOne(dist => dist.Dsp)
                .HasForeignKey(dist => dist.DspId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // TrackDistribution configuration
        modelBuilder.Entity<TrackDistribution>(entity =>
        {
            entity.HasKey(td => td.Id);

            entity.Property(td => td.Status)
                .HasConversion(new EnumToStringConverter<DistributionStatus>())
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(td => td.SubmittedAt).IsRequired();
            entity.Property(td => td.RejectionReason).HasMaxLength(500);
            entity.Property(td => td.ReviewedAt);

            // Prevent duplicate distribution rows for the same Track and DSP
            entity.HasIndex(td => new { td.TrackId, td.DspId }).IsUnique();
        });
    }
}
