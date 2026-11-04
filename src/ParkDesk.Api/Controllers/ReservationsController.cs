using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkDesk.Api.Common;
using ParkDesk.Api.Dtos;
using ParkDesk.Api.Entities;
using ParkDesk.Api.Services;

namespace ParkDesk.Api.Controllers;

[ApiController]
[Route("api/reservations")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;

    public ReservationsController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsPrivileged => User.IsInRole("Admin") || User.IsInRole("Operator");

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<ReservationResponse>> Create(CreateReservationRequest request)
    {
        var reservation = await _reservationService.CreateAsync(
            request.LotId, CurrentUserId, request.SpotType, request.StartTime, request.EndTime);

        return CreatedAtAction(nameof(GetById), new { id = reservation.Id }, ReservationResponse.From(reservation));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReservationResponse>> GetById(Guid id)
    {
        var reservation = await _reservationService.GetByIdAsync(id);
        if (!IsPrivileged && reservation.UserId != CurrentUserId)
            return NotFound();

        return Ok(ReservationResponse.From(reservation));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ReservationResponse>>> List(
        [FromQuery] Guid? lotId, [FromQuery] ReservationStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var effectiveUserId = IsPrivileged ? null : (Guid?)CurrentUserId;
        var result = await _reservationService.ListAsync(lotId, effectiveUserId, status, page, pageSize);
        return Ok(result.Map(ReservationResponse.From));
    }

    [HttpPost("{id:guid}/checkin")]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<ActionResult<SessionResponse>> CheckIn(Guid id, ReservationCheckInRequest request)
    {
        var session = await _reservationService.CheckInAsync(id, request.Plate, request.VehicleType);
        return Ok(SessionResponse.From(session));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ReservationResponse>> Cancel(Guid id)
    {
        var reservation = await _reservationService.CancelAsync(id, CurrentUserId, IsPrivileged);
        return Ok(ReservationResponse.From(reservation));
    }
}
