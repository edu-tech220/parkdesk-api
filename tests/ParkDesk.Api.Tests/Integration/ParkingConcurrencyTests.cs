using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ParkDesk.Api.Common;
using ParkDesk.Api.Entities;
using ParkDesk.Api.Services;
using ParkDesk.Api.Tests.Infrastructure;

namespace ParkDesk.Api.Tests.Integration;

public class ParkingConcurrencyTests : IClassFixture<ParkDeskWebApplicationFactory>
{
    private readonly ParkDeskWebApplicationFactory _factory;

    public ParkingConcurrencyTests(ParkDeskWebApplicationFactory factory)
    {
        _factory = factory;
        _ = _factory.Server; // forces ConfigureWebHost to run so the schema exists before we open raw connections.
    }

    [Fact]
    public async Task CheckInWalkIn_UnderConcurrentLoad_NeverDoubleAssignsASpot()
    {
        const int totalSpots = 3;
        const int concurrentDrivers = 10;

        var lotId = Guid.NewGuid();
        await using (var setupDb = _factory.CreateDbContext())
        {
            setupDb.ParkingLots.Add(new ParkingLot { Id = lotId, Name = "Lot concurrency", Address = "Test" });
            for (var i = 0; i < totalSpots; i++)
                setupDb.ParkingSpots.Add(new ParkingSpot { Id = Guid.NewGuid(), LotId = lotId, Code = $"X{i:00}", Type = SpotType.Regular });
            await setupDb.SaveChangesAsync();
        }

        var tasks = Enumerable.Range(0, concurrentDrivers).Select(async i =>
        {
            await using var db = _factory.CreateDbContext();
            var service = new ParkingService(db, new PricingService());
            try
            {
                var session = await service.CheckInWalkInAsync(lotId, $"CONC{i:00}", VehicleType.Car, null);
                return (Success: true, session.SpotId);
            }
            catch (ConflictException)
            {
                return (Success: false, SpotId: (Guid?)null);
            }
        });

        var results = await Task.WhenAll(tasks);

        results.Count(r => r.Success).Should().Be(totalSpots);
        results.Count(r => !r.Success).Should().Be(concurrentDrivers - totalSpots);
        results.Where(r => r.Success).Select(r => r.SpotId).Should().OnlyHaveUniqueItems();

        await using var verifyDb = _factory.CreateDbContext();
        var occupied = await verifyDb.ParkingSpots.CountAsync(s => s.LotId == lotId && s.Status == SpotStatus.Occupied);
        occupied.Should().Be(totalSpots);
    }
}
