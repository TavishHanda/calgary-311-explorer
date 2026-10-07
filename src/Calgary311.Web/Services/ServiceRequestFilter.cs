using Calgary311.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Calgary311.Web.Services;

/// <summary>
/// Search filters for service requests. Every filter is optional; empty ones are ignored.
/// Bound from the query string, e.g. ?Community=PANORAMA HILLS&amp;Status=Open.
/// Shared by the Browse page and (later) the API endpoint.
/// </summary>
public class ServiceRequestFilter
{
    /// <summary>Exact community name.</summary>
    public string? Community { get; set; }

    /// <summary>Part of the service name, e.g. "pothole". Not case-sensitive.</summary>
    public string? ServiceType { get; set; }

    /// <summary>Exact status, e.g. "Open".</summary>
    public string? Status { get; set; }

    /// <summary>Exact department (agency_responsible).</summary>
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
            requests = requests.Where(r => r.CommunityName == Community);
        }

        if (!string.IsNullOrWhiteSpace(ServiceType))
        {
            // SQLite's LIKE ignores case, so "pothole" matches "Roads - Pothole Repair".
            requests = requests.Where(r => EF.Functions.Like(r.ServiceName, $"%{ServiceType.Trim()}%"));
        }

        if (!string.IsNullOrWhiteSpace(Status))
        {
            requests = requests.Where(r => r.Status == Status);
        }

        if (!string.IsNullOrWhiteSpace(Department))
        {
            requests = requests.Where(r => r.AgencyResponsible == Department);
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
