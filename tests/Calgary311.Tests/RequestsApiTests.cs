using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Calgary311.Web.Api;
using Calgary311.Web.Data;
using Calgary311.Web.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Calgary311.Tests;

// Integration tests: WebApplicationFactory starts the whole app in memory and HttpClient calls it like a real client.
// The app's connection string is swapped for a shared in-memory SQLite database seeded with a few requests.
public class RequestsApiTests : IDisposable
{
    private readonly SqliteConnection _keepAlive;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public RequestsApiTests()
    {
        // "Cache=Shared" lets the app's connections see this database; it lasts as long as one connection is open.
        var connectionString = $"DataSource=api-tests-{Guid.NewGuid()};Mode=Memory;Cache=Shared";
        _keepAlive = new SqliteConnection(connectionString);
        _keepAlive.Open();

        using (var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_keepAlive).Options))
        {
            db.Database.EnsureCreated();
            db.ServiceRequests.AddRange(
                Request("A", "PANORAMA HILLS", "Open", new DateTime(2026, 9, 1)),
                Request("B", "PANORAMA HILLS", "Closed", new DateTime(2026, 9, 2), closed: new DateTime(2026, 9, 5)),
                Request("C", "BOWNESS", "Open", new DateTime(2026, 9, 3)));
            db.SaveChanges();
        }

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder
                .UseSetting("ConnectionStrings:Default", connectionString)
                // Otherwise starting the app would sync from the real City API during tests.
                .UseSetting("OpenCalgary:AutoSync", "false"));
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _keepAlive.Dispose();
    }

    [Fact]
    public async Task List_FiltersByCommunityAndStatus_IgnoringCase()
    {
        var response = await _client.GetFromJsonAsync<PagedResponse<ServiceRequestDto>>(
            "/api/requests?community=Panorama Hills&status=closed");

        var request = Assert.Single(response!.Items);
        Assert.Equal("B", request.ServiceRequestId);
        Assert.Equal(3, request.DaysToClose);
        Assert.Equal(1, response.TotalCount);
    }

    [Fact]
    public async Task List_ReturnsNewestFirst_AndPages()
    {
        var response = await _client.GetFromJsonAsync<PagedResponse<ServiceRequestDto>>("/api/requests?pageSize=2&page=2");

        Assert.Equal(3, response!.TotalCount);
        Assert.Equal(2, response.TotalPages);
        Assert.Equal("A", Assert.Single(response.Items).ServiceRequestId);
    }

    [Fact]
    public async Task List_UsesCamelCaseJson_WithoutTheDatabaseKey()
    {
        var json = await _client.GetStringAsync("/api/requests?pageSize=1");
        var item = JsonDocument.Parse(json).RootElement.GetProperty("items")[0];

        Assert.True(item.TryGetProperty("serviceRequestId", out _));
        Assert.False(item.TryGetProperty("id", out _));
    }

    [Fact]
    public async Task List_ReturnsBadRequest_ForInvalidDate()
    {
        var response = await _client.GetAsync("/api/requests?from=not-a-date");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsTheRequest()
    {
        var request = await _client.GetFromJsonAsync<ServiceRequestDto>("/api/requests/C");

        Assert.Equal("BOWNESS", request!.CommunityName);
    }

    [Fact]
    public async Task Get_ReturnsNotFound_ForUnknownId()
    {
        var response = await _client.GetAsync("/api/requests/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static ServiceRequest Request(string id, string community, string status, DateTime requested, DateTime? closed = null) =>
        new()
        {
            ServiceRequestId = id,
            ServiceName = "Roads - Pothole Repair",
            CommunityName = community,
            Status = status,
            RequestedDate = requested,
            ClosedDate = closed
        };
}
