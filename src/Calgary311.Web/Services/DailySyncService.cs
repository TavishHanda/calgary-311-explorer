using Microsoft.Extensions.Options;

namespace Calgary311.Web.Services;

/// <summary>
/// Runs the sync in the background: once when the app starts (to catch up), then every day at
/// OpenCalgary:DailySyncTime. Turn it off with OpenCalgary:AutoSync = false.
/// </summary>
public class DailySyncService(
    IServiceScopeFactory scopeFactory,
    IOptions<OpenCalgaryOptions> options,
    ILogger<DailySyncService> logger) : BackgroundService
{
    private readonly OpenCalgaryOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.AutoSync)
        {
            logger.LogInformation("Automatic sync is off");
            return;
        }

        await RunSyncAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNextRun(DateTime.Now, _options.DailySyncTime);
            logger.LogInformation("Next automatic sync in {Delay:hh\\:mm}", delay);

            // stoppingToken cancels the wait when the app shuts down.
            await Task.Delay(delay, stoppingToken);
            await RunSyncAsync(stoppingToken);
        }
    }

    /// <summary>Time from now until the next timeOfDay: later today, or tomorrow if it has passed.</summary>
    public static TimeSpan TimeUntilNextRun(DateTime now, TimeSpan timeOfDay)
    {
        var next = now.Date + timeOfDay;
        if (next <= now)
        {
            next = next.AddDays(1);
        }
        return next - now;
    }

    private async Task RunSyncAsync(CancellationToken stoppingToken)
    {
        try
        {
            // This service lives for the whole app (a singleton), but ServiceRequestSync uses the
            // DbContext, which is scoped. So each run gets its own scope, like one web request does.
            using var scope = scopeFactory.CreateScope();
            var sync = scope.ServiceProvider.GetRequiredService<ServiceRequestSync>();

            var result = await sync.SyncAsync(stoppingToken);
            if (result is null)
            {
                logger.LogInformation("Skipped automatic sync: a sync is already running");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Log and carry on, so one failed run (e.g. the City's API is down) doesn't stop tomorrow's.
            logger.LogError(ex, "Automatic sync failed");
        }
    }
}
