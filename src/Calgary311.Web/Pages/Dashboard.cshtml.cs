using System.Globalization;
using Calgary311.Web.Data;
using Calgary311.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Calgary311.Web.Pages;

public class DashboardModel(DashboardService dashboard, AppDbContext db) : PageModel
{
    // Reuses the Browse filters. The form only offers community and dates, but any filter in the URL
    // (e.g. ?Status=Open) also applies; the page lists those under the heading and keeps them.
    [BindProperty(SupportsGet = true)]
    public ServiceRequestFilter Filter { get; set; } = new();

    public DashboardStats Stats { get; private set; } = null!;
    public List<string> Communities { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Stats = await dashboard.GetStatsAsync(Filter, cancellationToken);
        Communities = await db.ServiceRequests.DistinctValuesAsync(r => r.CommunityName, cancellationToken);
    }

    /// <summary>
    /// Route values for a Browse link that keeps all of the dashboard's filters and adds (or replaces) one,
    /// so clicking a bar shows exactly the requests behind it.
    /// </summary>
    public Dictionary<string, string> BrowseLink(string key, string value)
    {
        var values = Filter.ToRouteValues();
        values[key] = value;
        return values;
    }

    /// <summary>Filters from the URL that the form doesn't show, e.g. "Status: Open", for the heading.</summary>
    public List<string> ExtraFilters()
    {
        var extras = new List<string>();
        if (!string.IsNullOrWhiteSpace(Filter.Status)) extras.Add($"Status: {Filter.Status}");
        if (!string.IsNullOrWhiteSpace(Filter.Department)) extras.Add($"Department: {Filter.Department}");
        if (!string.IsNullOrWhiteSpace(Filter.ServiceName)) extras.Add($"Service type: {Filter.ServiceName}");
        if (!string.IsNullOrWhiteSpace(Filter.ServiceType)) extras.Add($"Service type contains: {Filter.ServiceType}");
        return extras;
    }

    /// <summary>
    /// A bar's length as a CSS percentage of the largest value in its chart. InvariantCulture keeps the
    /// decimal point a "." as CSS expects, even on machines set to a culture that uses a comma.
    /// </summary>
    public static string Percent(double value, double max) =>
        max <= 0 ? "0%" : (value / max * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%";
}
