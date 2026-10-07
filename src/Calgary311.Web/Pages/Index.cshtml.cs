using Calgary311.Web.Data;
using Calgary311.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Calgary311.Web.Pages;

public class IndexModel(AppDbContext db, IOptions<OpenCalgaryOptions> options, ILogger<IndexModel> logger) : PageModel
{
    public int TotalRequests { get; private set; }
    public DateTime? NewestRequest { get; private set; }
    public string? SetupMessage { get; private set; }

    // Settings for the empty-database message.
    public int InitialSyncDays => options.Value.InitialSyncDays;
    public bool AutoSync => options.Value.AutoSync;

    /// <summary>Result of the last sync, kept across the redirect back to this page.</summary>
    [TempData]
    public string? SyncMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            TotalRequests = await db.ServiceRequests.CountAsync();
            NewestRequest = await db.ServiceRequests
                .MaxAsync(r => (DateTime?)r.RequestedDate);
        }
        catch (Exception ex)
        {
            // The app creates the database at startup, so this means something else went wrong (e.g. file permissions).
            logger.LogWarning(ex, "Could not read the database");
            SetupMessage = "Couldn't read the database. Check the app's log for details.";
        }
    }

    // Handles the Sync button (a POST to ?handler=Sync), then redirects so refreshing doesn't sync again.
    public async Task<IActionResult> OnPostSyncAsync([FromServices] ServiceRequestSync sync, CancellationToken cancellationToken)
    {
        try
        {
            var result = await sync.SyncAsync(cancellationToken);
            SyncMessage = result is null
                ? "A sync is already running. Refresh in a minute to see the new data."
                : $"Sync finished: {result.Added:N0} new requests, {result.Updated:N0} updated.";
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Any failure (the API timing out or changing its format, a database error) becomes a message on
            // the page rather than an error page. The filter skips the case where the browser itself gave up.
            logger.LogError(ex, "Sync failed");
            SyncMessage = "Sync failed. The City's API may be slow or unavailable; check the app's log for details.";
        }

        return RedirectToPage();
    }
}
