using System.Net.Http.Json;
using FluentAssertions;
using ParkDesk.Api.Common;
using ParkDesk.Api.Dtos;
using ParkDesk.Api.Entities;
using ParkDesk.Api.Tests.Infrastructure;

namespace ParkDesk.Api.Tests.Integration;

public class PaginationTests : IClassFixture<ParkDeskWebApplicationFactory>
{
    private readonly ParkDeskWebApplicationFactory _factory;

    public PaginationTests(ParkDeskWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task ListSessions_ReturnsPagedResultsRespectingPageSize()
    {
        var admin = await TestSetup.CreateAdminClientAsync(_factory);
        var lotId = await TestSetup.CreateLotAsync(admin, regularSpots: 5);
        var operatorClient = await TestSetup.CreateOperatorClientAsync(_factory);

        const int totalCars = 5;
        for (var i = 0; i < totalCars; i++)
        {
            (await operatorClient.PostAsJsonAsync($"/api/lots/{lotId}/sessions/checkin", new CheckInRequest($"PAGE{i:00}", VehicleType.Car)))
                .EnsureSuccessStatusCode();
        }

        var firstPage = await operatorClient.GetFromJsonAsync<PagedResult<SessionResponse>>(
            $"/api/sessions?lotId={lotId}&page=1&pageSize=2");

        firstPage!.Items.Should().HaveCount(2);
        firstPage.TotalCount.Should().Be(totalCars);
        firstPage.TotalPages.Should().Be(3);

        var lastPage = await operatorClient.GetFromJsonAsync<PagedResult<SessionResponse>>(
            $"/api/sessions?lotId={lotId}&page=3&pageSize=2");

        lastPage!.Items.Should().HaveCount(1);
    }
}
