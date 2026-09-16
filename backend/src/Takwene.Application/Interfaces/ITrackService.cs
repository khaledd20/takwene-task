using Takwene.Application.DTOs.Tracks;

namespace Takwene.Application.Interfaces;

public interface ITrackService
{
    Task<TrackDto> CreateTrackAsync(CreateTrackRequest request, CancellationToken ct = default);
    Task<IEnumerable<TrackDto>> GetTracksAsync(Guid? artistId, string? genre, string? status, CancellationToken ct = default);
    Task<TrackDetailDto> GetTrackByIdAsync(Guid id, CancellationToken ct = default);
    Task<TrackDetailDto> DistributeTrackAsync(Guid id, DistributeTrackRequest request, CancellationToken ct = default);
    Task<TrackDto> UpdateTrackStatusAsync(Guid id, UpdateTrackStatusRequest request, CancellationToken ct = default);
    Task<TrackDetailDto> UpdateDistributionStatusAsync(Guid trackId, Guid dspId, UpdateDistributionStatusRequest request, CancellationToken ct = default);
    Task<byte[]> ExportCatalogCsvAsync(CancellationToken ct = default);
}
