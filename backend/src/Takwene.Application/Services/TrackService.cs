using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Takwene.Application.DTOs.Dsps;
using Takwene.Application.DTOs.Tracks;
using Takwene.Application.Interfaces;
using Takwene.Domain.Entities;
using Takwene.Domain.Enums;
using Takwene.Domain.Exceptions;

namespace Takwene.Application.Services;

public class TrackService : ITrackService
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<CreateTrackRequest> _createValidator;
    private readonly IValidator<UpdateTrackStatusRequest> _updateStatusValidator;
    private readonly IValidator<DistributeTrackRequest> _distributeValidator;
    private readonly IValidator<UpdateDistributionStatusRequest> _updateDistributionStatusValidator;

    public TrackService(
        IApplicationDbContext context,
        IValidator<CreateTrackRequest> createValidator,
        IValidator<UpdateTrackStatusRequest> updateStatusValidator,
        IValidator<DistributeTrackRequest> distributeValidator,
        IValidator<UpdateDistributionStatusRequest> updateDistributionStatusValidator)
    {
        _context = context;
        _createValidator = createValidator;
        _updateStatusValidator = updateStatusValidator;
        _distributeValidator = distributeValidator;
        _updateDistributionStatusValidator = updateDistributionStatusValidator;
    }

    public async Task<TrackDto> CreateTrackAsync(CreateTrackRequest request, CancellationToken ct = default)
    {
        var validationResult = await _createValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new DomainException(errors);
        }

        var artist = await _context.Artists.FindAsync(new object[] { request.ArtistId }, ct);
        if (artist == null)
        {
            throw new NotFoundException("Artist", request.ArtistId);
        }

        var cleanIsrc = request.Isrc.Trim().Replace("-", "").ToUpperInvariant();
        var isrcExists = await _context.Tracks.AnyAsync(t => t.Isrc.Replace("-", "").ToUpper() == cleanIsrc, ct);
        if (isrcExists)
        {
            throw new ConflictException($"A track with ISRC '{request.Isrc}' already exists.");
        }

        var initialStatus = TrackStatus.Draft;
        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<TrackStatus>(request.Status.Trim(), true, out var parsedStatus))
        {
            initialStatus = parsedStatus;
        }

        var track = new Track
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            ArtistId = request.ArtistId,
            Isrc = cleanIsrc,
            ReleaseDate = request.ReleaseDate,
            Genre = request.Genre.Trim(),
            Status = initialStatus
        };

        _context.Tracks.Add(track);
        await _context.SaveChangesAsync(ct);

        return new TrackDto
        {
            Id = track.Id,
            Title = track.Title,
            ArtistId = track.ArtistId,
            ArtistName = artist.Name,
            Isrc = track.Isrc,
            ReleaseDate = track.ReleaseDate,
            Genre = track.Genre,
            Status = track.Status.ToString().ToLowerInvariant()
        };
    }

    public async Task<IEnumerable<TrackDto>> GetTracksAsync(Guid? artistId, string? genre, string? status, CancellationToken ct = default)
    {
        var query = _context.Tracks
            .Include(t => t.Artist)
            .AsNoTracking()
            .AsQueryable();

        if (artistId.HasValue && artistId.Value != Guid.Empty)
        {
            query = query.Where(t => t.ArtistId == artistId.Value);
        }

        if (!string.IsNullOrWhiteSpace(genre))
        {
            var trimmedGenre = genre.Trim().ToLower();
            query = query.Where(t => t.Genre.ToLower() == trimmedGenre);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<TrackStatus>(status.Trim(), true, out var parsedStatus))
            {
                query = query.Where(t => t.Status == parsedStatus);
            }
        }

        return await query
            .OrderByDescending(t => t.ReleaseDate)
            .Select(t => new TrackDto
            {
                Id = t.Id,
                Title = t.Title,
                ArtistId = t.ArtistId,
                ArtistName = t.Artist != null ? t.Artist.Name : "Unknown Artist",
                Isrc = t.Isrc,
                ReleaseDate = t.ReleaseDate,
                Genre = t.Genre,
                Status = t.Status.ToString().ToLowerInvariant()
            })
            .ToListAsync(ct);
    }

    public async Task<TrackDetailDto> GetTrackByIdAsync(Guid id, CancellationToken ct = default)
    {
        var track = await _context.Tracks
            .Include(t => t.Artist)
            .Include(t => t.Distributions)
                .ThenInclude(d => d.Dsp)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (track == null)
        {
            throw new NotFoundException("Track", id);
        }

        return new TrackDetailDto
        {
            Id = track.Id,
            Title = track.Title,
            ArtistId = track.ArtistId,
            ArtistName = track.Artist?.Name ?? "Unknown Artist",
            ArtistEmail = track.Artist?.Email ?? string.Empty,
            ArtistCountry = track.Artist?.Country ?? string.Empty,
            Isrc = track.Isrc,
            ReleaseDate = track.ReleaseDate,
            Genre = track.Genre,
            Status = track.Status.ToString().ToLowerInvariant(),
            Distributions = track.Distributions.Select(d => new TrackDistributionDto
            {
                Id = d.Id,
                TrackId = d.TrackId,
                DspId = d.DspId,
                DspName = d.Dsp?.Name ?? "Unknown DSP",
                SubmittedAt = d.SubmittedAt,
                Status = d.Status.ToString().ToLowerInvariant(),
                RejectionReason = d.RejectionReason,
                ReviewedAt = d.ReviewedAt
            }).OrderBy(d => d.DspName).ToList()
        };
    }

    public async Task<TrackDetailDto> DistributeTrackAsync(Guid id, DistributeTrackRequest request, CancellationToken ct = default)
    {
        var validationResult = await _distributeValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new DomainException(errors);
        }

        var track = await _context.Tracks
            .Include(t => t.Distributions)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (track == null)
        {
            throw new NotFoundException("Track", id);
        }

        var distinctDspIds = request.DspIds.Distinct().ToList();
        var existingDsps = await _context.Dsps
            .Where(d => distinctDspIds.Contains(d.Id))
            .ToListAsync(ct);

        if (existingDsps.Count != distinctDspIds.Count)
        {
            var foundIds = existingDsps.Select(d => d.Id);
            var missingIds = distinctDspIds.Except(foundIds);
            throw new NotFoundException($"DSP(s) with ID(s) {string.Join(", ", missingIds)} were not found.");
        }

        foreach (var dspId in distinctDspIds)
        {
            var existingDist = track.Distributions.FirstOrDefault(d => d.DspId == dspId);
            if (existingDist == null)
            {
                var newDist = new TrackDistribution
                {
                    Id = Guid.NewGuid(),
                    TrackId = track.Id,
                    DspId = dspId,
                    SubmittedAt = DateTime.UtcNow,
                    Status = DistributionStatus.Pending
                };
                _context.TrackDistributions.Add(newDist);
            }
            else
            {
                // Re-submit if rejected
                if (existingDist.Status == DistributionStatus.Rejected)
                {
                    existingDist.Status = DistributionStatus.Pending;
                    existingDist.SubmittedAt = DateTime.UtcNow;
                }
            }
        }

        // If currently in Draft, transition to Submitted upon distribution dispatch
        if (track.Status == TrackStatus.Draft)
        {
            track.Status = TrackStatus.Submitted;
        }

        await _context.SaveChangesAsync(ct);

        return await GetTrackByIdAsync(id, ct);
    }

    public async Task<TrackDto> UpdateTrackStatusAsync(Guid id, UpdateTrackStatusRequest request, CancellationToken ct = default)
    {
        var validationResult = await _updateStatusValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new DomainException(errors);
        }

        if (!Enum.TryParse<TrackStatus>(request.Status.Trim(), true, out var newStatus))
        {
            throw new DomainException($"Invalid status '{request.Status}'. Valid values are: draft, submitted, distributed.");
        }

        var track = await _context.Tracks
            .Include(t => t.Artist)
            .Include(t => t.Distributions)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (track == null)
        {
            throw new NotFoundException("Track", id);
        }

        track.Status = newStatus;

        // If status moved to distributed, transition any pending distributions to live
        if (newStatus == TrackStatus.Distributed)
        {
            foreach (var dist in track.Distributions.Where(d => d.Status == DistributionStatus.Pending))
            {
                dist.Status = DistributionStatus.Live;
            }
        }

        await _context.SaveChangesAsync(ct);

        return new TrackDto
        {
            Id = track.Id,
            Title = track.Title,
            ArtistId = track.ArtistId,
            ArtistName = track.Artist?.Name ?? "Unknown Artist",
            Isrc = track.Isrc,
            ReleaseDate = track.ReleaseDate,
            Genre = track.Genre,
            Status = track.Status.ToString().ToLowerInvariant()
        };
    }

    public async Task<TrackDetailDto> UpdateDistributionStatusAsync(Guid trackId, Guid dspId, UpdateDistributionStatusRequest request, CancellationToken ct = default)
    {
        var validationResult = await _updateDistributionStatusValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new DomainException(errors);
        }

        var track = await _context.Tracks
            .Include(t => t.Distributions)
            .FirstOrDefaultAsync(t => t.Id == trackId, ct);

        if (track == null)
        {
            throw new NotFoundException("Track", trackId);
        }

        var distribution = track.Distributions.FirstOrDefault(d => d.DspId == dspId);
        if (distribution == null)
        {
            throw new NotFoundException($"Distribution for DSP '{dspId}' on track '{trackId}' not found.");
        }

        var isLive = request.Status.Trim().Equals("live", StringComparison.OrdinalIgnoreCase);
        if (isLive)
        {
            distribution.Status = DistributionStatus.Live;
            distribution.RejectionReason = null;
            distribution.ReviewedAt = DateTime.UtcNow;

            // If all distributions are live, auto-transition track status to Distributed
            if (track.Distributions.Count > 0 && track.Distributions.All(d => d.Status == DistributionStatus.Live))
            {
                track.Status = TrackStatus.Distributed;
            }
        }
        else
        {
            distribution.Status = DistributionStatus.Rejected;
            distribution.RejectionReason = request.RejectionReason?.Trim();
            distribution.ReviewedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);

        return await GetTrackByIdAsync(trackId, ct);
    }

    public async Task<byte[]> ExportCatalogCsvAsync(CancellationToken ct = default)
    {
        var tracks = await _context.Tracks
            .Include(t => t.Artist)
            .Include(t => t.Distributions)
                .ThenInclude(d => d.Dsp)
            .AsNoTracking()
            .OrderBy(t => t.Title)
            .ToListAsync(ct);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("ID,Title,Artist,ISRC,Genre,ReleaseDate,Status,TotalDsps,LiveDsps,PendingDsps,RejectedDsps");

        foreach (var t in tracks)
        {
            var title = EscapeCsv(t.Title);
            var artistName = EscapeCsv(t.Artist?.Name ?? "Unknown");
            var isrc = EscapeCsv(t.Isrc);
            var genre = EscapeCsv(t.Genre);
            var releaseDate = t.ReleaseDate.ToString("yyyy-MM-dd");
            var status = t.Status.ToString().ToLowerInvariant();

            var totalDsps = t.Distributions.Count;
            var liveDsps = t.Distributions.Count(d => d.Status == DistributionStatus.Live);
            var pendingDsps = t.Distributions.Count(d => d.Status == DistributionStatus.Pending);
            var rejectedDsps = t.Distributions.Count(d => d.Status == DistributionStatus.Rejected);

            sb.AppendLine($"{t.Id},\"{title}\",\"{artistName}\",\"{isrc}\",\"{genre}\",{releaseDate},{status},{totalDsps},{liveDsps},{pendingDsps},{rejectedDsps}");
        }

        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string field)
    {
        if (string.IsNullOrEmpty(field)) return string.Empty;
        return field.Replace("\"", "\"\"");
    }
}
