using Microsoft.EntityFrameworkCore;
using ParkDesk.Api.Common;
using ParkDesk.Api.Data;
using ParkDesk.Api.Entities;

namespace ParkDesk.Api.Services;

public class MembershipService : IMembershipService
{
    private readonly AppDbContext _db;

    public MembershipService(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<MembershipPlan>> ListPlansAsync() => _db.MembershipPlans.AsNoTracking().ToListAsync();

    public async Task<UserMembership> SubscribeAsync(Guid userId, Guid planId)
    {
        var plan = await _db.MembershipPlans.FindAsync(planId)
            ?? throw new NotFoundException("Plan de membresia no encontrado.");

        var now = DateTime.UtcNow;
        var active = await _db.UserMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.ValidFrom <= now && m.ValidUntil >= now);

        if (active is not null)
            throw new ConflictException("El usuario ya tiene una membresia activa.");

        var membership = new UserMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = plan.Id,
            ValidFrom = now,
            ValidUntil = now.AddDays(30)
        };

        _db.UserMemberships.Add(membership);
        await _db.SaveChangesAsync();
        return membership;
    }
}
