using Takwene.Application.DTOs.Artists;

namespace Takwene.Application.Interfaces;

public interface IArtistService
{
    Task<ArtistDto> CreateArtistAsync(CreateArtistRequest request, CancellationToken ct = default);
    Task<IEnumerable<ArtistDto>> GetAllArtistsAsync(CancellationToken ct = default);
}
