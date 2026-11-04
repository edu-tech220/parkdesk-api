using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkDesk.Api.Common;
using ParkDesk.Api.Dtos;
using ParkDesk.Api.Entities;
using ParkDesk.Api.Services;

namespace ParkDesk.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Operator")]
public class SessionsController : ControllerBase
{
    private readonly IParkingService _parkingService;

    public SessionsController(IParkingService parkingService)
    {
        _parkingService = parkingService;
    }

    [HttpPost("api/lots/{lotId:guid}/sessions/checkin")]
    public async Task<ActionResult<SessionResponse>> CheckIn(Guid lotId, CheckInRequest request)
    {
        var session = await _parkingService.CheckInWalkInAsync(lotId, request.Plate, request.VehicleType, userId: null);
        return CreatedAtAction(nameof(GetById), new { id = session.Id }, SessionResponse.From(session));
    }

    [HttpPost("api/sessions/{id:guid}/checkout")]
    public async Task<ActionResult<SessionResponse>> CheckOut(Guid id)
    {
        var session = await _parkingService.CheckOutAsync(id);
        return Ok(SessionResponse.From(session));
    }

    [HttpGet("api/sessions/{id:guid}")]
    public async Task<ActionResult<SessionResponse>> GetById(Guid id)
    {
        var session = await _parkingService.GetByIdAsync(id);
        return session is null ? NotFound() : Ok(SessionResponse.From(session));
    }

    [HttpGet("api/sessions")]
    public async Task<ActionResult<PagedResult<SessionResponse>>> List(
        [FromQuery] Guid? lotId, [FromQuery] SessionStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _parkingService.ListSessionsAsync(lotId, status, page, pageSize);
        return Ok(result.Map(SessionResponse.From));
    }
}
