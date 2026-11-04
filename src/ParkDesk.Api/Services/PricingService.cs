using ParkDesk.Api.Entities;

namespace ParkDesk.Api.Services;

public class PricingService : IPricingService
{
    public PricingResult Calculate(
        Tariff tariff,
        DateTime checkInAt,
        DateTime checkOutAt,
        decimal membershipDiscountPercent,
        DateTime? reservedUntil)
    {
        if (checkOutAt < checkInAt)
            throw new ArgumentException("checkOutAt must be on or after checkInAt.", nameof(checkOutAt));

        var totalMinutes = (checkOutAt - checkInAt).TotalMinutes;
        var billableMinutes = Math.Max(0, totalMinutes - tariff.GraceMinutes);
        var billableHours = (int)Math.Ceiling(billableMinutes / 60.0);
        var baseAmount = billableHours * tariff.PricePerHour;

        var totalDays = Math.Max(1, (int)Math.Ceiling((checkOutAt - checkInAt).TotalHours / 24.0));
        var dailyCap = tariff.DailyCapAmount * totalDays;
        if (dailyCap > 0 && baseAmount > dailyCap)
            baseAmount = dailyCap;

        if (membershipDiscountPercent > 0)
            baseAmount *= 1 - membershipDiscountPercent / 100m;

        var overstayFee = 0m;
        if (reservedUntil.HasValue && checkOutAt > reservedUntil.Value)
        {
            var overstayMinutes = (checkOutAt - reservedUntil.Value).TotalMinutes;
            var overstayHours = (int)Math.Ceiling(overstayMinutes / 60.0);
            overstayFee = overstayHours * tariff.PricePerHour * (tariff.OverstayPenaltyPercent / 100m);
        }

        baseAmount = Math.Round(baseAmount, 2, MidpointRounding.AwayFromZero);
        overstayFee = Math.Round(overstayFee, 2, MidpointRounding.AwayFromZero);

        return new PricingResult(baseAmount, overstayFee, baseAmount + overstayFee);
    }
}
