using Microsoft.AspNetCore.Mvc;
using Takwene.Application.DTOs.Artists;
using Takwene.Application.Interfaces;

namespace Takwene.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArtistsController : ControllerBase
{
    private readonly IArtistService _artistService;

    public ArtistsController(IArtistService artistService)
    {
        _artistService = artistService;
    }

    /// <summary>
    /// Create a new artist.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ArtistDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ArtistDto>> CreateArtist([FromBody] CreateArtistRequest request, CancellationToken ct)
    {
        var result = await _artistService.CreateArtistAsync(request, ct);
        return CreatedAtAction(nameof(GetArtists), new { id = result.Id }, result);
    }

    /// <summary>
    /// List all registered artists.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ArtistDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ArtistDto>>> GetArtists(CancellationToken ct)
    {
        var artists = await _artistService.GetAllArtistsAsync(ct);
        return Ok(artists);
    }
}
