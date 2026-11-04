using ParkDesk.Api.Entities;

namespace ParkDesk.Api.Services;

public record PricingResult(decimal BaseAmount, decimal OverstayFee, decimal Total);

public interface IPricingService
{
    /// <summary>
    /// Calculates the fee for a parking session.
    /// </summary>
    /// <param name="tariff">Tariff applicable to the spot type used.</param>
    /// <param name="checkInAt">When the vehicle entered the lot.</param>
    /// <param name="checkOutAt">When the vehicle left the lot.</param>
    /// <param name="membershipDiscountPercent">Discount (0-100) granted by the driver's active membership, if any.</param>
    /// <param name="reservedUntil">
    /// End of the reservation window that was honored for this session, if it originated
    /// from a reservation. A checkout past this time triggers the tariff's overstay penalty.
    /// </param>
    PricingResult Calculate(
        Tariff tariff,
        DateTime checkInAt,
        DateTime checkOutAt,
        decimal membershipDiscountPercent,
        DateTime? reservedUntil);
}
