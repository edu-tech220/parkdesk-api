namespace ParkDesk.Api.Services;

public record AuthResult(string Token, DateTime ExpiresAt, string Email, string Role);

public interface IAuthService
{
    Task<AuthResult> RegisterCustomerAsync(string email, string password);
    Task<AuthResult> LoginAsync(string email, string password);
}
