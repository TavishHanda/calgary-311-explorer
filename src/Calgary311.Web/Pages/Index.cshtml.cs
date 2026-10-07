using Calgary311.Web.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Calgary311.Web.Pages;

public class IndexModel(AppDbContext db, ILogger<IndexModel> logger) : PageModel
{
    public int TotalRequests { get; private set; }
    public DateTime? NewestRequest { get; private set; }
    public string? SetupMessage { get; private set; }

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
}
