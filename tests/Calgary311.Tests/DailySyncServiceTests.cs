using Calgary311.Web.Services;

namespace Calgary311.Tests;

public class DailySyncServiceTests
{
    private static readonly TimeSpan ThreeAm = new(3, 0, 0);

    [Fact]
    public void TimeUntilNextRun_IsLaterToday_WhenTheTimeHasNotPassed()
    {
        var now = new DateTime(2026, 10, 6, 1, 30, 0);

        Assert.Equal(TimeSpan.FromMinutes(90), DailySyncService.TimeUntilNextRun(now, ThreeAm));
    }

    [Fact]
    public void TimeUntilNextRun_IsTomorrow_WhenTheTimeHasPassed()
    {
        var now = new DateTime(2026, 10, 6, 14, 0, 0);

        Assert.Equal(TimeSpan.FromHours(13), DailySyncService.TimeUntilNextRun(now, ThreeAm));
    }

    [Fact]
    public void TimeUntilNextRun_IsAFullDay_AtExactlyTheSyncTime()
    {
        // Just ran at 3:00, so the next run is tomorrow, not immediately again.
        var now = new DateTime(2026, 10, 6, 3, 0, 0);

        Assert.Equal(TimeSpan.FromDays(1), DailySyncService.TimeUntilNextRun(now, ThreeAm));
    }
}
