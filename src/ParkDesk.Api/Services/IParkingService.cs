using ParkDesk.Api.Common;
using ParkDesk.Api.Entities;

namespace ParkDesk.Api.Services;

public interface IParkingService
{
    Task<ParkingSession> CheckInWalkInAsync(Guid lotId, string plate, VehicleType vehicleType, Guid? userId);

    Task<ParkingSession> CheckInForReservationAsync(Reservation reservation, string plate, VehicleType vehicleType);

    Task<ParkingSession> CheckOutAsync(Guid sessionId);

    Task<ParkingSession?> GetByIdAsync(Guid sessionId);

    Task<PagedResult<ParkingSession>> ListSessionsAsync(Guid? lotId, SessionStatus? status, int page, int pageSize);
}
