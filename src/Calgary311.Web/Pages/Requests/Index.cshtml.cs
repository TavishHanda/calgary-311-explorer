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
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

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
            .OrderByDescending(r => r.RequestedDate)
            .ThenByDescending(r => r.ServiceRequestId)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        Communities = await DistinctValuesAsync(r => r.CommunityName);
        Statuses = await DistinctValuesAsync(r => r.Status);
        Departments = await DistinctValuesAsync(r => r.AgencyResponsible);
        ServiceTypes = await DistinctValuesAsync(r => r.ServiceName);
    }

    /// <summary>
    /// The current filters plus a page number, for building Previous/Next links that keep the filters.
    /// </summary>
    public Dictionary<string, string> RouteValuesFor(int page)
    {
        var values = new Dictionary<string, string>
        {
            ["Community"] = Filter.Community ?? "",
            ["ServiceType"] = Filter.ServiceType ?? "",
            ["Status"] = Filter.Status ?? "",
            ["Department"] = Filter.Department ?? "",
            ["From"] = Filter.From?.ToString("yyyy-MM-dd") ?? "",
            ["To"] = Filter.To?.ToString("yyyy-MM-dd") ?? "",
            ["pageNumber"] = page.ToString()
        };

        // Leave out empty filters so the URL stays short.
        return values.Where(v => v.Value != "").ToDictionary();
    }

    private Task<List<string>> DistinctValuesAsync(System.Linq.Expressions.Expression<Func<ServiceRequest, string?>> column) =>
        db.ServiceRequests
            .Select(column)
            .Where(v => v != null && v != "")
            .Distinct()
            .OrderBy(v => v)
            .Select(v => v!)
            .ToListAsync();
}
