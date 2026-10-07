using Calgary311.Web;
using Calgary311.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Calgary311.Tests;

// Shares a collection with ServiceRequestSyncTests so they don't run at the same time: only one sync can run
// at once (a static lock), so a sync test running in parallel would make this one's sync skip.
[Collection(SyncTests.Collection)]
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

    [Fact]
    public async Task ATimedOutSync_IsLogged_AndTheServiceKeepsRunning()
    {
        // An HttpClient timeout surfaces as TaskCanceledException, an OperationCanceledException.
        // It must not escape the service, because a background service that crashes stops the whole app.
        using var database = new TestDatabase();
        var api = new TimingOutApi();
        var options = Options.Create(new OpenCalgaryOptions());

        var services = new ServiceCollection();
        services.AddScoped(_ => new ServiceRequestSync(
            new HttpClient(api) { BaseAddress = new Uri("https://example.test/resource/") },
            database.Db,
            options,
            NullLogger<ServiceRequestSync>.Instance));
        await using var provider = services.BuildServiceProvider();

        using var service = new DailySyncService(
            provider.GetRequiredService<IServiceScopeFactory>(), options, NullLogger<DailySyncService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await api.Called.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await Task.Delay(200); // let the failure reach the catch block

        // Still running: waiting for tomorrow's sync rather than faulted.
        Assert.False(service.ExecuteTask!.IsCompleted);

        await service.StopAsync(CancellationToken.None);
    }

    // Fails every request the way an HttpClient timeout does.
    private class TimingOutApi : HttpMessageHandler
    {
        public TaskCompletionSource Called { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Called.TrySetResult();
            throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout");
        }
    }
}
