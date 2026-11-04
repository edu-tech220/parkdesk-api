using System.ComponentModel.DataAnnotations;
using ParkDesk.Api.Entities;

namespace ParkDesk.Api.Dtos;

public record UpsertTariffRequest(
    [Required] SpotType SpotType,
    [Range(0, 10000)] decimal PricePerHour,
    [Range(0, 120)] int GraceMinutes,
    [Range(0, 100000)] decimal DailyCapAmount,
    [Range(0, 200)] decimal OverstayPenaltyPercent);

public record TariffResponse(
    Guid Id,
    Guid LotId,
    SpotType SpotType,
    decimal PricePerHour,
    int GraceMinutes,
    decimal DailyCapAmount,
    decimal OverstayPenaltyPercent);
