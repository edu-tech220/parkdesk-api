using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ParkDesk.Api.Dtos;
using ParkDesk.Api.Entities;
using ParkDesk.Api.Tests.Infrastructure;

namespace ParkDesk.Api.Tests.Integration;

public class ReservationsFlowTests : IClassFixture<ParkDeskWebApplicationFactory>
{
    private readonly ParkDeskWebApplicationFactory _factory;

    public ReservationsFlowTests(ParkDeskWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Create_ThenCheckIn_AssignsASpotAndMarksCheckedIn()
    {
        var admin = await TestSetup.CreateAdminClientAsync(_factory);
        var lotId = await TestSetup.CreateLotAsync(admin, regularSpots: 1);

        var client = _factory.CreateClient();
        var (token, email) = await AuthHelper.RegisterCustomerAsync(client);
        client.UseBearer(token);

        var start = DateTime.UtcNow.AddMinutes(5);
        var end = start.AddHours(2);
        var createResponse = await client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest(lotId, SpotType.Regular, start, end));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var reservation = await createResponse.Content.ReadFromJsonAsync<ReservationResponse>();
        reservation!.Status.Should().Be(ReservationStatus.Pending);

        var operatorClient = await TestSetup.CreateOperatorClientAsync(_factory);
        var checkInResponse = await operatorClient.PostAsJsonAsync(
            $"/api/reservations/{reservation.Id}/checkin", new ReservationCheckInRequest("RES0001", VehicleType.Car));
        checkInResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var session = await checkInResponse.Content.ReadFromJsonAsync<SessionResponse>();
        session!.ReservationId.Should().Be(reservation.Id);

        var getResponse = await client.GetAsync($"/api/reservations/{reservation.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<ReservationResponse>();
        updated!.Status.Should().Be(ReservationStatus.CheckedIn);
    }

    [Fact]
    public async Task Create_WhenNoCapacityLeftForOverlappingWindow_ReturnsConflict()
    {
        var admin = await TestSetup.CreateAdminClientAsync(_factory);
        var lotId = await TestSetup.CreateLotAsync(admin, regularSpots: 1);

        var client = _factory.CreateClient();
        var (token, _) = await AuthHelper.RegisterCustomerAsync(client);
        client.UseBearer(token);

        var start = DateTime.UtcNow.AddHours(1);
        var end = start.AddHours(1);

        (await client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest(lotId, SpotType.Regular, start, end)))
            .EnsureSuccessStatusCode();

        // Overlaps the first reservation's window but there is only one Regular spot.
        var overlappingResponse = await client.PostAsJsonAsync(
            "/api/reservations", new CreateReservationRequest(lotId, SpotType.Regular, start.AddMinutes(30), end.AddMinutes(30)));

        overlappingResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_NonOverlappingWindow_Succeeds()
    {
        var admin = await TestSetup.CreateAdminClientAsync(_factory);
        var lotId = await TestSetup.CreateLotAsync(admin, regularSpots: 1);

        var client = _factory.CreateClient();
        var (token, _) = await AuthHelper.RegisterCustomerAsync(client);
        client.UseBearer(token);

        var start = DateTime.UtcNow.AddHours(1);
        var end = start.AddHours(1);

        (await client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest(lotId, SpotType.Regular, start, end)))
            .EnsureSuccessStatusCode();

        var secondResponse = await client.PostAsJsonAsync(
            "/api/reservations", new CreateReservationRequest(lotId, SpotType.Regular, end.AddMinutes(5), end.AddHours(1)));

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Cancel_APendingReservation_MarksItCancelled()
    {
        var admin = await TestSetup.CreateAdminClientAsync(_factory);
        var lotId = await TestSetup.CreateLotAsync(admin, regularSpots: 1);

        var client = _factory.CreateClient();
        var (token, _) = await AuthHelper.RegisterCustomerAsync(client);
        client.UseBearer(token);

        var start = DateTime.UtcNow.AddHours(1);
        var createResponse = await client.PostAsJsonAsync(
            "/api/reservations", new CreateReservationRequest(lotId, SpotType.Regular, start, start.AddHours(1)));
        var reservation = await createResponse.Content.ReadFromJsonAsync<ReservationResponse>();

        var cancelResponse = await client.PostAsync($"/api/reservations/{reservation!.Id}/cancel", null);

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<ReservationResponse>();
        cancelled!.Status.Should().Be(ReservationStatus.Cancelled);
    }

    [Fact]
    public async Task AnotherCustomer_CannotSeeOrCancelSomeoneElsesReservation()
    {
        var admin = await TestSetup.CreateAdminClientAsync(_factory);
        var lotId = await TestSetup.CreateLotAsync(admin, regularSpots: 1);

        var ownerClient = _factory.CreateClient();
        var (ownerToken, _) = await AuthHelper.RegisterCustomerAsync(ownerClient);
        ownerClient.UseBearer(ownerToken);

        var start = DateTime.UtcNow.AddHours(1);
        var createResponse = await ownerClient.PostAsJsonAsync(
            "/api/reservations", new CreateReservationRequest(lotId, SpotType.Regular, start, start.AddHours(1)));
        var reservation = await createResponse.Content.ReadFromJsonAsync<ReservationResponse>();

        var strangerClient = _factory.CreateClient();
        var (strangerToken, _) = await AuthHelper.RegisterCustomerAsync(strangerClient);
        strangerClient.UseBearer(strangerToken);

        var getResponse = await strangerClient.GetAsync($"/api/reservations/{reservation!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var cancelResponse = await strangerClient.PostAsync($"/api/reservations/{reservation.Id}/cancel", null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
