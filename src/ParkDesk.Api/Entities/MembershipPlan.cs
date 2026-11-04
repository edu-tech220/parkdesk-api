namespace ParkDesk.Api.Entities;

public class MembershipPlan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; }
    public decimal MonthlyFee { get; set; }
}
