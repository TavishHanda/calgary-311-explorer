using Calgary311.Web.Data;
using Calgary311.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Calgary311.Web.Pages.Requests;

/// <summary>Everything known about one request, found by the City's ID, e.g. /Requests/26-00745785.</summary>
public class DetailsModel(AppDbContext db) : PageModel
{
    public ServiceRequest ServiceRequest { get; private set; } = null!;

    /// <summary>Days since the request was made, for requests that are still open.</summary>
    public int? DaysOpen { get; private set; }

    // The id comes from the URL because of the "{id}" route in Details.cshtml.
    public async Task<IActionResult> OnGetAsync(string id)
    {
        var request = await db.ServiceRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ServiceRequestId == id);

        if (request is null)
        {
            return NotFound();
        }

        ServiceRequest = request;
        if (request.ClosedDate is null)
        {
            DaysOpen = (DateTime.Today - request.RequestedDate.Date).Days;
        }

        return Page();
    }
}
