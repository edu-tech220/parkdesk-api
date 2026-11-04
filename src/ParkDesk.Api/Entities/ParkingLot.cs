namespace ParkDesk.Api.Entities;

public class ParkingLot
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public List<ParkingSpot> Spots { get; set; } = new();
    public List<Tariff> Tariffs { get; set; } = new();
}
