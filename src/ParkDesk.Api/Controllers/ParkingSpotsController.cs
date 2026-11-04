using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkDesk.Api.Common;
using ParkDesk.Api.Data;
using ParkDesk.Api.Dtos;
using ParkDesk.Api.Entities;

namespace ParkDesk.Api.Controllers;

[ApiController]
[Route("api/lots/{lotId:guid}/spots")]
public class ParkingSpotsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ParkingSpotsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<SpotResponse>>> GetAll(Guid lotId, [FromQuery] SpotType? type, [FromQuery] SpotStatus? status)
    {
        if (!await _db.ParkingLots.AnyAsync(l => l.Id == lotId))
            throw new NotFoundException("Estacionamiento no encontrado.");

        var query = _db.ParkingSpots.AsNoTracking().Where(s => s.LotId == lotId);
        if (type.HasValue) query = query.Where(s => s.Type == type.Value);
        if (status.HasValue) query = query.Where(s => s.Status == status.Value);

        var spots = await query.OrderBy(s => s.Code)
            .Select(s => new SpotResponse(s.Id, s.LotId, s.Code, s.Type, s.Status))
            .ToListAsync();

        return Ok(spots);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SpotResponse>> Create(Guid lotId, CreateSpotRequest request)
    {
        if (!await _db.ParkingLots.AnyAsync(l => l.Id == lotId))
            throw new NotFoundException("Estacionamiento no encontrado.");

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (await _db.ParkingSpots.AnyAsync(s => s.LotId == lotId && s.Code == normalizedCode))
            throw new ConflictException("Ya existe un espacio con ese codigo en este estacionamiento.");

        var spot = new ParkingSpot { Id = Guid.NewGuid(), LotId = lotId, Code = normalizedCode, Type = request.Type };
        _db.ParkingSpots.Add(spot);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { lotId }, new SpotResponse(spot.Id, spot.LotId, spot.Code, spot.Type, spot.Status));
    }
}
