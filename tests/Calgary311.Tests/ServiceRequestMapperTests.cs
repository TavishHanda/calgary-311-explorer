using Calgary311.Web.Models;
using Calgary311.Web.Services;

namespace Calgary311.Tests;

public class ServiceRequestMapperTests
{
    [Fact]
    public void ToModel_CopiesFields_AndParsesCoordinates()
    {
        var row = new SocrataServiceRequest
        {
            ServiceRequestId = "26-00745785",
            RequestedDate = new DateTime(2026, 10, 1),
            Status = "Open",
            ServiceName = "Roads - Pothole Repair",
            CommunityName = "PANORAMA HILLS",
            Longitude = "-113.958374560746",
            Latitude = "51.156126735611394"
        };

        var request = ServiceRequestMapper.ToModel(row);

        Assert.NotNull(request);
        Assert.Equal("26-00745785", request.ServiceRequestId);
        Assert.Equal(new DateTime(2026, 10, 1), request.RequestedDate);
        Assert.Equal("Open", request.Status);
        Assert.Equal("PANORAMA HILLS", request.CommunityName);
        Assert.Equal(-113.958374560746, request.Longitude);
        Assert.Equal(51.156126735611394, request.Latitude);
        Assert.Null(request.ClosedDate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a number")]
    public void ToModel_LeavesCoordinateNull_WhenMissingOrInvalid(string? longitude)
    {
        var row = new SocrataServiceRequest
        {
            ServiceRequestId = "1",
            RequestedDate = new DateTime(2026, 10, 1),
            Longitude = longitude
        };

        Assert.Null(ServiceRequestMapper.ToModel(row)!.Longitude);
    }

    [Fact]
    public void ToModel_ReturnsNull_WhenIdIsMissing()
    {
        var row = new SocrataServiceRequest { RequestedDate = new DateTime(2026, 10, 1) };

        Assert.Null(ServiceRequestMapper.ToModel(row));
    }

    [Fact]
    public void ToModel_ReturnsNull_WhenRequestedDateIsMissing()
    {
        var row = new SocrataServiceRequest { ServiceRequestId = "1" };

        Assert.Null(ServiceRequestMapper.ToModel(row));
    }

    [Fact]
    public void CopyTo_UpdatesFields_ButKeepsDatabaseKey()
    {
        var stored = new ServiceRequest { Id = 42, ServiceRequestId = "1", Status = "Open" };
        var fresh = new ServiceRequest
        {
            ServiceRequestId = "1",
            Status = "Closed",
            ClosedDate = new DateTime(2026, 10, 3)
        };

        ServiceRequestMapper.CopyTo(fresh, stored);

        Assert.Equal(42, stored.Id);
        Assert.Equal("Closed", stored.Status);
        Assert.Equal(new DateTime(2026, 10, 3), stored.ClosedDate);
    }
}
