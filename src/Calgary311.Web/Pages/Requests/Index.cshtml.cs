using System.Globalization;
using Calgary311.Web.Data;
using Calgary311.Web.Models;
using Calgary311.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Calgary311.Web.Pages.Requests;

/// <summary>The Browse page: a filtered, paged table of requests, newest first.</summary>
public class IndexModel(AppDbContext db) : PageModel
{
    public const int PageSize = 50;

    // SupportsGet = true binds these from the query string on a GET; by default Razor Pages only binds on POST.
    [BindProperty(SupportsGet = true)]
    public ServiceRequestFilter Filter { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public List<ServiceRequest> Requests { get; private set; } = [];
    public int TotalCount { get; private set; }

    // At least 1, so the Math.Clamp in OnGetAsync still has a valid range when nothing matches.
    public int TotalPages => Math.Max(1, ServiceRequestQueries.PageCount(TotalCount, PageSize));

    // Options for the dropdowns.
    public List<string> Communities { get; private set; } = [];
    public List<string> Statuses { get; private set; } = [];
    public List<string> Departments { get; private set; } = [];
    public List<string> ServiceTypes { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var query = Filter.Apply(db.ServiceRequests.AsNoTracking());

        TotalCount = await query.CountAsync();
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);

        Requests = await query
            .NewestFirst()
            .GetPage(PageNumber, PageSize)
            .ToListAsync();

        Communities = await db.ServiceRequests.DistinctValuesAsync(r => r.CommunityName);
        Statuses = await db.ServiceRequests.DistinctValuesAsync(r => r.Status);
        Departments = await db.ServiceRequests.DistinctValuesAsync(r => r.AgencyResponsible);
        ServiceTypes = await db.ServiceRequests.DistinctValuesAsync(r => r.ServiceName);
    }

    /// <summary>
    /// The current filters plus a page number, for building Previous/Next links that keep the filters.
    /// </summary>
    public Dictionary<string, string> RouteValuesFor(int page)
    {
        var values = Filter.ToRouteValues();
        values["pageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        return values;
    }

    /// <summary>The current filters without the exact service name, for the "show all types" link.</summary>
    public Dictionary<string, string> RouteValuesWithoutServiceName()
    {
        var values = Filter.ToRouteValues();
        values.Remove(nameof(ServiceRequestFilter.ServiceName));
        return values;
    }
}
