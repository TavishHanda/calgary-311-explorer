using System.Globalization;
using Calgary311.Web.Models;

namespace Calgary311.Web.Services;

/// <summary>
/// Turns API rows into <see cref="ServiceRequest"/> entities.
/// </summary>
public static class ServiceRequestMapper
{
    /// <summary>
    /// Maps one API row, or returns null if it has no ID or requested date (it can't be stored without them).
    /// </summary>
    public static ServiceRequest? ToModel(SocrataServiceRequest row)
    {
        if (string.IsNullOrWhiteSpace(row.ServiceRequestId) || row.RequestedDate is null)
        {
            return null;
        }

        return new ServiceRequest
        {
            ServiceRequestId = row.ServiceRequestId,
            RequestedDate = row.RequestedDate.Value,
            UpdatedDate = row.UpdatedDate,
            ClosedDate = row.ClosedDate,
            Status = row.Status ?? string.Empty,
            Source = row.Source,
            ServiceName = row.ServiceName ?? string.Empty,
            AgencyResponsible = row.AgencyResponsible,
            Address = row.Address,
            CommunityCode = row.CommunityCode,
            CommunityName = row.CommunityName,
            Longitude = ParseCoordinate(row.Longitude),
            Latitude = ParseCoordinate(row.Latitude)
        };
    }

    /// <summary>
    /// Copies every field except the database key, so a request that was synced before is updated in place.
    /// </summary>
    public static void CopyTo(ServiceRequest source, ServiceRequest target)
    {
        target.RequestedDate = source.RequestedDate;
        target.UpdatedDate = source.UpdatedDate;
        target.ClosedDate = source.ClosedDate;
        target.Status = source.Status;
        target.Source = source.Source;
        target.ServiceName = source.ServiceName;
        target.AgencyResponsible = source.AgencyResponsible;
        target.Address = source.Address;
        target.CommunityCode = source.CommunityCode;
        target.CommunityName = source.CommunityName;
        target.Longitude = source.Longitude;
        target.Latitude = source.Latitude;
    }

    // InvariantCulture so "51.15" parses the same on machines that use a comma as the decimal separator.
    private static double? ParseCoordinate(string? value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : null;
}
