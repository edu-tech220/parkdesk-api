using System.ComponentModel.DataAnnotations;

namespace ParkDesk.Api.Dtos;

public record PlanResponse(Guid Id, string Name, decimal DiscountPercent, decimal MonthlyFee);

public record SubscribeRequest([Required] Guid PlanId);

public record MembershipResponse(Guid Id, Guid UserId, Guid PlanId, DateTime ValidFrom, DateTime ValidUntil);
