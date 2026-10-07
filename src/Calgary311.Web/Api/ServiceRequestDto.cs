using Calgary311.Web.Models;

namespace Calgary311.Web.Api;

/// <summary>
/// A request as the API returns it. A separate type from the entity, so the API's JSON doesn't change
/// by accident when the database model does, and the internal database key (Id) isn't exposed.
/// </summary>
public record ServiceRequestDto(
    string ServiceRequestId,
    DateTime RequestedDate,
    DateTime? UpdatedDate,
    DateTime? ClosedDate,
    int? DaysToClose,
    string Status,
    string? Source,
    string ServiceName,
    string? AgencyResponsible,
    string? Address,
    string? CommunityCode,
    string? CommunityName,
    double? Longitude,
    double? Latitude)
{
    public static ServiceRequestDto From(ServiceRequest r) => new(
        r.ServiceRequestId,
        r.RequestedDate,
        r.UpdatedDate,
        r.ClosedDate,
        r.DaysToClose,
        r.Status,
        r.Source,
        r.ServiceName,
        r.AgencyResponsible,
        r.Address,
        r.CommunityCode,
        r.CommunityName,
        r.Longitude,
        r.Latitude);
}

/// <summary>One page of results plus the numbers a client needs to fetch the rest.</summary>
public record PagedResponse<T>(int Page, int PageSize, int TotalCount, int TotalPages, List<T> Items);
