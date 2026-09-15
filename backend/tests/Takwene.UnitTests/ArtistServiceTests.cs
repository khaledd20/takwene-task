using FluentAssertions;
using Takwene.Application.DTOs.Artists;
using Takwene.Application.Services;
using Takwene.Application.Validators;
using Takwene.Domain.Exceptions;

namespace Takwene.UnitTests;

public class ArtistServiceTests : IDisposable
{
    private readonly Takwene.Infrastructure.Persistence.TakweneDbContext _context;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
    private readonly ArtistService _sut;

    public ArtistServiceTests()
    {
        (_context, _connection) = TestDbContextFactory.CreateInMemoryDbContext();
        var validator = new CreateArtistRequestValidator();
        _sut = new ArtistService(_context, validator);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateArtistAsync_WithValidData_CreatesAndReturnsArtist()
    {
        // Arrange
        var request = new CreateArtistRequest
        {
            Name = "Billie Eilish",
            Email = "billie@eilish.com",
            Country = "United States"
        };

        // Act
        var result = await _sut.CreateArtistAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Billie Eilish");
        result.Email.Should().Be("billie@eilish.com");
        result.Country.Should().Be("United States");
        result.TrackCount.Should().Be(0);

        var inDb = await _context.Artists.FindAsync(result.Id);
        inDb.Should().NotBeNull();
        inDb!.Name.Should().Be("Billie Eilish");
    }

    [Fact]
    public async Task CreateArtistAsync_WithDuplicateEmail_ThrowsConflictException()
    {
        // Arrange
        var request1 = new CreateArtistRequest
        {
            Name = "Billie Eilish",
            Email = "billie@eilish.com",
            Country = "United States"
        };
        await _sut.CreateArtistAsync(request1);

        var request2 = new CreateArtistRequest
        {
            Name = "Another Artist",
            Email = "billie@eilish.com",
            Country = "Canada"
        };

        // Act & Assert
        var act = async () => await _sut.CreateArtistAsync(request2);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateArtistAsync_WithInvalidEmail_ThrowsDomainException()
    {
        // Arrange
        var request = new CreateArtistRequest
        {
            Name = "Invalid Artist",
            Email = "not-a-valid-email",
            Country = "Egypt"
        };

        // Act & Assert
        var act = async () => await _sut.CreateArtistAsync(request);
        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task GetAllArtistsAsync_ReturnsAllArtistsWithTrackCount()
    {
        // Arrange
        await _sut.CreateArtistAsync(new CreateArtistRequest { Name = "Artist A", Email = "a@test.com", Country = "EG" });
        await _sut.CreateArtistAsync(new CreateArtistRequest { Name = "Artist B", Email = "b@test.com", Country = "US" });

        // Act
        var result = await _sut.GetAllArtistsAsync();

        // Assert
        result.Should().HaveCount(2);
    }
}
