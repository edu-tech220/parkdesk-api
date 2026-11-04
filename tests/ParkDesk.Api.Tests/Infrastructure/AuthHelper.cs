using System.Net.Http.Headers;
using System.Net.Http.Json;
using ParkDesk.Api.Dtos;

namespace ParkDesk.Api.Tests.Infrastructure;

public static class AuthHelper
{
    public static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return result!.Token;
    }

    public static async Task<(string Token, string Email)> RegisterCustomerAsync(HttpClient client, string? email = null)
    {
        email ??= $"customer-{Guid.NewGuid():N}@test.local";
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password123!"));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return (result!.Token, email);
    }

    public static void UseBearer(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public const string AdminEmail = "admin@parkdesk.local";
    public const string AdminPassword = "Admin123!";
    public const string OperatorEmail = "operator@parkdesk.local";
    public const string OperatorPassword = "Operator123!";
}
