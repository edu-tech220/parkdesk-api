using System.ComponentModel.DataAnnotations;
using ParkDesk.Api.Entities;

namespace ParkDesk.Api.Dtos;

public record CreateSpotRequest(
    [Required, MaxLength(10)] string Code,
    [Required] SpotType Type);

public record SpotResponse(Guid Id, Guid LotId, string Code, SpotType Type, SpotStatus Status);
