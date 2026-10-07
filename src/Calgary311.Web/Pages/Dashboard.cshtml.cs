using Calgary311.Web.Data;
using Calgary311.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Calgary311.Web.Pages;

public class DashboardModel(DashboardService dashboard, AppDbContext db) : PageModel
{
    // Reuses the Browse filters; the form only offers community and dates, but any filter in the URL works.
    [BindProperty(SupportsGet = true)]
    public ServiceRequestFilter Filter { get; set; } = new();

    public DashboardStats Stats { get; private set; } = null!;
    public List<string> Communities { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Stats = await dashboard.GetStatsAsync(Filter, cancellationToken);

        Communities = await db.ServiceRequests
            .Where(r => r.CommunityName != null && r.CommunityName != "")
            .Select(r => r.CommunityName!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Route values for a Browse link that keeps the dashboard's filters and adds one more,
    /// so clicking a bar shows the requests behind it.
    /// </summary>
    public Dictionary<string, string> BrowseLink(string key, string value)
    {
        var values = new Dictionary<string, string> { [key] = value };
        if (!string.IsNullOrWhiteSpace(Filter.Community)) values["Community"] = Filter.Community;
        if (Filter.From is DateTime from) values["From"] = from.ToString("yyyy-MM-dd");
        if (Filter.To is DateTime to) values["To"] = to.ToString("yyyy-MM-dd");
        return values;
    }

    /// <summary>A bar's length as a CSS percentage of the largest value in its chart.</summary>
    public static string Percent(double value, double max) =>
        max <= 0 ? "0%" : (value / max * 100).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "%";
}
