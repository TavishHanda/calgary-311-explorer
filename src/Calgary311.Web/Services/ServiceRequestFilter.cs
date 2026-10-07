using System.Globalization;
using Calgary311.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Calgary311.Web.Services;

/// <summary>
/// Search filters for service requests. Every filter is optional; empty ones are ignored.
/// Bound from the query string, e.g. ?Community=Panorama Hills&amp;Status=Open.
/// Shared by the Browse page, the dashboard and the /api/requests endpoint.
/// </summary>
public class ServiceRequestFilter
{
    /// <summary>Community name, e.g. "Panorama Hills". Not case-sensitive.</summary>
    public string? Community { get; set; }

    /// <summary>Part of the service name, e.g. "pothole". Not case-sensitive.</summary>
    public string? ServiceType { get; set; }

    /// <summary>
    /// The exact service name, e.g. "WATS - Fire Hydrant". Not case-sensitive. The dashboard's links use this,
    /// because a partial match (ServiceType) would also pull in "WATS - Fire Hydrant Flow Test".
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>Status, e.g. "Open". Not case-sensitive.</summary>
    public string? Status { get; set; }

    /// <summary>Department (agency_responsible). Not case-sensitive.</summary>
    public string? Department { get; set; }

    /// <summary>Earliest requested date, inclusive.</summary>
    public DateTime? From { get; set; }

    /// <summary>Latest requested date, inclusive (the whole day counts).</summary>
    public DateTime? To { get; set; }

    /// <summary>
    /// Adds a Where clause for each filter that has a value. Because it works on IQueryable,
    /// EF turns the whole thing into one SQL query; nothing is filtered in memory.
    /// </summary>
    public IQueryable<ServiceRequest> Apply(IQueryable<ServiceRequest> requests)
    {
        if (!string.IsNullOrWhiteSpace(Community))
        {
            // The City stores names in capitals ("PANORAMA HILLS"); NOCASE lets "Panorama Hills" match too.
            requests = requests.Where(r => EF.Functions.Collate(r.CommunityName, "NOCASE") == Community.Trim());
        }

        if (!string.IsNullOrWhiteSpace(ServiceType))
        {
            // SQLite's LIKE ignores case, so "pothole" matches "Roads - Pothole Repair".
            // % and _ are LIKE wildcards, so they're escaped with \ to be matched as ordinary characters.
            var pattern = $"%{EscapeLike(ServiceType.Trim())}%";
            requests = requests.Where(r => EF.Functions.Like(r.ServiceName, pattern, @"\"));
        }

        if (!string.IsNullOrWhiteSpace(ServiceName))
        {
            requests = requests.Where(r => EF.Functions.Collate(r.ServiceName, "NOCASE") == ServiceName.Trim());
        }

        if (!string.IsNullOrWhiteSpace(Status))
        {
            requests = requests.Where(r => EF.Functions.Collate(r.Status, "NOCASE") == Status.Trim());
        }

        if (!string.IsNullOrWhiteSpace(Department))
        {
            requests = requests.Where(r => EF.Functions.Collate(r.AgencyResponsible, "NOCASE") == Department.Trim());
        }

        if (From is DateTime from)
        {
            requests = requests.Where(r => r.RequestedDate >= from.Date);
        }

        if (To is DateTime to)
        {
            // Before the start of the next day, so requests at any time on the To date are included.
            var dayAfter = to.Date.AddDays(1);
            requests = requests.Where(r => r.RequestedDate < dayAfter);
        }

        return requests;
    }

    /// <summary>
    /// The filters that have a value, as query-string values, for links that keep the current filters
    /// (Browse paging, the dashboard's click-throughs).
    /// </summary>
    public Dictionary<string, string> ToRouteValues()
    {
        var values = new Dictionary<string, string?>
        {
            [nameof(Community)] = Community,
            [nameof(ServiceType)] = ServiceType,
            [nameof(ServiceName)] = ServiceName,
            [nameof(Status)] = Status,
            [nameof(Department)] = Department,
            [nameof(From)] = IsoDate(From),
            [nameof(To)] = IsoDate(To)
        };

        // Leave out empty filters so URLs stay short.
        return values
            .Where(v => !string.IsNullOrWhiteSpace(v.Value))
            .ToDictionary(v => v.Key, v => v.Value!);
    }

    /// <summary>
    /// A date as yyyy-MM-dd, the format URLs and date inputs expect. InvariantCulture keeps it the same
    /// on every machine; otherwise a computer set to, say, Thai would write the year as 2569.
    /// </summary>
    public static string? IsoDate(DateTime? date) =>
        date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string EscapeLike(string text) =>
        text.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
}
