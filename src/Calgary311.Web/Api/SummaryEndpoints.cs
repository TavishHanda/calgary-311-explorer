using Calgary311.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Calgary311.Web.Api;

/// <summary>A few headline numbers, small enough for another site (like the portfolio) to show.</summary>
/// <param name="Day">The most recent full day in the data, or null if there's no data yet.</param>
public record SummaryResponse(DateTime? Day, int RequestsThatDay, string? TopServiceThatDay, int OpenRequests);

public static class SummaryEndpoints
{
    /// <summary>The CORS policy that lets the sites listed in Cors:AllowedOrigins call this endpoint.</summary>
    public const string CorsPolicy = "PublicSummary";

    public static void MapSummaryApi(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/summary", GetSummaryAsync).RequireCors(CorsPolicy);
    }

    // GET /api/summary
    private static async Task<SummaryResponse> GetSummaryAsync(AppDbContext db)
    {
        var requests = db.ServiceRequests.AsNoTracking();

        // The cast to DateTime? makes MaxAsync return null on an empty table instead of throwing.
        var newest = await requests.MaxAsync(r => (DateTime?)r.RequestedDate);
        if (newest is null)
        {
            return new SummaryResponse(null, 0, null, 0);
        }

        // The newest day is usually still filling up, so report the day before it, which is complete.
        var day = newest.Value.Date.AddDays(-1);
        var thatDay = requests.Where(r => r.RequestedDate >= day && r.RequestedDate < day.AddDays(1));

        var count = await thatDay.CountAsync();
        var topService = await thatDay
            .GroupBy(r => r.ServiceName)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => g.Key)
            .FirstOrDefaultAsync();

        // LIKE is case-insensitive in SQLite, matching how the dashboard counts open requests.
        var open = await requests.CountAsync(r => EF.Functions.Like(r.Status, "%open%"));

        return new SummaryResponse(day, count, topService, open);
    }
}
