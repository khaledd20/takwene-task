using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Takwene.Application.DTOs.Tracks;
using Takwene.Application.Services;
using Takwene.Application.Validators;
using Takwene.Domain.Entities;
using Takwene.Domain.Enums;
using Takwene.Domain.Exceptions;

namespace Takwene.UnitTests;

public class TrackServiceTests : IDisposable
{
    private readonly Takwene.Infrastructure.Persistence.TakweneDbContext _context;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
    private readonly TrackService _sut;
    private readonly Artist _testArtist;
    private readonly Dsp _testDsp1;
    private readonly Dsp _testDsp2;

    public TrackServiceTests()
    {
        (_context, _connection) = TestDbContextFactory.CreateInMemoryDbContext();

        _testArtist = new Artist
        {
            Id = Guid.NewGuid(),
            Name = "Taylor Swift",
            Email = "taylor@swift.com",
            Country = "US"
        };
        _context.Artists.Add(_testArtist);

        _testDsp1 = new Dsp { Id = Guid.NewGuid(), Name = "Spotify" };
        _testDsp2 = new Dsp { Id = Guid.NewGuid(), Name = "Apple Music" };
        _context.Dsps.AddRange(_testDsp1, _testDsp2);
        _context.SaveChanges();

        var createVal = new CreateTrackRequestValidator();
        var updateVal = new UpdateTrackStatusRequestValidator();
        var distVal = new DistributeTrackRequestValidator();
        var updateDistVal = new UpdateDistributionStatusRequestValidator();

        _sut = new TrackService(_context, createVal, updateVal, distVal, updateDistVal);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateTrackAsync_WithValidData_CreatesDraftTrack()
    {
        // Arrange
        var request = new CreateTrackRequest
        {
            Title = "Anti-Hero",
            ArtistId = _testArtist.Id,
            Isrc = "USUG12204567",
            ReleaseDate = new DateTime(2022, 10, 21),
            Genre = "Pop",
            Status = "draft"
        };

        // Act
        var result = await _sut.CreateTrackAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Title.Should().Be("Anti-Hero");
        result.ArtistName.Should().Be("Taylor Swift");
        result.Status.Should().Be("draft");
        result.Isrc.Should().Be("USUG12204567");
    }

    [Fact]
    public async Task CreateTrackAsync_WithNonExistentArtist_ThrowsNotFoundException()
    {
        // Arrange
        var request = new CreateTrackRequest
        {
            Title = "Ghost Track",
            ArtistId = Guid.NewGuid(),
            Isrc = "USUG12204599",
            ReleaseDate = DateTime.UtcNow,
            Genre = "Pop"
        };

        // Act & Assert
        var act = async () => await _sut.CreateTrackAsync(request);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateTrackAsync_WithDuplicateIsrc_ThrowsConflictException()
    {
        // Arrange
        var request1 = new CreateTrackRequest
        {
            Title = "Track 1",
            ArtistId = _testArtist.Id,
            Isrc = "USUG12204567",
            ReleaseDate = DateTime.UtcNow,
            Genre = "Pop"
        };
        await _sut.CreateTrackAsync(request1);

        var request2 = new CreateTrackRequest
        {
            Title = "Track 2",
            ArtistId = _testArtist.Id,
            Isrc = "US-UG1-22-04567", // same ISRC with hyphens
            ReleaseDate = DateTime.UtcNow,
            Genre = "Pop"
        };

        // Act & Assert
        var act = async () => await _sut.CreateTrackAsync(request2);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task DistributeTrackAsync_WhenDraft_TransitionsTrackToSubmittedAndAddsDistributions()
    {
        // Arrange
        var track = await _sut.CreateTrackAsync(new CreateTrackRequest
        {
            Title = "Lavender Haze",
            ArtistId = _testArtist.Id,
            Isrc = "USUG12204588",
            ReleaseDate = DateTime.UtcNow,
            Genre = "Pop",
            Status = "draft"
        });

        // Act
        var detail = await _sut.DistributeTrackAsync(track.Id, new DistributeTrackRequest
        {
            DspIds = new List<Guid> { _testDsp1.Id, _testDsp2.Id }
        });

        // Assert
        detail.Status.Should().Be("submitted");
        detail.Distributions.Should().HaveCount(2);
        detail.Distributions.All(d => d.Status == "pending").Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTrackStatusAsync_ToDistributed_MarksPendingDistributionsLive()
    {
        // Arrange
        var track = await _sut.CreateTrackAsync(new CreateTrackRequest
        {
            Title = "Karma",
            ArtistId = _testArtist.Id,
            Isrc = "USUG12204577",
            ReleaseDate = DateTime.UtcNow,
            Genre = "Pop"
        });

        await _sut.DistributeTrackAsync(track.Id, new DistributeTrackRequest
        {
            DspIds = new List<Guid> { _testDsp1.Id }
        });

        // Act
        var updated = await _sut.UpdateTrackStatusAsync(track.Id, new UpdateTrackStatusRequest
        {
            Status = "distributed"
        });

        // Assert
        updated.Status.Should().Be("distributed");

        var detail = await _sut.GetTrackByIdAsync(track.Id);
        detail.Distributions.First().Status.Should().Be("live");
    }

    [Fact]
    public async Task GetTracksAsync_FilterByStatus_ReturnsOnlyMatchingTracks()
    {
        // Arrange
        await _sut.CreateTrackAsync(new CreateTrackRequest
        {
            Title = "Draft Song",
            ArtistId = _testArtist.Id,
            Isrc = "USUG12200001",
            ReleaseDate = DateTime.UtcNow,
            Genre = "Rock",
            Status = "draft"
        });

        await _sut.CreateTrackAsync(new CreateTrackRequest
        {
            Title = "Submitted Song",
            ArtistId = _testArtist.Id,
            Isrc = "USUG12200002",
            ReleaseDate = DateTime.UtcNow,
            Genre = "Rock",
            Status = "submitted"
        });

        // Act
        var drafts = await _sut.GetTracksAsync(null, null, "draft");
        var submitted = await _sut.GetTracksAsync(null, null, "submitted");

        // Assert
        drafts.Should().ContainSingle(t => t.Title == "Draft Song");
        submitted.Should().ContainSingle(t => t.Title == "Submitted Song");
    }

    [Fact]
    public async Task UpdateDistributionStatusAsync_WhenApprovedToLive_UpdatesDistributionAndSetsTrackToDistributedWhenAllLive()
    {
        // Arrange
        var track = await _sut.CreateTrackAsync(new CreateTrackRequest
        {
            Title = "Karma",
            ArtistId = _testArtist.Id,
            Isrc = "USUG12200111",
            ReleaseDate = DateTime.UtcNow,
            Genre = "Pop"
        });

        await _sut.DistributeTrackAsync(track.Id, new DistributeTrackRequest
        {
            DspIds = new List<Guid> { _testDsp1.Id }
        });

        var request = new UpdateDistributionStatusRequest
        {
            Status = "live"
        };

        // Act
        var result = await _sut.UpdateDistributionStatusAsync(track.Id, _testDsp1.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("distributed");
        var dist = result.Distributions.Should().ContainSingle(d => d.DspId == _testDsp1.Id).Subject;
        dist.Status.Should().Be("live");
        dist.RejectionReason.Should().BeNull();
        dist.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateDistributionStatusAsync_WhenRejectedWithReason_SetsRejectedStatusAndRejectionReason()
    {
        // Arrange
        var track = await _sut.CreateTrackAsync(new CreateTrackRequest
        {
            Title = "Mastermind",
            ArtistId = _testArtist.Id,
            Isrc = "USUG12200222",
            ReleaseDate = DateTime.UtcNow,
            Genre = "Pop"
        });

        await _sut.DistributeTrackAsync(track.Id, new DistributeTrackRequest
        {
            DspIds = new List<Guid> { _testDsp1.Id }
        });

        var request = new UpdateDistributionStatusRequest
        {
            Status = "rejected",
            RejectionReason = "Audio bitrate does not meet DSP requirements (minimum 320kbps)."
        };

        // Act
        var result = await _sut.UpdateDistributionStatusAsync(track.Id, _testDsp1.Id, request);

        // Assert
        result.Should().NotBeNull();
        var dist = result.Distributions.Should().ContainSingle(d => d.DspId == _testDsp1.Id).Subject;
        dist.Status.Should().Be("rejected");
        dist.RejectionReason.Should().Be("Audio bitrate does not meet DSP requirements (minimum 320kbps).");
        dist.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateDistributionStatusAsync_WhenRejectedWithoutReason_ThrowsDomainException()
    {
        // Arrange
        var track = await _sut.CreateTrackAsync(new CreateTrackRequest
        {
            Title = "Bejeweled",
            ArtistId = _testArtist.Id,
            Isrc = "USUG12200333",
            ReleaseDate = DateTime.UtcNow,
            Genre = "Pop"
        });

        await _sut.DistributeTrackAsync(track.Id, new DistributeTrackRequest
        {
            DspIds = new List<Guid> { _testDsp1.Id }
        });

        var request = new UpdateDistributionStatusRequest
        {
            Status = "rejected",
            RejectionReason = "" // Empty rejection reason should fail validation
        };

        // Act & Assert
        var act = async () => await _sut.UpdateDistributionStatusAsync(track.Id, _testDsp1.Id, request);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Rejection reason is required*");
    }

    [Fact]
    public async Task ExportCatalogCsvAsync_ReturnsValidCsvWithHeadersAndContent()
    {
        // Arrange
        var track = await _sut.CreateTrackAsync(new CreateTrackRequest
        {
            Title = "Lavender Haze",
            ArtistId = _testArtist.Id,
            Isrc = "USUG12200444",
            ReleaseDate = new DateTime(2022, 10, 21),
            Genre = "Pop",
            Status = "draft"
        });

        // Act
        var csvBytes = await _sut.ExportCatalogCsvAsync();
        var csvContent = System.Text.Encoding.UTF8.GetString(csvBytes);

        // Assert
        csvContent.Should().NotBeNullOrEmpty();
        csvContent.Should().StartWith("ID,Title,Artist,ISRC,Genre,ReleaseDate,Status,TotalDsps,LiveDsps,PendingDsps,RejectedDsps");
        csvContent.Should().Contain("Lavender Haze");
        csvContent.Should().Contain("Taylor Swift");
        csvContent.Should().Contain("USUG12200444");
    }
}
