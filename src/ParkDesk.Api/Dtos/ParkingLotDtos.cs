using System.ComponentModel.DataAnnotations;

namespace ParkDesk.Api.Dtos;

public record CreateLotRequest(
    [Required, MaxLength(100)] string Name,
    [Required, MaxLength(200)] string Address);

public record LotResponse(Guid Id, string Name, string Address, int TotalSpots, int AvailableSpots);
