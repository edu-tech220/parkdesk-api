using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkDesk.Api.Data;
using ParkDesk.Api.Dtos;
using ParkDesk.Api.Entities;

namespace ParkDesk.Api.Controllers;

[ApiController]
[Route("api/lots")]
public class ParkingLotsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ParkingLotsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<LotResponse>>> GetAll()
    {
        var lots = await _db.ParkingLots
            .Select(l => new LotResponse(
                l.Id,
                l.Name,
                l.Address,
                l.Spots.Count(s => s.Status != SpotStatus.OutOfService),
                l.Spots.Count(s => s.Status == SpotStatus.Available)))
            .ToListAsync();

        return Ok(lots);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<LotResponse>> GetById(Guid id)
    {
        var lot = await _db.ParkingLots
            .Where(l => l.Id == id)
            .Select(l => new LotResponse(
                l.Id,
                l.Name,
                l.Address,
                l.Spots.Count(s => s.Status != SpotStatus.OutOfService),
                l.Spots.Count(s => s.Status == SpotStatus.Available)))
            .FirstOrDefaultAsync();

        return lot is null ? NotFound() : Ok(lot);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LotResponse>> Create(CreateLotRequest request)
    {
        var lot = new ParkingLot { Id = Guid.NewGuid(), Name = request.Name, Address = request.Address };
        _db.ParkingLots.Add(lot);
        await _db.SaveChangesAsync();

        var response = new LotResponse(lot.Id, lot.Name, lot.Address, 0, 0);
        return CreatedAtAction(nameof(GetById), new { id = lot.Id }, response);
    }
}
