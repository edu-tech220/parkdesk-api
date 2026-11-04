using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ParkDesk.Api.Dtos;
using ParkDesk.Api.Tests.Infrastructure;

namespace ParkDesk.Api.Tests.Integration;

public class AuthFlowTests : IClassFixture<ParkDeskWebApplicationFactory>
{
    private readonly ParkDeskWebApplicationFactory _factory;

    public AuthFlowTests(ParkDeskWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_ThenLogin_ReturnsValidToken()
    {
        var client = _factory.CreateClient();
        var email = $"new-{Guid.NewGuid():N}@test.local";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password123!"));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Password123!"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        auth!.Token.Should().NotBeNullOrWhiteSpace();
        auth.Role.Should().Be("Customer");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        var email = $"dup-{Guid.NewGuid():N}@test.local";

        (await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password123!"))).EnsureSuccessStatusCode();
        var secondResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password123!"));

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(AuthHelper.AdminEmail, "wrong-password"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/lots", new CreateLotRequest("Lot", "Address"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminOnlyEndpoint_WithCustomerToken_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        var (token, _) = await AuthHelper.RegisterCustomerAsync(client);
        client.UseBearer(token);

        var response = await client.PostAsJsonAsync("/api/lots", new CreateLotRequest("Lot", "Address"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
