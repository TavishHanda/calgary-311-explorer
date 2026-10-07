namespace Calgary311.Web.Models;

/// <summary>
/// One 311 service request from the City of Calgary's open data portal
/// (dataset iahh-g8bj). Property comments give the source field name.
/// </summary>
public class ServiceRequest
{
    /// <summary>Database key.</summary>
    public int Id { get; set; }

    /// <summary>service_request_id: the City's unique ID for the request.</summary>
    public string ServiceRequestId { get; set; } = string.Empty;

    /// <summary>requested_date: when the request was submitted.</summary>
    public DateTime RequestedDate { get; set; }

    /// <summary>updated_date: when the request was last updated.</summary>
    public DateTime? UpdatedDate { get; set; }

    /// <summary>closed_date: when the request was closed, if it has been.</summary>
    public DateTime? ClosedDate { get; set; }

    /// <summary>status_description: for example "Open" or "Closed".</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>source: how the request was submitted (phone, app, web, other).</summary>
    public string? Source { get; set; }

    /// <summary>service_name: the type of request, e.g. "Roads - Pothole Repair".</summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>agency_responsible: the City department handling it.</summary>
    public string? AgencyResponsible { get; set; }

    /// <summary>address: location of the request, when one was given.</summary>
    public string? Address { get; set; }

    /// <summary>comm_code: community code.</summary>
    public string? CommunityCode { get; set; }

    /// <summary>comm_name: community name, e.g. "Panorama Hills".</summary>
    public string? CommunityName { get; set; }

    /// <summary>longitude</summary>
    public double? Longitude { get; set; }

    /// <summary>latitude</summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Whole days between the request and its closure, or null while it's still open.
    /// Not stored in the database.
    /// </summary>
    public int? DaysToClose =>
        ClosedDate.HasValue ? (ClosedDate.Value.Date - RequestedDate.Date).Days : null;
}
