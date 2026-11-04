using Microsoft.EntityFrameworkCore;
using ParkDesk.Api.Entities;
using ParkDesk.Api.Services;

namespace ParkDesk.Api.Data;

public static class DbSeeder
{
    /// <summary>
    /// Applies pending migrations and seeds baseline data. Only safe against a real relational
    /// provider (SQL Server); tests create their schema with EnsureCreated and call
    /// <see cref="SeedDataAsync"/> directly instead.
    /// </summary>
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher passwordHasher)
    {
        await db.Database.MigrateAsync();
        await SeedDataAsync(db, passwordHasher);
    }

    public static async Task SeedDataAsync(AppDbContext db, IPasswordHasher passwordHasher)
    {
        if (!await db.Users.AnyAsync(u => u.Role == UserRole.Admin))
        {
            var (hash, salt) = passwordHasher.Hash("Admin123!");
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@parkdesk.local",
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            });

            var (opHash, opSalt) = passwordHasher.Hash("Operator123!");
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = "operator@parkdesk.local",
                PasswordHash = opHash,
                PasswordSalt = opSalt,
                Role = UserRole.Operator,
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        if (!await db.MembershipPlans.AnyAsync())
        {
            db.MembershipPlans.AddRange(
                new MembershipPlan { Id = Guid.NewGuid(), Name = "Basico", DiscountPercent = 0, MonthlyFee = 0 },
                new MembershipPlan { Id = Guid.NewGuid(), Name = "Frecuente", DiscountPercent = 15, MonthlyFee = 199 },
                new MembershipPlan { Id = Guid.NewGuid(), Name = "Premium", DiscountPercent = 30, MonthlyFee = 399 }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.ParkingLots.AnyAsync())
        {
            var lot = new ParkingLot
            {
                Id = Guid.NewGuid(),
                Name = "Plaza Centro",
                Address = "Av. Reforma 123, CDMX"
            };
            db.ParkingLots.Add(lot);

            var spots = new List<ParkingSpot>();
            for (var i = 1; i <= 10; i++)
                spots.Add(new ParkingSpot { Id = Guid.NewGuid(), LotId = lot.Id, Code = $"R{i:00}", Type = SpotType.Regular });
            for (var i = 1; i <= 3; i++)
                spots.Add(new ParkingSpot { Id = Guid.NewGuid(), LotId = lot.Id, Code = $"C{i:00}", Type = SpotType.Compact });
            for (var i = 1; i <= 2; i++)
                spots.Add(new ParkingSpot { Id = Guid.NewGuid(), LotId = lot.Id, Code = $"E{i:00}", Type = SpotType.Electric });
            spots.Add(new ParkingSpot { Id = Guid.NewGuid(), LotId = lot.Id, Code = "D01", Type = SpotType.Disabled });
            db.ParkingSpots.AddRange(spots);

            db.Tariffs.AddRange(
                new Tariff { Id = Guid.NewGuid(), LotId = lot.Id, SpotType = SpotType.Regular, PricePerHour = 20, GraceMinutes = 15, DailyCapAmount = 150, OverstayPenaltyPercent = 50 },
                new Tariff { Id = Guid.NewGuid(), LotId = lot.Id, SpotType = SpotType.Compact, PricePerHour = 18, GraceMinutes = 15, DailyCapAmount = 130, OverstayPenaltyPercent = 50 },
                new Tariff { Id = Guid.NewGuid(), LotId = lot.Id, SpotType = SpotType.Electric, PricePerHour = 30, GraceMinutes = 10, DailyCapAmount = 200, OverstayPenaltyPercent = 50 },
                new Tariff { Id = Guid.NewGuid(), LotId = lot.Id, SpotType = SpotType.Disabled, PricePerHour = 15, GraceMinutes = 15, DailyCapAmount = 120, OverstayPenaltyPercent = 50 }
            );

            await db.SaveChangesAsync();
        }
    }
}
