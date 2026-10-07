using System.Globalization;
using System.Net.Http.Json;
using Calgary311.Web.Data;
using Calgary311.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Calgary311.Web.Services;

/// <summary>How many requests a sync added and how many existing ones changed.</summary>
public record SyncResult(int Added, int Updated);

/// <summary>
/// Loads 311 requests from the Open Calgary API into the database. The first sync loads the last
/// InitialSyncDays of requests; later syncs only fetch requests updated since the newest one stored.
/// </summary>
public class ServiceRequestSync(
    HttpClient http,
    AppDbContext db,
    IOptions<OpenCalgaryOptions> options,
    ILogger<ServiceRequestSync> logger)
{
    // Shared by every instance (static), so only one sync runs at a time even if the button is clicked twice.
    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    private readonly OpenCalgaryOptions _options = options.Value;

    /// <summary>
    /// Runs a sync, or returns null straight away if another sync is already running.
    /// </summary>
    public async Task<SyncResult?> SyncAsync(CancellationToken cancellationToken = default)
    {
        if (!await SyncLock.WaitAsync(0, cancellationToken))
        {
            return null;
        }

        try
        {
            return await RunSyncAsync(cancellationToken);
        }
        finally
        {
            SyncLock.Release();
        }
    }

    private async Task<SyncResult> RunSyncAsync(CancellationToken cancellationToken)
    {
        var where = await BuildWhereClauseAsync(cancellationToken);
        logger.LogInformation("Starting sync with filter {Where}", where);

        int added = 0, updated = 0, offset = 0;
        while (true)
        {
            var page = await FetchPageAsync(where, offset, cancellationToken);
            var (pageAdded, pageUpdated) = await SavePageAsync(page, cancellationToken);
            added += pageAdded;
            updated += pageUpdated;
            logger.LogInformation("Synced {Count} rows at offset {Offset}", page.Count, offset);

            // A short page means there's nothing after it.
            if (page.Count < _options.PageSize)
            {
                break;
            }
            offset += _options.PageSize;
        }

        logger.LogInformation("Sync finished: {Added} added, {Updated} updated", added, updated);
        return new SyncResult(added, updated);
    }

    private async Task<string> BuildWhereClauseAsync(CancellationToken cancellationToken)
    {
        var lastUpdate = await db.ServiceRequests.MaxAsync(r => r.UpdatedDate, cancellationToken);
        if (lastUpdate is DateTime since)
        {
            // >= rather than > because the API's dates have no time part, so later changes on the same day share it.
            return $"updated_date >= '{FormatDate(since)}'";
        }

        var start = DateTime.Today.AddDays(-_options.InitialSyncDays);
        return $"requested_date >= '{FormatDate(start)}'";
    }

    private async Task<List<SocrataServiceRequest>> FetchPageAsync(
        string where, int offset, CancellationToken cancellationToken)
    {
        // Oldest update first, so if a sync stops partway, everything saved is older than everything not yet
        // saved. The next sync asks for "updated since the newest stored", so it picks up where this one
        // stopped instead of leaving a gap. The unique ID breaks ties so $offset never skips or repeats rows.
        var url = $"{_options.DatasetId}.json"
            + $"?$where={Uri.EscapeDataString(where)}"
            + $"&$order={Uri.EscapeDataString("updated_date,service_request_id")}"
            + $"&$limit={_options.PageSize}"
            + $"&$offset={offset}";

        return await http.GetFromJsonAsync<List<SocrataServiceRequest>>(url, cancellationToken) ?? [];
    }

    private async Task<(int Added, int Updated)> SavePageAsync(
        List<SocrataServiceRequest> page, CancellationToken cancellationToken)
    {
        var incoming = page
            .Select(ServiceRequestMapper.ToModel)
            .OfType<ServiceRequest>()
            .DistinctBy(r => r.ServiceRequestId)
            .ToList();

        // One query loads every request on this page that's already stored.
        var ids = incoming.Select(r => r.ServiceRequestId).ToList();
        var existing = await db.ServiceRequests
            .Where(r => ids.Contains(r.ServiceRequestId))
            .ToDictionaryAsync(r => r.ServiceRequestId, cancellationToken);

        var added = 0;
        foreach (var request in incoming)
        {
            if (existing.TryGetValue(request.ServiceRequestId, out var stored))
            {
                ServiceRequestMapper.CopyTo(request, stored);
            }
            else
            {
                db.ServiceRequests.Add(request);
                added++;
            }
        }

        // EF only marks a row Modified if a value actually changed, so this counts real updates.
        var updated = db.ChangeTracker.Entries<ServiceRequest>().Count(e => e.State == EntityState.Modified);

        await db.SaveChangesAsync(cancellationToken);

        // Stop tracking this page so memory stays flat across 100,000+ rows.
        db.ChangeTracker.Clear();

        return (added, updated);
    }

    // InvariantCulture so the API always gets 2026-10-01T00:00:00, whatever the machine's region settings.
    private static string FormatDate(DateTime date) => date.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
}
