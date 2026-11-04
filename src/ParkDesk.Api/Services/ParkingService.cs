using Microsoft.EntityFrameworkCore;
using ParkDesk.Api.Common;
using ParkDesk.Api.Data;
using ParkDesk.Api.Entities;

namespace ParkDesk.Api.Services;

public class ParkingService : IParkingService
{
    /// <summary>
    /// Spot types a vehicle may use, in order of preference. Disabled spots are reserved for
    /// permit holders and are only ever assigned through an explicit reservation, never to a
    /// walk-in, so they are intentionally absent from this table.
    /// </summary>
    private static readonly Dictionary<VehicleType, SpotType[]> CompatibleTypes = new()
    {
        [VehicleType.Car] = new[] { SpotType.Regular, SpotType.Compact },
        [VehicleType.Motorcycle] = new[] { SpotType.Compact, SpotType.Regular },
        [VehicleType.Electric] = new[] { SpotType.Electric, SpotType.Regular }
    };

    private const int MaxClaimAttemptsPerSpot = 3;

    private readonly AppDbContext _db;
    private readonly IPricingService _pricingService;

    public ParkingService(AppDbContext db, IPricingService pricingService)
    {
        _db = db;
        _pricingService = pricingService;
    }

    public async Task<ParkingSession> CheckInWalkInAsync(Guid lotId, string plate, VehicleType vehicleType, Guid? userId)
    {
        if (string.IsNullOrWhiteSpace(plate))
            throw new BusinessRuleException("La placa es obligatoria.");

        var normalizedPlate = plate.Trim().ToUpperInvariant();

        if (!await _db.ParkingLots.AnyAsync(l => l.Id == lotId))
            throw new NotFoundException("Estacionamiento no encontrado.");

        await EnsureNoActiveSessionAsync(lotId, normalizedPlate);

        var spot = await AssignSpotAsync(lotId, CompatibleTypes[vehicleType]);
        if (spot is null)
            throw new ConflictException("No hay espacios disponibles para este tipo de vehiculo.");

        return await CreateSessionAsync(lotId, spot, normalizedPlate, vehicleType, userId, reservationId: null);
    }

    public async Task<ParkingSession> CheckInForReservationAsync(Reservation reservation, string plate, VehicleType vehicleType)
    {
        if (string.IsNullOrWhiteSpace(plate))
            throw new BusinessRuleException("La placa es obligatoria.");

        var normalizedPlate = plate.Trim().ToUpperInvariant();
        await EnsureNoActiveSessionAsync(reservation.LotId, normalizedPlate);

        var spot = await AssignSpotAsync(reservation.LotId, new[] { reservation.SpotType });
        if (spot is null)
            throw new ConflictException("No hay espacios disponibles para el tipo reservado en este momento.");

        return await CreateSessionAsync(reservation.LotId, spot, normalizedPlate, vehicleType, reservation.UserId, reservation.Id);
    }

    public async Task<ParkingSession> CheckOutAsync(Guid sessionId)
    {
        var session = await _db.ParkingSessions
            .Include(s => s.Reservation)
            .FirstOrDefaultAsync(s => s.Id == sessionId)
            ?? throw new NotFoundException("Sesion no encontrada.");

        if (session.Status != SessionStatus.Active)
            throw new BusinessRuleException("La sesion ya fue cerrada.");

        var spot = await _db.ParkingSpots.FirstOrDefaultAsync(s => s.Id == session.SpotId)
            ?? throw new NotFoundException("Espacio no encontrado.");

        var tariff = await _db.Tariffs.FirstOrDefaultAsync(t => t.LotId == session.LotId && t.SpotType == spot.Type)
            ?? throw new BusinessRuleException("No hay tarifa configurada para este tipo de espacio.");

        var checkOutAt = DateTime.UtcNow;
        var discountPercent = await GetActiveDiscountPercentAsync(session.UserId);
        var reservedUntil = session.Reservation?.EndTime;

        var pricing = _pricingService.Calculate(tariff, session.CheckInAt, checkOutAt, discountPercent, reservedUntil);

        session.CheckOutAt = checkOutAt;
        session.Status = SessionStatus.Completed;
        session.AmountCharged = pricing.BaseAmount;
        session.OverstayFee = pricing.OverstayFee;

        spot.Status = SpotStatus.Available;
        spot.ConcurrencyStamp = Guid.NewGuid();

        if (session.Reservation is { Status: ReservationStatus.CheckedIn } reservation)
            reservation.Status = ReservationStatus.Completed;

        await _db.SaveChangesAsync();
        return session;
    }

