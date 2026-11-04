using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using ParkDesk.Api.Dtos;
using ParkDesk.Api.Tests.Infrastructure;

namespace ParkDesk.Api.Tests.Integration;

public class RateLimitingTests : IClassFixture<ParkDeskWebApplicationFactory>
{
    private readonly ParkDeskWebApplicationFactory _factory;

    public RateLimitingTests(ParkDeskWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_BeyondThePermitLimit_IsThrottledWith429()
    {
        var throttledFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:Login:PermitLimit"] = "2"
                });
            });
        });

        var client = throttledFactory.CreateClient();
        var request = new LoginRequest(AuthHelper.AdminEmail, "wrong-password");

        var first = await client.PostAsJsonAsync("/api/auth/login", request);
        var second = await client.PostAsJsonAsync("/api/auth/login", request);
        var third = await client.PostAsJsonAsync("/api/auth/login", request);

        first.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        third.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
