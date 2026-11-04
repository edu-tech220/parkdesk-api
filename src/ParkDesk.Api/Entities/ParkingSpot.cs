namespace ParkDesk.Api.Entities;

public class ParkingSpot
{
    public Guid Id { get; set; }
    public Guid LotId { get; set; }
    public ParkingLot? Lot { get; set; }
    public string Code { get; set; } = string.Empty;
    public SpotType Type { get; set; }
    public SpotStatus Status { get; set; } = SpotStatus.Available;

    /// <summary>
    /// Manually-rotated token used as an EF Core concurrency check. A real rowversion column
    /// only works against SQL Server; this keeps the same optimistic-concurrency semantics
    /// portable to SQLite for the test suite.
    /// </summary>
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();
}
