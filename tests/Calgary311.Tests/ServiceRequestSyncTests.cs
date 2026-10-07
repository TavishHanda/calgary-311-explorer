using System.Globalization;
using System.Net;
using System.Text;
using Calgary311.Web;
using Calgary311.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Calgary311.Tests;

// Names the xUnit collection for tests that run a sync. Tests in one collection run one at a time.
public static class SyncTests
{
    public const string Collection = "Sync";
}

// Runs the real sync against a fake API and an in-memory SQLite database.
[Collection(SyncTests.Collection)]
public class ServiceRequestSyncTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly FakeApi _api = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task SyncAsync_PagesThroughResults_AndAddsEveryRow()
    {
        // Page size is 2, so three rows arrive as one full page and one short page.
        _api.Pages.Add(Page(Row("A", "2026-10-01", "Open"), Row("B", "2026-10-01", "Open")));
        _api.Pages.Add(Page(Row("C", "2026-10-02", "Open")));

        var result = await CreateSync().SyncAsync();

        Assert.Equal(new SyncResult(Added: 3, Updated: 0), result);
        Assert.Equal(3, await _database.Db.ServiceRequests.CountAsync());
        Assert.Equal(2, _api.RequestedUrls.Count);
        Assert.Contains("requested_date", _api.RequestedUrls[0]);
    }

    [Fact]
    public async Task SyncAsync_UpdatesExistingRows_InsteadOfDuplicating()
    {
        _api.Pages.Add(Page(Row("A", "2026-10-01", "Open")));
        await CreateSync().SyncAsync();

        _api.Pages.Clear();
        _api.RequestedUrls.Clear();
        _api.Pages.Add(Page(Row("A", "2026-10-01", "Closed", updated: "2026-10-03", closed: "2026-10-03")));
        var result = await CreateSync().SyncAsync();

        Assert.Equal(new SyncResult(Added: 0, Updated: 1), result);
        var stored = Assert.Single(await _database.Db.ServiceRequests.ToListAsync());
        Assert.Equal("Closed", stored.Status);
        Assert.Equal(2, stored.DaysToClose);

        // The second sync only asks for rows updated since the newest one it already has.
        Assert.Contains("updated_date >= '2026-10-01T00:00:00'", _api.RequestedUrls[0]);
    }

    [Fact]
    public async Task SyncAsync_AfterAnInterruptedSync_PicksUpWhereItStopped()
    {
        // Rows come oldest update first. The first page saves, then the API fails on the second.
        _api.Pages.Add(Page(Row("A", "2026-09-01", "Open"), Row("B", "2026-09-02", "Open")));
        _api.Pages.Add(null);
        await Assert.ThrowsAsync<HttpRequestException>(() => CreateSync().SyncAsync());

        _api.Pages.Clear();
        _api.RequestedUrls.Clear();
        _api.Pages.Add(Page(Row("B", "2026-09-02", "Open"), Row("C", "2026-09-05", "Open")));
        var result = await CreateSync().SyncAsync();

        // Everything not yet saved was updated on or after the newest saved row (B), so nothing is skipped.
        Assert.Contains("updated_date >= '2026-09-02T00:00:00'", _api.RequestedUrls[0]);
        Assert.Contains("$order=updated_date,service_request_id", _api.RequestedUrls[0]);
        Assert.Equal(new SyncResult(Added: 1, Updated: 0), result);
        Assert.Equal(3, await _database.Db.ServiceRequests.CountAsync());
    }

    [Fact]
    public async Task SyncAsync_FormatsDatesTheSame_WhateverTheMachinesRegion()
    {
        _api.Pages.Add(Page(Row("A", "2026-10-01", "Open")));
        await CreateSync().SyncAsync();
        _api.RequestedUrls.Clear();

        var original = CultureInfo.CurrentCulture;
        try
        {
            // Finnish writes times as 00.00.00, and the City's API wouldn't understand that.
            CultureInfo.CurrentCulture = new CultureInfo("fi-FI");
            await CreateSync().SyncAsync();
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }

        Assert.Contains("updated_date >= '2026-10-01T00:00:00'", _api.RequestedUrls[0]);
    }

    // Builds one API row as JSON, with dates in the API's format (2026-10-01T00:00:00.000).
    private static string Row(string id, string requested, string status, string? updated = null, string? closed = null)
    {
        var closedField = closed is null ? "" : $", \"closed_date\": \"{closed}T00:00:00.000\"";
        return $"{{\"service_request_id\": \"{id}\", \"requested_date\": \"{requested}T00:00:00.000\", "
            + $"\"updated_date\": \"{updated ?? requested}T00:00:00.000\", \"status_description\": \"{status}\"{closedField}}}";
    }

    private static string Page(params string[] rows) => $"[{string.Join(",", rows)}]";

    private ServiceRequestSync CreateSync()
    {
        var http = new HttpClient(_api) { BaseAddress = new Uri("https://example.test/resource/") };
        var options = Options.Create(new OpenCalgaryOptions { PageSize = 2 });
        return new ServiceRequestSync(http, _database.Db, options, NullLogger<ServiceRequestSync>.Instance);
    }

    // Stands in for the City's API: returns the queued pages in order, then an empty page.
    // A null page makes that request fail, as if the API were down.
    private class FakeApi : HttpMessageHandler
    {
        public List<string?> Pages { get; } = [];
        public List<string> RequestedUrls { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var index = RequestedUrls.Count;
            RequestedUrls.Add(Uri.UnescapeDataString(request.RequestUri!.AbsoluteUri));
            var json = index < Pages.Count ? Pages[index] : "[]";
            if (json is null)
            {
                throw new HttpRequestException("The API is down");
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
