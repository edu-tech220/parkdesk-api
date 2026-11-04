using System.ComponentModel.DataAnnotations;

namespace ParkDesk.Api.Dtos;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password);

public record LoginRequest(
    [Required] string Email,
    [Required] string Password);

public record AuthResponse(string Token, DateTime ExpiresAt, string Email, string Role);
