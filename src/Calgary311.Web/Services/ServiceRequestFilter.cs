using Calgary311.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Calgary311.Web.Services;

/// <summary>
/// Search filters for service requests. Every filter is optional; empty ones are ignored.
/// Bound from the query string, e.g. ?Community=Panorama Hills&amp;Status=Open.
/// Shared by the Browse page and the /api/requests endpoint.
/// </summary>
public class ServiceRequestFilter
{
    /// <summary>Community name, e.g. "Panorama Hills". Not case-sensitive.</summary>
    public string? Community { get; set; }

    /// <summary>Part of the service name, e.g. "pothole". Not case-sensitive.</summary>
    public string? ServiceType { get; set; }

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
            requests = requests.Where(r => EF.Functions.Like(r.ServiceName, $"%{ServiceType.Trim()}%"));
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
}
