using Microsoft.EntityFrameworkCore;
using ParkDesk.Api.Entities;

namespace ParkDesk.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<ParkingLot> ParkingLots => Set<ParkingLot>();
    public DbSet<ParkingSpot> ParkingSpots => Set<ParkingSpot>();
    public DbSet<Tariff> Tariffs => Set<Tariff>();
    public DbSet<ParkingSession> ParkingSessions => Set<ParkingSession>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<MembershipPlan> MembershipPlans => Set<MembershipPlan>();
    public DbSet<UserMembership> UserMemberships => Set<UserMembership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasConversion<string>();
        });

        modelBuilder.Entity<ParkingLot>();

        modelBuilder.Entity<ParkingSpot>(e =>
        {
            e.HasOne(s => s.Lot).WithMany(l => l.Spots).HasForeignKey(s => s.LotId);
            e.Property(s => s.Type).HasConversion<string>();
            e.Property(s => s.Status).HasConversion<string>();
            e.Property(s => s.ConcurrencyStamp).IsConcurrencyToken();
            e.HasIndex(s => new { s.LotId, s.Code }).IsUnique();
        });

        modelBuilder.Entity<Tariff>(e =>
        {
            e.HasOne(t => t.Lot).WithMany(l => l.Tariffs).HasForeignKey(t => t.LotId);
            e.Property(t => t.SpotType).HasConversion<string>();
            e.Property(t => t.PricePerHour).HasPrecision(10, 2);
            e.Property(t => t.DailyCapAmount).HasPrecision(10, 2);
            e.Property(t => t.OverstayPenaltyPercent).HasPrecision(5, 2);
            e.HasIndex(t => new { t.LotId, t.SpotType }).IsUnique();
        });

        modelBuilder.Entity<ParkingSession>(e =>
        {
            e.HasOne(s => s.Lot).WithMany().HasForeignKey(s => s.LotId);
            e.HasOne(s => s.Spot).WithMany().HasForeignKey(s => s.SpotId);
            e.HasOne(s => s.Reservation).WithMany().HasForeignKey(s => s.ReservationId).IsRequired(false);
            e.Property(s => s.VehicleType).HasConversion<string>();
            e.Property(s => s.Status).HasConversion<string>();
            e.Property(s => s.AmountCharged).HasPrecision(10, 2);
            e.Property(s => s.OverstayFee).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Reservation>(e =>
        {
            e.HasOne(r => r.Lot).WithMany().HasForeignKey(r => r.LotId);
            e.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId);
            e.HasOne(r => r.Spot).WithMany().HasForeignKey(r => r.SpotId).IsRequired(false);
            e.Property(r => r.SpotType).HasConversion<string>();
            e.Property(r => r.Status).HasConversion<string>();
        });

        modelBuilder.Entity<MembershipPlan>(e =>
        {
            e.Property(p => p.DiscountPercent).HasPrecision(5, 2);
            e.Property(p => p.MonthlyFee).HasPrecision(10, 2);
        });

        modelBuilder.Entity<UserMembership>(e =>
        {
            e.HasOne(m => m.User).WithMany(u => u.Memberships).HasForeignKey(m => m.UserId);
            e.HasOne(m => m.Plan).WithMany().HasForeignKey(m => m.PlanId);
        });
    }
}
