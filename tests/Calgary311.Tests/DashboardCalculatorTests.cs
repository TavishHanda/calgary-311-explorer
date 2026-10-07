using Calgary311.Web.Services;

namespace Calgary311.Tests;

public class DashboardCalculatorTests
{
    private static DashboardRow Row(
        string service = "Roads - Pothole Repair",
        string status = "Open",
        string? department = "OS - Mobility",
        DateTime? requested = null,
        DateTime? closed = null) =>
        new(service, status, department, requested ?? new DateTime(2026, 9, 1), closed);

    [Fact]
    public void Calculate_CountsOpenAndClosed_ByStatus()
    {
        var rows = new[]
        {
            Row(status: "Open"),
            Row(status: "Duplicate (Open)"),
            Row(status: "Closed", closed: new DateTime(2026, 9, 3)),
            // Reopened: has a closed date but its status is Open, so it counts as open.
            Row(status: "Open", closed: new DateTime(2026, 9, 2))
        };

        var stats = DashboardCalculator.Calculate(rows);

        Assert.Equal(4, stats.Total);
        Assert.Equal(3, stats.Open);
        Assert.Equal(1, stats.Closed);
    }

    [Fact]
    public void Calculate_AveragesAndMedians_DaysToClose_ForClosedRequestsOnly()
    {
        var rows = new[]
        {
            Row(status: "Closed", requested: new DateTime(2026, 9, 1), closed: new DateTime(2026, 9, 2)),  // 1 day
            Row(status: "Closed", requested: new DateTime(2026, 9, 1), closed: new DateTime(2026, 9, 3)),  // 2 days
            Row(status: "Closed", requested: new DateTime(2026, 9, 1), closed: new DateTime(2026, 9, 30)), // 29 days
            Row(status: "Open", requested: new DateTime(2026, 9, 1), closed: new DateTime(2026, 9, 21))    // ignored
        };

        var stats = DashboardCalculator.Calculate(rows);

        Assert.Equal(32 / 3.0, stats.AverageDaysToClose!.Value, precision: 6);
        Assert.Equal(2, stats.MedianDaysToClose);
    }

    [Fact]
    public void Calculate_LeavesDaysToCloseEmpty_WhenNothingIsClosed()
    {
        var stats = DashboardCalculator.Calculate([Row(status: "Open")]);

        Assert.Null(stats.AverageDaysToClose);
        Assert.Null(stats.MedianDaysToClose);
    }

    [Fact]
    public void Calculate_ListsTopServiceTypes_MostCommonFirst_UpToTen()
    {
        var rows = new List<DashboardRow>();
        for (var i = 1; i <= 12; i++)
        {
            // Service type 12 appears 12 times, 11 appears 11 times, and so on.
            rows.AddRange(Enumerable.Repeat(Row(service: $"Type {i}"), i));
        }

        var top = DashboardCalculator.Calculate(rows).TopServiceTypes;

        Assert.Equal(DashboardCalculator.TopServiceTypeCount, top.Count);
        Assert.Equal(new CountItem("Type 12", 12), top[0]);
        Assert.Equal(new CountItem("Type 3", 3), top[^1]);
    }

    [Fact]
    public void Calculate_BreaksDownByDepartment()
    {
        var rows = new[]
        {
            Row(department: "Water", status: "Closed", requested: new DateTime(2026, 9, 1), closed: new DateTime(2026, 9, 5)),
            Row(department: "Water", status: "Closed", requested: new DateTime(2026, 9, 1), closed: new DateTime(2026, 9, 3)),
            Row(department: "Water", status: "Open"),
            Row(department: "Parks", status: "Open"),
            Row(department: null)
        };

        var departments = DashboardCalculator.Calculate(rows).Departments;

        Assert.Equal(2, departments.Count);
        Assert.Equal(new DepartmentStats("Water", Requests: 3, Closed: 2, AverageDaysToClose: 3, MedianDaysToClose: 3), departments[0]);
        Assert.Equal(new DepartmentStats("Parks", Requests: 1, Closed: 0, AverageDaysToClose: null, MedianDaysToClose: null), departments[1]);
    }

    [Theory]
    [InlineData("2026-09-28", "2026-09-28")] // Monday
    [InlineData("2026-10-01", "2026-09-28")] // Wednesday
    [InlineData("2026-10-04", "2026-09-28")] // Sunday belongs to the week before
    public void WeekStart_ReturnsTheMonday(string date, string expected)
    {
        Assert.Equal(DateTime.Parse(expected), DashboardCalculator.WeekStart(DateTime.Parse(date)));
    }

    [Fact]
    public void Calculate_CountsRequestsPerWeek_IncludingEmptyAndPartialWeeks()
    {
        var rows = new[]
        {
            Row(requested: new DateTime(2026, 7, 15)),  // Wednesday, first day of data: partial week
            Row(requested: new DateTime(2026, 9, 21)),  // Monday
            Row(requested: new DateTime(2026, 9, 23)),
            Row(requested: new DateTime(2026, 10, 1))   // Wednesday, last day of data: partial week
        };

        var weeks = DashboardCalculator.Calculate(rows).RequestsPerWeek;

        Assert.Equal(DashboardCalculator.WeeksShown, weeks.Count);
        Assert.Equal(new WeekCount(new DateTime(2026, 9, 28), 1, IsPartial: true), weeks[^1]);
        Assert.Equal(new WeekCount(new DateTime(2026, 9, 21), 2, IsPartial: false), weeks[^2]);
        Assert.Equal(new WeekCount(new DateTime(2026, 9, 14), 0, IsPartial: false), weeks[^3]);
        // 12 weeks back from the week of Sept 28 starts on Monday, July 13.
        Assert.Equal(new WeekCount(new DateTime(2026, 7, 13), 1, IsPartial: true), weeks[0]);
    }

    [Fact]
    public void Calculate_UsesDataStart_ToSpotAPartialFirstWeek()
    {
        // An old request (like the ones later syncs pick up) shouldn't hide that the window starts mid-week.
        var rows = new[]
        {
            Row(requested: new DateTime(2024, 1, 1)),
            Row(requested: new DateTime(2026, 7, 15)),
            Row(requested: new DateTime(2026, 10, 1))
        };

        var withoutStart = DashboardCalculator.Calculate(rows).RequestsPerWeek;
        var withStart = DashboardCalculator.Calculate(rows, dataStart: new DateTime(2026, 7, 15)).RequestsPerWeek;

        Assert.False(withoutStart[0].IsPartial);
        Assert.True(withStart[0].IsPartial);
    }

    [Theory]
    [InlineData(new[] { 5 }, 5.0)]
    [InlineData(new[] { 9, 1, 3 }, 3.0)]
    [InlineData(new[] { 4, 1, 3, 2 }, 2.5)]
    public void Median_ReturnsTheMiddleValue(int[] values, double expected)
    {
        Assert.Equal(expected, DashboardCalculator.Median(values.ToList()));
    }
}
