using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Takwene.Application.DTOs.Tracks;
using Takwene.Application.Interfaces;

namespace Takwene.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TracksController : ControllerBase
{
    private readonly ITrackService _trackService;

    public TracksController(ITrackService trackService)
    {
        _trackService = trackService;
    }

    /// <summary>
    /// Create a new track for an artist (JWT protected).
    /// </summary>
    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(TrackDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TrackDto>> CreateTrack([FromBody] CreateTrackRequest request, CancellationToken ct)
    {
        var result = await _trackService.CreateTrackAsync(request, ct);
        return CreatedAtAction(nameof(GetTrackById), new { id = result.Id }, result);
    }

    /// <summary>
    /// List all tracks with optional filters by artistId, genre, and status.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TrackDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TrackDto>>> GetTracks(
        [FromQuery] Guid? artistId,
        [FromQuery] string? genre,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var tracks = await _trackService.GetTracksAsync(artistId, genre, status, ct);
        return Ok(tracks);
    }

    /// <summary>
    /// Get full track details including its DSP distribution statuses.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TrackDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrackDetailDto>> GetTrackById(Guid id, CancellationToken ct)
    {
        var track = await _trackService.GetTrackByIdAsync(id, ct);
        return Ok(track);
    }

    /// <summary>
    /// Submit a track to one or more DSPs (JWT protected).
    /// </summary>
    [Authorize]
    [HttpPost("{id:guid}/distribute")]
    [ProducesResponseType(typeof(TrackDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrackDetailDto>> DistributeTrack(
        Guid id,
        [FromBody] DistributeTrackRequest request,
        CancellationToken ct)
    {
        var result = await _trackService.DistributeTrackAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Update a track's status (e.g. draft, submitted, distributed) (JWT protected).
    /// </summary>
    [Authorize]
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(TrackDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrackDto>> UpdateTrackStatus(
        Guid id,
        [FromBody] UpdateTrackStatusRequest request,
        CancellationToken ct)
    {
        var result = await _trackService.UpdateTrackStatusAsync(id, request, ct);
        return Ok(result);
    }
}
