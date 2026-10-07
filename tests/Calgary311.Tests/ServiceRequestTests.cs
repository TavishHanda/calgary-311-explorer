using Calgary311.Web.Models;

namespace Calgary311.Tests;

// An example to copy from. Add your own test classes for the sync mapping,
// filters and dashboard calculations as you build them.
public class ServiceRequestTests
{
    [Fact]
    public void DaysToClose_IsNull_WhenRequestIsStillOpen()
    {
        var request = new ServiceRequest { RequestedDate = new DateTime(2026, 9, 1) };

        Assert.Null(request.DaysToClose);
    }

    [Fact]
    public void DaysToClose_CountsWholeDays_BetweenRequestAndClose()
    {
        var request = new ServiceRequest
        {
            RequestedDate = new DateTime(2026, 9, 1, 14, 30, 0),
            ClosedDate = new DateTime(2026, 9, 4, 9, 0, 0)
        };

        Assert.Equal(3, request.DaysToClose);
    }
}
