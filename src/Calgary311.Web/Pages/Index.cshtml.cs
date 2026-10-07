using Calgary311.Web.Data;
using Calgary311.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Calgary311.Web.Pages;

public class IndexModel(AppDbContext db, ILogger<IndexModel> logger) : PageModel
{
    public int TotalRequests { get; private set; }
    public DateTime? NewestRequest { get; private set; }
    public string? SetupMessage { get; private set; }

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
            // Most likely the database hasn't been created yet.
            logger.LogWarning(ex, "Could not read the database");
            SetupMessage = "Couldn't read the database. Create it with: dotnet ef migrations add InitialCreate, then dotnet ef database update (run both in src/Calgary311.Web).";
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
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Sync failed");
            SyncMessage = "Sync failed: couldn't get data from the City's API. Check the app's log for details.";
        }

        return RedirectToPage();
    }
}
