namespace ParkDesk.Api.Entities;

public class ParkingSession
{
    public Guid Id { get; set; }
    public Guid LotId { get; set; }
    public ParkingLot? Lot { get; set; }
    public Guid SpotId { get; set; }
    public ParkingSpot? Spot { get; set; }
    public string Plate { get; set; } = string.Empty;
    public VehicleType VehicleType { get; set; }
    public DateTime CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public Guid? ReservationId { get; set; }
    public Reservation? Reservation { get; set; }
    public Guid? UserId { get; set; }
    public decimal? AmountCharged { get; set; }
    public decimal? OverstayFee { get; set; }
}
