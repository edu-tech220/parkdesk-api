using Microsoft.EntityFrameworkCore;
using ParkDesk.Api.Common;
using ParkDesk.Api.Data;
using ParkDesk.Api.Entities;

namespace ParkDesk.Api.Services;

public class ReservationService : IReservationService
{
    /// <summary>A no-show reservation is only released this long after its start time.</summary>
    private const int LateArrivalGraceMinutes = 30;

    /// <summary>A driver may not check in earlier than this many minutes before the window starts.</summary>
    private const int EarlyArrivalGraceMinutes = 30;

    private readonly AppDbContext _db;
    private readonly IParkingService _parkingService;

    public ReservationService(AppDbContext db, IParkingService parkingService)
    {
        _db = db;
        _parkingService = parkingService;
    }

    public async Task<Reservation> CreateAsync(Guid lotId, Guid userId, SpotType spotType, DateTime startTime, DateTime endTime)
    {
        if (startTime >= endTime)
            throw new BusinessRuleException("La hora de inicio debe ser anterior a la hora de fin.");

        if (startTime < DateTime.UtcNow.AddMinutes(-1))
            throw new BusinessRuleException("No se puede reservar en el pasado.");

        if (!await _db.ParkingLots.AnyAsync(l => l.Id == lotId))
            throw new NotFoundException("Estacionamiento no encontrado.");

        await ExpireStaleReservationsAsync(lotId);

        var totalSpotsOfType = await _db.ParkingSpots
            .CountAsync(s => s.LotId == lotId && s.Type == spotType && s.Status != SpotStatus.OutOfService);

        if (totalSpotsOfType == 0)
            throw new BusinessRuleException("El estacionamiento no tiene espacios de ese tipo.");

        var overlapping = await _db.Reservations.CountAsync(r =>
            r.LotId == lotId &&
            r.SpotType == spotType &&
            (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.CheckedIn) &&
            r.StartTime < endTime && startTime < r.EndTime);

        if (overlapping >= totalSpotsOfType)
            throw new ConflictException("No hay capacidad disponible para ese tipo de espacio en ese horario.");

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            LotId = lotId,
            UserId = userId,
            SpotType = spotType,
            StartTime = startTime,
            EndTime = endTime,
            Status = ReservationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();
        return reservation;
    }

    public async Task<ParkingSession> CheckInAsync(Guid reservationId, string plate, VehicleType vehicleType)
    {
        var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId)
            ?? throw new NotFoundException("Reserva no encontrada.");

        await ExpireIfStaleAsync(reservation);

        if (reservation.Status == ReservationStatus.Expired)
            throw new BusinessRuleException("La reserva expiro por no presentarse a tiempo.");

        if (reservation.Status != ReservationStatus.Pending)
            throw new BusinessRuleException($"La reserva no se puede registrar porque su estado es {reservation.Status}.");

        var now = DateTime.UtcNow;
        if (now < reservation.StartTime.AddMinutes(-EarlyArrivalGraceMinutes))
            throw new BusinessRuleException("Aun es demasiado pronto para registrar la entrada de esta reserva.");

        var session = await _parkingService.CheckInForReservationAsync(reservation, plate, vehicleType);

        reservation.Status = ReservationStatus.CheckedIn;
        reservation.SpotId = session.SpotId;
        reservation.SessionId = session.Id;
        await _db.SaveChangesAsync();

        return session;
    }

    public async Task<Reservation> CancelAsync(Guid reservationId, Guid requestingUserId, bool isPrivileged)
    {
        var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId)
            ?? throw new NotFoundException("Reserva no encontrada.");

        if (!isPrivileged && reservation.UserId != requestingUserId)
            throw new NotFoundException("Reserva no encontrada.");

        await ExpireIfStaleAsync(reservation);

        if (reservation.Status != ReservationStatus.Pending)
            throw new BusinessRuleException($"No se puede cancelar una reserva en estado {reservation.Status}.");

        reservation.Status = ReservationStatus.Cancelled;
        await _db.SaveChangesAsync();
        return reservation;
    }

    public async Task<Reservation> GetByIdAsync(Guid reservationId)
    {
        var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId)
            ?? throw new NotFoundException("Reserva no encontrada.");

        await ExpireIfStaleAsync(reservation);
        return reservation;
    }

    public async Task<PagedResult<Reservation>> ListAsync(Guid? lotId, Guid? userId, ReservationStatus? status, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        if (lotId.HasValue)
            await ExpireStaleReservationsAsync(lotId.Value);

        var query = _db.Reservations.AsNoTracking().AsQueryable();
        if (lotId.HasValue) query = query.Where(r => r.LotId == lotId.Value);
        if (userId.HasValue) query = query.Where(r => r.UserId == userId.Value);
        if (status.HasValue) query = query.Where(r => r.Status == status.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Reservation> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    private async Task ExpireIfStaleAsync(Reservation reservation)
    {
        if (reservation.Status != ReservationStatus.Pending)
            return;

        if (DateTime.UtcNow <= reservation.StartTime.AddMinutes(LateArrivalGraceMinutes))
            return;

        reservation.Status = ReservationStatus.Expired;
        await _db.SaveChangesAsync();
    }

    private async Task ExpireStaleReservationsAsync(Guid lotId)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-LateArrivalGraceMinutes);
        var stale = await _db.Reservations
            .Where(r => r.LotId == lotId && r.Status == ReservationStatus.Pending && r.StartTime < cutoff)
            .ToListAsync();

        if (stale.Count == 0) return;

        foreach (var r in stale) r.Status = ReservationStatus.Expired;
        await _db.SaveChangesAsync();
    }
}
