using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Takwene.Application.DTOs.Artists;
using Takwene.Application.Interfaces;
using Takwene.Domain.Entities;
using Takwene.Domain.Exceptions;

namespace Takwene.Application.Services;

public class ArtistService : IArtistService
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<CreateArtistRequest> _validator;

    public ArtistService(IApplicationDbContext _context, IValidator<CreateArtistRequest> validator)
    {
        this._context = _context;
        _validator = validator;
    }

    public async Task<ArtistDto> CreateArtistAsync(CreateArtistRequest request, CancellationToken ct = default)
    {
        var validationResult = await _validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new DomainException(errors);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailExists = await _context.Artists.AnyAsync(a => a.Email.ToLower() == normalizedEmail, ct);
        if (emailExists)
        {
            throw new ConflictException($"An artist with email '{request.Email}' already exists.");
        }

        var artist = new Artist
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Country = request.Country.Trim()
        };

        _context.Artists.Add(artist);
        await _context.SaveChangesAsync(ct);

        return new ArtistDto
        {
            Id = artist.Id,
            Name = artist.Name,
            Email = artist.Email,
            Country = artist.Country,
            TrackCount = 0
        };
    }

    public async Task<IEnumerable<ArtistDto>> GetAllArtistsAsync(CancellationToken ct = default)
    {
        return await _context.Artists
            .AsNoTracking()
            .Select(a => new ArtistDto
            {
                Id = a.Id,
                Name = a.Name,
                Email = a.Email,
                Country = a.Country,
                TrackCount = a.Tracks.Count
            })
            .OrderBy(a => a.Name)
            .ToListAsync(ct);
    }
}
