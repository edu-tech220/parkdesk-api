namespace ParkDesk.Api.Entities;

public class Tariff
{
    public Guid Id { get; set; }
    public Guid LotId { get; set; }
    public ParkingLot? Lot { get; set; }
    public SpotType SpotType { get; set; }
    public decimal PricePerHour { get; set; }
    public int GraceMinutes { get; set; }
    public decimal DailyCapAmount { get; set; }
    public decimal OverstayPenaltyPercent { get; set; }
}
