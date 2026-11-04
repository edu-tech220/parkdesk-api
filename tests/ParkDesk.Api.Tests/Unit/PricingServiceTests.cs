using FluentAssertions;
using ParkDesk.Api.Entities;
using ParkDesk.Api.Services;

namespace ParkDesk.Api.Tests.Unit;

public class PricingServiceTests
{
    private readonly PricingService _sut = new();

    private static Tariff RegularTariff() => new()
    {
        SpotType = SpotType.Regular,
        PricePerHour = 20m,
        GraceMinutes = 15,
        DailyCapAmount = 150m,
        OverstayPenaltyPercent = 50m
    };

    [Fact]
    public void Calculate_WithinGracePeriod_IsFree()
    {
        var checkIn = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var checkOut = checkIn.AddMinutes(10);

        var result = _sut.Calculate(RegularTariff(), checkIn, checkOut, 0, null);

        result.BaseAmount.Should().Be(0m);
        result.Total.Should().Be(0m);
    }

    [Fact]
    public void Calculate_RoundsUpToTheNextFullHour()
    {
        var checkIn = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var checkOut = checkIn.AddHours(1).AddMinutes(20);

        var result = _sut.Calculate(RegularTariff(), checkIn, checkOut, 0, null);

        // 1h20m - 15m grace = 1h05m billable -> rounds up to 2 billable hours.
        result.BaseAmount.Should().Be(40m);
    }

    [Fact]
    public void Calculate_AppliesDailyCap()
    {
        var checkIn = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var checkOut = checkIn.AddHours(12);

        var result = _sut.Calculate(RegularTariff(), checkIn, checkOut, 0, null);

        result.BaseAmount.Should().Be(150m);
    }

    [Fact]
    public void Calculate_AppliesMembershipDiscountAfterCap()
    {
        var checkIn = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var checkOut = checkIn.AddHours(2);

        var result = _sut.Calculate(RegularTariff(), checkIn, checkOut, 15m, null);

        // 2h * 20 = 40, -15% = 34
        result.BaseAmount.Should().Be(34m);
    }

    [Fact]
    public void Calculate_ChargesOverstayFeeWhenCheckoutIsAfterReservationWindow()
    {
        var checkIn = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var reservedUntil = checkIn.AddHours(1);
        var checkOut = reservedUntil.AddMinutes(30);

        var result = _sut.Calculate(RegularTariff(), checkIn, checkOut, 0, reservedUntil);

        // Base: 1h30m - 15m grace = 1h15m -> 2 billable hours * 20 = 40
        result.BaseAmount.Should().Be(40m);
        // Overstay: 30 minutes past reservation -> 1 billable hour * 20 * 50% = 10
        result.OverstayFee.Should().Be(10m);
        result.Total.Should().Be(50m);
    }

    [Fact]
    public void Calculate_NoOverstayFeeWhenCheckoutIsWithinReservationWindow()
    {
        var checkIn = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var reservedUntil = checkIn.AddHours(2);
        var checkOut = checkIn.AddHours(1);

        var result = _sut.Calculate(RegularTariff(), checkIn, checkOut, 0, reservedUntil);

        result.OverstayFee.Should().Be(0m);
    }

    [Fact]
    public void Calculate_Throws_WhenCheckOutIsBeforeCheckIn()
    {
        var checkIn = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var act = () => _sut.Calculate(RegularTariff(), checkIn, checkIn.AddMinutes(-1), 0, null);

        act.Should().Throw<ArgumentException>();
    }
}
