using System.Text.Json.Serialization;

namespace Calgary311.Web.Services;

/// <summary>
/// One row exactly as the Socrata API returns it. Any field can be missing, so everything is nullable.
/// Longitude and latitude arrive as strings and are parsed in <see cref="ServiceRequestMapper"/>.
/// </summary>
public class SocrataServiceRequest
{
    [JsonPropertyName("service_request_id")]
    public string? ServiceRequestId { get; set; }

    [JsonPropertyName("requested_date")]
    public DateTime? RequestedDate { get; set; }

    [JsonPropertyName("updated_date")]
    public DateTime? UpdatedDate { get; set; }

    [JsonPropertyName("closed_date")]
    public DateTime? ClosedDate { get; set; }

    [JsonPropertyName("status_description")]
    public string? Status { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("service_name")]
    public string? ServiceName { get; set; }

    [JsonPropertyName("agency_responsible")]
    public string? AgencyResponsible { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("comm_code")]
    public string? CommunityCode { get; set; }

    [JsonPropertyName("comm_name")]
    public string? CommunityName { get; set; }

    [JsonPropertyName("longitude")]
    public string? Longitude { get; set; }

    [JsonPropertyName("latitude")]
    public string? Latitude { get; set; }
}
