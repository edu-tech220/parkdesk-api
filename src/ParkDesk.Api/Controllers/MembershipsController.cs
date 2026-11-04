using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkDesk.Api.Dtos;
using ParkDesk.Api.Services;

namespace ParkDesk.Api.Controllers;

[ApiController]
[Route("api/memberships")]
public class MembershipsController : ControllerBase
{
    private readonly IMembershipService _membershipService;

    public MembershipsController(IMembershipService membershipService)
    {
        _membershipService = membershipService;
    }

    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<ActionResult<List<PlanResponse>>> GetPlans()
    {
        var plans = await _membershipService.ListPlansAsync();
        return Ok(plans.Select(p => new PlanResponse(p.Id, p.Name, p.DiscountPercent, p.MonthlyFee)));
    }

    [HttpPost("subscribe")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<MembershipResponse>> Subscribe(SubscribeRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var membership = await _membershipService.SubscribeAsync(userId, request.PlanId);
        return Ok(new MembershipResponse(membership.Id, membership.UserId, membership.PlanId, membership.ValidFrom, membership.ValidUntil));
    }
}
