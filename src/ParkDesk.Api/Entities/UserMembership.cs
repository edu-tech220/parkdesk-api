namespace ParkDesk.Api.Entities;

public class UserMembership
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid PlanId { get; set; }
    public MembershipPlan? Plan { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
}
