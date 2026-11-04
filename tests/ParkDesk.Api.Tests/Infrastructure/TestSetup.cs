using System.Net.Http.Json;
using ParkDesk.Api.Dtos;
using ParkDesk.Api.Entities;

namespace ParkDesk.Api.Tests.Infrastructure;

public static class TestSetup
{
    public static async Task<HttpClient> CreateAdminClientAsync(ParkDeskWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        var token = await AuthHelper.LoginAsync(client, AuthHelper.AdminEmail, AuthHelper.AdminPassword);
        client.UseBearer(token);
        return client;
    }

    public static async Task<HttpClient> CreateOperatorClientAsync(ParkDeskWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        var token = await AuthHelper.LoginAsync(client, AuthHelper.OperatorEmail, AuthHelper.OperatorPassword);
        client.UseBearer(token);
        return client;
    }

    public static async Task<Guid> CreateLotAsync(
        HttpClient adminClient,
        int regularSpots = 1,
        decimal pricePerHour = 20m,
        int graceMinutes = 15,
        decimal dailyCap = 150m,
        decimal overstayPenaltyPercent = 50m,
        SpotType spotType = SpotType.Regular)
    {
        var lotResponse = await adminClient.PostAsJsonAsync("/api/lots", new CreateLotRequest($"Lot-{Guid.NewGuid():N}", "Calle de prueba 1"));
        lotResponse.EnsureSuccessStatusCode();
        var lot = await lotResponse.Content.ReadFromJsonAsync<LotResponse>();

        for (var i = 0; i < regularSpots; i++)
        {
            var spotResponse = await adminClient.PostAsJsonAsync($"/api/lots/{lot!.Id}/spots", new CreateSpotRequest($"S{i:00}", spotType));
            spotResponse.EnsureSuccessStatusCode();
        }

        var tariffResponse = await adminClient.PutAsJsonAsync(
            $"/api/lots/{lot!.Id}/tariffs",
            new UpsertTariffRequest(spotType, pricePerHour, graceMinutes, dailyCap, overstayPenaltyPercent));
        tariffResponse.EnsureSuccessStatusCode();

        return lot.Id;
    }
}
