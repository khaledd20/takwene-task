using Microsoft.EntityFrameworkCore;
using Takwene.Domain.Entities;
using Takwene.Domain.Enums;

namespace Takwene.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(TakweneDbContext context)
    {
        if (await context.Artists.AnyAsync())
        {
            return; // Already seeded
        }

        // 1. Seed DSPs
        var dspSpotify = new Dsp { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Spotify" };
        var dspApple = new Dsp { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Apple Music" };
        var dspYouTube = new Dsp { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "YouTube" };

        await context.Dsps.AddRangeAsync(dspSpotify, dspApple, dspYouTube);

        // 2. Seed Artists
        var artistWeeknd = new Artist
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = "The Weeknd",
            Email = "theweeknd@xo.records",
            Country = "Canada"
        };

        var artistDua = new Artist
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Name = "Dua Lipa",
            Email = "contact@dualipa.com",
            Country = "United Kingdom"
        };

        var artistDaftPunk = new Artist
        {
            Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            Name = "Daft Punk",
            Email = "robots@daftpunk.fr",
            Country = "France"
        };

        var artistAmrDiab = new Artist
        {
            Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            Name = "Amr Diab",
            Email = "amr@amrdiab.net",
            Country = "Egypt"
        };

        await context.Artists.AddRangeAsync(artistWeeknd, artistDua, artistDaftPunk, artistAmrDiab);

        // 3. Seed Tracks
        var t1 = new Track
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Title = "Blinding Lights",
            ArtistId = artistWeeknd.Id,
            Isrc = "USUM72000572",
            ReleaseDate = new DateTime(2020, 1, 29, 0, 0, 0, DateTimeKind.Utc),
            Genre = "Synthwave",
            Status = TrackStatus.Distributed
        };

        var t2 = new Track
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            Title = "Save Your Tears",
            ArtistId = artistWeeknd.Id,
            Isrc = "USUM72000573",
            ReleaseDate = new DateTime(2020, 8, 9, 0, 0, 0, DateTimeKind.Utc),
            Genre = "Synthpop",
            Status = TrackStatus.Distributed
        };

        var t3 = new Track
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
            Title = "Levitating",
            ArtistId = artistDua.Id,
            Isrc = "GBAYE2000301",
            ReleaseDate = new DateTime(2020, 3, 27, 0, 0, 0, DateTimeKind.Utc),
            Genre = "Disco Pop",
            Status = TrackStatus.Distributed
        };

        var t4 = new Track
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
            Title = "Don't Start Now",
            ArtistId = artistDua.Id,
            Isrc = "GBAYE1901202",
            ReleaseDate = new DateTime(2019, 10, 31, 0, 0, 0, DateTimeKind.Utc),
            Genre = "Nu-Disco",
            Status = TrackStatus.Submitted
        };

        var t5 = new Track
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000005"),
            Title = "One More Time",
            ArtistId = artistDaftPunk.Id,
            Isrc = "FR06X0000210",
            ReleaseDate = new DateTime(2000, 11, 13, 0, 0, 0, DateTimeKind.Utc),
            Genre = "French House",
            Status = TrackStatus.Distributed
        };

        var t6 = new Track
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000006"),
            Title = "Harder, Better, Faster, Stronger",
            ArtistId = artistDaftPunk.Id,
            Isrc = "FR06X0100412",
            ReleaseDate = new DateTime(2001, 10, 13, 0, 0, 0, DateTimeKind.Utc),
            Genre = "Electronic",
            Status = TrackStatus.Submitted
        };

        var t7 = new Track
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000007"),
            Title = "Tamally Maak",
            ArtistId = artistAmrDiab.Id,
            Isrc = "EGG320000001",
            ReleaseDate = new DateTime(2000, 7, 17, 0, 0, 0, DateTimeKind.Utc),
            Genre = "Arabic Pop",
            Status = TrackStatus.Distributed
        };

        var t8 = new Track
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000008"),
            Title = "Nour El Ain",
            ArtistId = artistAmrDiab.Id,
            Isrc = "EGG329600002",
            ReleaseDate = new DateTime(1996, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Genre = "Mediterranean Pop",
            Status = TrackStatus.Submitted
        };

        var t9 = new Track
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000009"),
            Title = "Midnight City Remix (Unreleased)",
            ArtistId = artistDaftPunk.Id,
            Isrc = "FR06X2500999",
            ReleaseDate = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            Genre = "Synthwave",
            Status = TrackStatus.Draft
        };

        var t10 = new Track
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000010"),
            Title = "Houdini (Acoustic Demo)",
            ArtistId = artistDua.Id,
            Isrc = "GBAYE2500115",
            ReleaseDate = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            Genre = "Acoustic Pop",
            Status = TrackStatus.Draft
        };

        await context.Tracks.AddRangeAsync(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);

        // 4. Seed Track Distributions
        var distributions = new List<TrackDistribution>
        {
            // Blinding Lights -> Spotify (Live), Apple (Live), YouTube (Live)
            new() { Id = Guid.NewGuid(), TrackId = t1.Id, DspId = dspSpotify.Id, SubmittedAt = DateTime.UtcNow.AddMonths(-12), Status = DistributionStatus.Live },
            new() { Id = Guid.NewGuid(), TrackId = t1.Id, DspId = dspApple.Id, SubmittedAt = DateTime.UtcNow.AddMonths(-12), Status = DistributionStatus.Live },
            new() { Id = Guid.NewGuid(), TrackId = t1.Id, DspId = dspYouTube.Id, SubmittedAt = DateTime.UtcNow.AddMonths(-12), Status = DistributionStatus.Live },

            // Save Your Tears -> Spotify (Live), Apple (Live), YouTube (Pending)
            new() { Id = Guid.NewGuid(), TrackId = t2.Id, DspId = dspSpotify.Id, SubmittedAt = DateTime.UtcNow.AddMonths(-6), Status = DistributionStatus.Live },
            new() { Id = Guid.NewGuid(), TrackId = t2.Id, DspId = dspApple.Id, SubmittedAt = DateTime.UtcNow.AddMonths(-6), Status = DistributionStatus.Live },
            new() { Id = Guid.NewGuid(), TrackId = t2.Id, DspId = dspYouTube.Id, SubmittedAt = DateTime.UtcNow.AddDays(-2), Status = DistributionStatus.Pending },

            // Levitating -> Spotify (Live), Apple (Live), YouTube (Live)
            new() { Id = Guid.NewGuid(), TrackId = t3.Id, DspId = dspSpotify.Id, SubmittedAt = DateTime.UtcNow.AddMonths(-8), Status = DistributionStatus.Live },
            new() { Id = Guid.NewGuid(), TrackId = t3.Id, DspId = dspApple.Id, SubmittedAt = DateTime.UtcNow.AddMonths(-8), Status = DistributionStatus.Live },
            new() { Id = Guid.NewGuid(), TrackId = t3.Id, DspId = dspYouTube.Id, SubmittedAt = DateTime.UtcNow.AddMonths(-8), Status = DistributionStatus.Live },

            // Don't Start Now -> Spotify (Pending), Apple (Pending)
            new() { Id = Guid.NewGuid(), TrackId = t4.Id, DspId = dspSpotify.Id, SubmittedAt = DateTime.UtcNow.AddDays(-1), Status = DistributionStatus.Pending },
            new() { Id = Guid.NewGuid(), TrackId = t4.Id, DspId = dspApple.Id, SubmittedAt = DateTime.UtcNow.AddDays(-1), Status = DistributionStatus.Pending },

            // One More Time -> Spotify (Live), Apple (Live), YouTube (Live)
            new() { Id = Guid.NewGuid(), TrackId = t5.Id, DspId = dspSpotify.Id, SubmittedAt = DateTime.UtcNow.AddYears(-2), Status = DistributionStatus.Live },
            new() { Id = Guid.NewGuid(), TrackId = t5.Id, DspId = dspApple.Id, SubmittedAt = DateTime.UtcNow.AddYears(-2), Status = DistributionStatus.Live },
            new() { Id = Guid.NewGuid(), TrackId = t5.Id, DspId = dspYouTube.Id, SubmittedAt = DateTime.UtcNow.AddYears(-2), Status = DistributionStatus.Live },

            // Harder, Better, Faster, Stronger -> Spotify (Pending), Apple (Rejected)
            new() { Id = Guid.NewGuid(), TrackId = t6.Id, DspId = dspSpotify.Id, SubmittedAt = DateTime.UtcNow.AddDays(-4), Status = DistributionStatus.Pending },
            new() { Id = Guid.NewGuid(), TrackId = t6.Id, DspId = dspApple.Id, SubmittedAt = DateTime.UtcNow.AddDays(-5), Status = DistributionStatus.Rejected },

            // Tamally Maak -> Spotify (Live), Apple (Live), YouTube (Live)
            new() { Id = Guid.NewGuid(), TrackId = t7.Id, DspId = dspSpotify.Id, SubmittedAt = DateTime.UtcNow.AddYears(-1), Status = DistributionStatus.Live },
            new() { Id = Guid.NewGuid(), TrackId = t7.Id, DspId = dspApple.Id, SubmittedAt = DateTime.UtcNow.AddYears(-1), Status = DistributionStatus.Live },
            new() { Id = Guid.NewGuid(), TrackId = t7.Id, DspId = dspYouTube.Id, SubmittedAt = DateTime.UtcNow.AddYears(-1), Status = DistributionStatus.Live },

            // Nour El Ain -> Spotify (Pending), YouTube (Pending)
            new() { Id = Guid.NewGuid(), TrackId = t8.Id, DspId = dspSpotify.Id, SubmittedAt = DateTime.UtcNow.AddHours(-10), Status = DistributionStatus.Pending },
            new() { Id = Guid.NewGuid(), TrackId = t8.Id, DspId = dspYouTube.Id, SubmittedAt = DateTime.UtcNow.AddHours(-10), Status = DistributionStatus.Pending }
        };

        await context.TrackDistributions.AddRangeAsync(distributions);

        await context.SaveChangesAsync();
    }
}
