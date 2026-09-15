using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Takwene.Application.DTOs.Dsps;
using Takwene.Application.Interfaces;

namespace Takwene.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DspsController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public DspsController(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// List all configured Digital Service Providers (DSPs).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DspDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DspDto>>> GetDsps(CancellationToken ct)
    {
        var dsps = await _context.Dsps
            .AsNoTracking()
            .Select(d => new DspDto
            {
                Id = d.Id,
                Name = d.Name
            })
            .OrderBy(d => d.Name)
            .ToListAsync(ct);

        return Ok(dsps);
    }
}
