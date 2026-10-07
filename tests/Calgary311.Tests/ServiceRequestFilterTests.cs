using Calgary311.Web.Data;
using Calgary311.Web.Models;
using Calgary311.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Calgary311.Tests;

// Runs the filters against a real (in-memory) SQLite database, so the SQL EF generates is tested too.
public class ServiceRequestFilterTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly AppDbContext _db;

    public ServiceRequestFilterTests()
    {
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();

        _db.ServiceRequests.AddRange(
            Request("1", "Roads - Pothole Repair", "PANORAMA HILLS", "Open", "OS - Mobility", new DateTime(2026, 9, 1, 8, 0, 0)),
            Request("2", "Roads - Pothole Repair", "BOWNESS", "Closed", "OS - Mobility", new DateTime(2026, 9, 15)),
            Request("3", "WRS - Cart Management", "PANORAMA HILLS", "Closed", "OS - Waste and Recycling Services", new DateTime(2026, 9, 30, 23, 0, 0)));
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public void Apply_WithNoFilters_ReturnsEverything()
    {
        Assert.Equal(["1", "2", "3"], Ids(new ServiceRequestFilter()));
    }

    [Fact]
    public void Apply_CombinesFilters()
    {
        var filter = new ServiceRequestFilter { Community = "PANORAMA HILLS", Status = "Closed" };

        Assert.Equal(["3"], Ids(filter));
    }

    [Fact]
    public void Apply_MatchesCommunityAndStatus_IgnoringCase()
    {
        var filter = new ServiceRequestFilter { Community = "Panorama Hills", Status = "open" };

        Assert.Equal(["1"], Ids(filter));
    }

    [Fact]
    public void Apply_MatchesPartOfServiceType_IgnoringCase()
    {
        var filter = new ServiceRequestFilter { ServiceType = "pothole" };

        Assert.Equal(["1", "2"], Ids(filter));
    }

    [Fact]
    public void Apply_FiltersByDepartment()
    {
        var filter = new ServiceRequestFilter { Department = "OS - Waste and Recycling Services" };

        Assert.Equal(["3"], Ids(filter));
    }

    [Fact]
    public void Apply_DateRange_IncludesTheWholeToDay()
    {
        // Request 3 is at 11 p.m. on Sept 30, so a To date of Sept 30 must still include it.
        var filter = new ServiceRequestFilter { From = new DateTime(2026, 9, 15), To = new DateTime(2026, 9, 30) };

        Assert.Equal(["2", "3"], Ids(filter));
    }

    private List<string> Ids(ServiceRequestFilter filter) =>
        filter.Apply(_db.ServiceRequests).Select(r => r.ServiceRequestId).OrderBy(id => id).ToList();

    private static ServiceRequest Request(
        string id, string serviceName, string community, string status, string department, DateTime requested) =>
        new()
        {
            ServiceRequestId = id,
            ServiceName = serviceName,
            CommunityName = community,
            Status = status,
            AgencyResponsible = department,
            RequestedDate = requested
        };
}
