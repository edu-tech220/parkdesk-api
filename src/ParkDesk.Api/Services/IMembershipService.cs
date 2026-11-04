using ParkDesk.Api.Entities;

namespace ParkDesk.Api.Services;

public interface IMembershipService
{
    Task<List<MembershipPlan>> ListPlansAsync();

    Task<UserMembership> SubscribeAsync(Guid userId, Guid planId);
}
