using Calgary311.Web.Models;

namespace Calgary311.Tests;

// Tests for the calculated properties on the entity: IsClosed and DaysToClose.
public class ServiceRequestTests
{
    [Fact]
    public void DaysToClose_IsNull_WhenRequestIsStillOpen()
    {
        var request = new ServiceRequest { Status = "Open", RequestedDate = new DateTime(2026, 9, 1) };

        Assert.Null(request.DaysToClose);
    }

    [Fact]
    public void DaysToClose_CountsWholeDays_BetweenRequestAndClose()
    {
        var request = new ServiceRequest
        {
            Status = "Closed",
            RequestedDate = new DateTime(2026, 9, 1, 14, 30, 0),
            ClosedDate = new DateTime(2026, 9, 4, 9, 0, 0)
        };

        Assert.Equal(3, request.DaysToClose);
    }

    [Fact]
    public void DaysToClose_IsNull_ForAReopenedRequest()
    {
        // Reopened: the status says Open, but the old closed date is still there.
        var request = new ServiceRequest
        {
            Status = "Open",
            RequestedDate = new DateTime(2026, 9, 1),
            ClosedDate = new DateTime(2026, 9, 4)
        };

        Assert.False(request.IsClosed);
        Assert.Null(request.DaysToClose);
    }

    [Theory]
    [InlineData("Closed", true)]
    [InlineData("Duplicate (Closed)", true)]
    [InlineData("Open", false)]
    [InlineData("Duplicate (Open)", false)]
    public void IsClosedStatus_GoesByTheStatusText(string status, bool expected)
    {
        Assert.Equal(expected, ServiceRequest.IsClosedStatus(status));
    }
}
