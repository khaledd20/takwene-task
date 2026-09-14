using Microsoft.EntityFrameworkCore;
using Takwene.Domain.Entities;

namespace Takwene.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Artist> Artists { get; }
    DbSet<Track> Tracks { get; }
    DbSet<Dsp> Dsps { get; }
    DbSet<TrackDistribution> TrackDistributions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
