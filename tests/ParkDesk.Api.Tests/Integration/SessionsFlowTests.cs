using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ParkDesk.Api.Dtos;
using ParkDesk.Api.Entities;
using ParkDesk.Api.Tests.Infrastructure;

namespace ParkDesk.Api.Tests.Integration;

public class SessionsFlowTests : IClassFixture<ParkDeskWebApplicationFactory>
{
    private readonly ParkDeskWebApplicationFactory _factory;

    public SessionsFlowTests(ParkDeskWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task CheckIn_ThenCheckOut_ChargesExpectedAmount()
    {
        var admin = await TestSetup.CreateAdminClientAsync(_factory);
        var lotId = await TestSetup.CreateLotAsync(admin, regularSpots: 1, pricePerHour: 20m, graceMinutes: 15);

        var operatorClient = await TestSetup.CreateOperatorClientAsync(_factory);

        var checkInResponse = await operatorClient.PostAsJsonAsync(
            $"/api/lots/{lotId}/sessions/checkin", new CheckInRequest("ABC123", VehicleType.Car));
        checkInResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var session = await checkInResponse.Content.ReadFromJsonAsync<SessionResponse>();
        session!.Status.Should().Be(SessionStatus.Active);

        var checkOutResponse = await operatorClient.PostAsync($"/api/sessions/{session.Id}/checkout", null);
        checkOutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var closed = await checkOutResponse.Content.ReadFromJsonAsync<SessionResponse>();
        closed!.Status.Should().Be(SessionStatus.Completed);
        closed.AmountCharged.Should().Be(0m); // checked out almost immediately, within the grace period
    }

    [Fact]
    public async Task CheckIn_SamePlateTwice_ReturnsConflict()
    {
        var admin = await TestSetup.CreateAdminClientAsync(_factory);
        var lotId = await TestSetup.CreateLotAsync(admin, regularSpots: 2);
        var operatorClient = await TestSetup.CreateOperatorClientAsync(_factory);

        (await operatorClient.PostAsJsonAsync($"/api/lots/{lotId}/sessions/checkin", new CheckInRequest("REPEAT1", VehicleType.Car)))
            .EnsureSuccessStatusCode();

        var secondAttempt = await operatorClient.PostAsJsonAsync($"/api/lots/{lotId}/sessions/checkin", new CheckInRequest("REPEAT1", VehicleType.Car));

        secondAttempt.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CheckIn_WhenLotIsFull_ReturnsConflict()
    {
        var admin = await TestSetup.CreateAdminClientAsync(_factory);
        var lotId = await TestSetup.CreateLotAsync(admin, regularSpots: 1);
        var operatorClient = await TestSetup.CreateOperatorClientAsync(_factory);

        (await operatorClient.PostAsJsonAsync($"/api/lots/{lotId}/sessions/checkin", new CheckInRequest("FIRST01", VehicleType.Car)))
            .EnsureSuccessStatusCode();

        var secondAttempt = await operatorClient.PostAsJsonAsync($"/api/lots/{lotId}/sessions/checkin", new CheckInRequest("SECOND1", VehicleType.Car));

        secondAttempt.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CheckOut_ANonExistentSession_ReturnsNotFound()
    {
        var operatorClient = await TestSetup.CreateOperatorClientAsync(_factory);

        var response = await operatorClient.PostAsync($"/api/sessions/{Guid.NewGuid()}/checkout", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CustomerCannotCheckInVehicles()
    {
        var client = _factory.CreateClient();
        var (token, _) = await AuthHelper.RegisterCustomerAsync(client);
        client.UseBearer(token);

        var admin = await TestSetup.CreateAdminClientAsync(_factory);
        var lotId = await TestSetup.CreateLotAsync(admin);

        var response = await client.PostAsJsonAsync($"/api/lots/{lotId}/sessions/checkin", new CheckInRequest("NOPE001", VehicleType.Car));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
