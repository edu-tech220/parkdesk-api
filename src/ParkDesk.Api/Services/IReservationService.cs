using ParkDesk.Api.Common;
using ParkDesk.Api.Entities;

namespace ParkDesk.Api.Services;

public interface IReservationService
{
    Task<Reservation> CreateAsync(Guid lotId, Guid userId, SpotType spotType, DateTime startTime, DateTime endTime);

    Task<ParkingSession> CheckInAsync(Guid reservationId, string plate, VehicleType vehicleType);

    Task<Reservation> CancelAsync(Guid reservationId, Guid requestingUserId, bool isPrivileged);

    Task<Reservation> GetByIdAsync(Guid reservationId);

    Task<PagedResult<Reservation>> ListAsync(Guid? lotId, Guid? userId, ReservationStatus? status, int page, int pageSize);
}
