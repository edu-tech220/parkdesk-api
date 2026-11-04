using System.ComponentModel.DataAnnotations;
using ParkDesk.Api.Entities;

namespace ParkDesk.Api.Dtos;

public record CheckInRequest(
    [Required, MaxLength(10)] string Plate,
    [Required] VehicleType VehicleType);

public record SessionResponse(
    Guid Id,
    Guid LotId,
    Guid SpotId,
    string? SpotCode,
    string Plate,
    VehicleType VehicleType,
    DateTime CheckInAt,
    DateTime? CheckOutAt,
    SessionStatus Status,
    Guid? ReservationId,
    decimal? AmountCharged,
    decimal? OverstayFee)
{
    public static SessionResponse From(ParkingSession session) => new(
        session.Id,
        session.LotId,
        session.SpotId,
        session.Spot?.Code,
        session.Plate,
        session.VehicleType,
        session.CheckInAt,
        session.CheckOutAt,
        session.Status,
        session.ReservationId,
        session.AmountCharged,
        session.OverstayFee);
}