    public Task<ParkingSession?> GetByIdAsync(Guid sessionId) =>
        _db.ParkingSessions.AsNoTracking().Include(s => s.Spot).FirstOrDefaultAsync(s => s.Id == sessionId);

    public async Task<PagedResult<ParkingSession>> ListSessionsAsync(Guid? lotId, SessionStatus? status, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.ParkingSessions.AsNoTracking().Include(s => s.Spot).AsQueryable();
        if (lotId.HasValue) query = query.Where(s => s.LotId == lotId.Value);
        if (status.HasValue) query = query.Where(s => s.Status == status.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.CheckInAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ParkingSession> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    private async Task EnsureNoActiveSessionAsync(Guid lotId, string normalizedPlate)
    {
        var hasActive = await _db.ParkingSessions.AnyAsync(s =>
            s.LotId == lotId && s.Plate == normalizedPlate && s.Status == SessionStatus.Active);

        if (hasActive)
            throw new ConflictException("El vehiculo ya tiene una sesion activa en este estacionamiento.");
    }

    private async Task<ParkingSession> CreateSessionAsync(
        Guid lotId, ParkingSpot spot, string plate, VehicleType vehicleType, Guid? userId, Guid? reservationId)
    {
        var session = new ParkingSession
        {
            Id = Guid.NewGuid(),
            LotId = lotId,
            SpotId = spot.Id,
            Spot = spot,
            Plate = plate,
            VehicleType = vehicleType,
            CheckInAt = DateTime.UtcNow,
            Status = SessionStatus.Active,
            UserId = userId,
            ReservationId = reservationId
        };

        _db.ParkingSessions.Add(session);
        await _db.SaveChangesAsync();
        return session;
    }

    private async Task<decimal> GetActiveDiscountPercentAsync(Guid? userId)
    {
        if (userId is null) return 0;

        var now = DateTime.UtcNow;
        var bestDiscount = await _db.UserMemberships
            .Include(m => m.Plan)
            .Where(m => m.UserId == userId && m.ValidFrom <= now && m.ValidUntil >= now)
            .Select(m => m.Plan!.DiscountPercent)
            .OrderByDescending(d => d)
            .FirstOrDefaultAsync();

        return bestDiscount;
    }

    /// <summary>
    /// Finds a free spot of one of the given types (in priority order) and atomically marks it
    /// Occupied. Candidates are tried one at a time with an optimistic-concurrency check so that
    /// two requests racing for the same spot never both succeed; the loser simply moves on to the
    /// next free spot instead of failing the whole check-in.
    /// </summary>
    private async Task<ParkingSpot?> AssignSpotAsync(Guid lotId, IReadOnlyList<SpotType> candidateTypesInPriorityOrder)
    {
        foreach (var type in candidateTypesInPriorityOrder)
        {
            var candidateIds = await _db.ParkingSpots
                .AsNoTracking()
                .Where(s => s.LotId == lotId && s.Type == type && s.Status == SpotStatus.Available)
                .OrderBy(s => s.Code)
                .Select(s => s.Id)
                .ToListAsync();

            foreach (var spotId in candidateIds)
            {
                var claimed = await TryClaimSpotAsync(spotId);
                if (claimed is not null)
                    return claimed;
            }
        }

        return null;
    }

    private async Task<ParkingSpot?> TryClaimSpotAsync(Guid spotId)
    {
        for (var attempt = 0; attempt < MaxClaimAttemptsPerSpot; attempt++)
        {
            var spot = await _db.ParkingSpots.FirstOrDefaultAsync(s => s.Id == spotId);
            if (spot is null || spot.Status != SpotStatus.Available)
                return null;

            spot.Status = SpotStatus.Occupied;
            spot.ConcurrencyStamp = Guid.NewGuid();

            try
            {
                await _db.SaveChangesAsync();
                return spot;
            }
            catch (DbUpdateConcurrencyException)
            {
                _db.Entry(spot).State = EntityState.Detached;
            }
            catch (DbUpdateException)
            {
                // Transient failure (e.g. SQLite reporting the database as locked under
                // concurrent writers). Detach and retry a bounded number of times.
                _db.Entry(spot).State = EntityState.Detached;
                await Task.Delay(Random.Shared.Next(10, 40));
            }
        }

        return null;
    }
}
