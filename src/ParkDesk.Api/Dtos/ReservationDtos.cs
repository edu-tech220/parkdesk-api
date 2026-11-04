using System.ComponentModel.DataAnnotations;
using ParkDesk.Api.Entities;

namespace ParkDesk.Api.Dtos;

public record CreateReservationRequest(
    [Required] Guid LotId,
    [Required] SpotType SpotType,
    [Required] DateTime StartTime,
    [Required] DateTime EndTime);

public record ReservationCheckInRequest(
    [Required, MaxLength(10)] string Plate,
    [Required] VehicleType VehicleType);

public record ReservationResponse(
    Guid Id,
    Guid LotId,
    Guid UserId,
    SpotType SpotType,
    DateTime StartTime,
    DateTime EndTime,
    ReservationStatus Status,
    Guid? SpotId,
    Guid? SessionId)
{
    public static ReservationResponse From(Reservation r) => new(
        r.Id, r.LotId, r.UserId, r.SpotType, r.StartTime, r.EndTime, r.Status, r.SpotId, r.SessionId);
}
