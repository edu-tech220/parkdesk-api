using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkDesk.Api.Common;
using ParkDesk.Api.Data;
using ParkDesk.Api.Dtos;
using ParkDesk.Api.Entities;

namespace ParkDesk.Api.Controllers;

[ApiController]
[Route("api/lots/{lotId:guid}/tariffs")]
public class TariffsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TariffsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<TariffResponse>>> GetAll(Guid lotId)
    {
        var tariffs = await _db.Tariffs.AsNoTracking()
            .Where(t => t.LotId == lotId)
            .Select(t => new TariffResponse(t.Id, t.LotId, t.SpotType, t.PricePerHour, t.GraceMinutes, t.DailyCapAmount, t.OverstayPenaltyPercent))
            .ToListAsync();

        return Ok(tariffs);
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TariffResponse>> Upsert(Guid lotId, UpsertTariffRequest request)
    {
        if (!await _db.ParkingLots.AnyAsync(l => l.Id == lotId))
            throw new NotFoundException("Estacionamiento no encontrado.");

        var tariff = await _db.Tariffs.FirstOrDefaultAsync(t => t.LotId == lotId && t.SpotType == request.SpotType);
        if (tariff is null)
        {
            tariff = new Tariff { Id = Guid.NewGuid(), LotId = lotId, SpotType = request.SpotType };
            _db.Tariffs.Add(tariff);
        }

        tariff.PricePerHour = request.PricePerHour;
        tariff.GraceMinutes = request.GraceMinutes;
        tariff.DailyCapAmount = request.DailyCapAmount;
        tariff.OverstayPenaltyPercent = request.OverstayPenaltyPercent;

        await _db.SaveChangesAsync();

        return Ok(new TariffResponse(tariff.Id, tariff.LotId, tariff.SpotType, tariff.PricePerHour, tariff.GraceMinutes, tariff.DailyCapAmount, tariff.OverstayPenaltyPercent));
    }
}
