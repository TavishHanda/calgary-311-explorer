using System.Globalization;
using Calgary311.Web.Models;
using Calgary311.Web.Services;

namespace Calgary311.Tests;

// Runs the filters against a real (in-memory) SQLite database, so the SQL EF generates is tested too.
public class ServiceRequestFilterTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public ServiceRequestFilterTests()
    {
        _database.Db.ServiceRequests.AddRange(
            Request("1", "Roads - Pothole Repair", "PANORAMA HILLS", "Open", "OS - Mobility", new DateTime(2026, 9, 1, 8, 0, 0)),
            Request("2", "Roads - Pothole Repair", "BOWNESS", "Closed", "OS - Mobility", new DateTime(2026, 9, 15)),
            Request("3", "WRS - Cart Management", "PANORAMA HILLS", "Closed", "OS - Waste and Recycling Services", new DateTime(2026, 9, 30, 23, 0, 0)));
        _database.Db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public void Apply_WithNoFilters_ReturnsEverything()
    {
        Assert.Equal(["1", "2", "3"], Ids(new ServiceRequestFilter()));
    }

    [Fact]
    public void Apply_ServiceName_MatchesOnlyTheExactName()
    {
        AddRequests(
            Request("10", "WATS - Fire Hydrant", "BOWNESS", "Open", "OS - Water Services", new DateTime(2026, 9, 2)),
            Request("11", "WATS - Fire Hydrant Flow Test", "BOWNESS", "Open", "OS - Water Services", new DateTime(2026, 9, 3)));

        // A partial match would also return 11, so the dashboard's links use the exact name.
        Assert.Equal(["10"], Ids(new ServiceRequestFilter { ServiceName = "wats - fire hydrant" }));
        Assert.Equal(["10", "11"], Ids(new ServiceRequestFilter { ServiceType = "Fire Hydrant" }));
    }

    [Fact]
    public void Apply_ServiceType_TreatsWildcardsAsOrdinaryCharacters()
    {
        AddRequests(Request("10", "Parks - 100% Tree Survey", "BOWNESS", "Open", "OS - Parks", new DateTime(2026, 9, 2)));

        // Unescaped, "%" and "_" are LIKE wildcards and would match every request.
        Assert.Equal(["10"], Ids(new ServiceRequestFilter { ServiceType = "100%" }));
        Assert.Empty(Ids(new ServiceRequestFilter { ServiceType = "_" }));
    }

    [Fact]
    public void ToRouteValues_KeepsOnlyFiltersWithValues_WithIsoDates()
    {
        var filter = new ServiceRequestFilter { Community = "BOWNESS", Status = " ", From = new DateTime(2026, 9, 15) };

        Assert.Equal(
            new Dictionary<string, string> { ["Community"] = "BOWNESS", ["From"] = "2026-09-15" },
            filter.ToRouteValues());
    }

    [Fact]
    public void IsoDate_IsTheSame_WhateverTheMachinesRegion()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            // Thai uses the Buddhist calendar, so a culture-dependent format would give 2569.
            CultureInfo.CurrentCulture = new CultureInfo("th-TH");
            Assert.Equal("2026-09-15", ServiceRequestFilter.IsoDate(new DateTime(2026, 9, 15)));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
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

    private void AddRequests(params ServiceRequest[] requests)
    {
        _database.Db.ServiceRequests.AddRange(requests);
        _database.Db.SaveChanges();
    }

    private List<string> Ids(ServiceRequestFilter filter) =>
        filter.Apply(_database.Db.ServiceRequests).Select(r => r.ServiceRequestId).OrderBy(id => id).ToList();

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
