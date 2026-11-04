namespace ParkDesk.Api.Entities;

public class Reservation
{
    public Guid Id { get; set; }
    public Guid LotId { get; set; }
    public ParkingLot? Lot { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public SpotType SpotType { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public Guid? SpotId { get; set; }
    public ParkingSpot? Spot { get; set; }
    public Guid? SessionId { get; set; }
    public DateTime CreatedAt { get; set; }
}
