using Calgary311.Web.Models;

namespace Calgary311.Web.Services;

/// <summary>The few columns the dashboard needs from each request.</summary>
public record DashboardRow(string ServiceName, string Status, string? Department, DateTime RequestedDate, DateTime? ClosedDate);

public record CountItem(string Label, int Count);

public record DepartmentStats(
    string Department, int Requests, int Closed, double? AverageDaysToClose, double? MedianDaysToClose);

/// <summary>Requests made in the week starting WeekStart (a Monday). Partial if the data doesn't cover the whole week.</summary>
public record WeekCount(DateTime WeekStart, int Count, bool IsPartial);

public record DashboardStats(
    int Total,
    int Open,
    int Closed,
    double? AverageDaysToClose,
    double? MedianDaysToClose,
    List<CountItem> Statuses,
    List<CountItem> TopServiceTypes,
    List<DepartmentStats> Departments,
    List<WeekCount> RequestsPerWeek);

/// <summary>
/// Turns rows into dashboard numbers. Pure calculation with no database, so it's simple to unit test.
/// </summary>
public static class DashboardCalculator
{
    public const int TopServiceTypeCount = 10;
    // 12 rather than 13: the first sync loads 90 days, which only covers 12 whole weeks.
    public const int WeeksShown = 12;

    /// <param name="dataStart">
    /// Where the data starts, if known (e.g. the filter's From date), for spotting a partial first week.
    /// Defaults to the oldest request.
    /// </param>
    public static DashboardStats Calculate(IReadOnlyCollection<DashboardRow> rows, DateTime? dataStart = null)
    {
        // Go by status, not ClosedDate: some reopened requests say "Open" but still have a closed date.
        var closedRows = rows.Where(IsClosed).ToList();
        var daysToClose = DaysToClose(closedRows);

        return new DashboardStats(
            Total: rows.Count,
            Open: rows.Count(IsOpen),
            Closed: closedRows.Count,
            AverageDaysToClose: AverageOrNull(daysToClose),
            MedianDaysToClose: Median(daysToClose),
            Statuses: CountBy(rows, r => r.Status).ToList(),
            TopServiceTypes: CountBy(rows, r => r.ServiceName).Take(TopServiceTypeCount).ToList(),
            Departments: DepartmentBreakdown(rows),
            RequestsPerWeek: RequestsPerWeek(rows, dataStart));
    }

    public static bool IsOpen(DashboardRow row) => row.Status.Contains("Open", StringComparison.OrdinalIgnoreCase);

    public static bool IsClosed(DashboardRow row) => ServiceRequest.IsClosedStatus(row.Status);

    /// <summary>Monday of the week containing the date.</summary>
    public static DateTime WeekStart(DateTime date)
    {
        // DayOfWeek has Sunday = 0, so shift it to make Monday = 0.
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-daysSinceMonday);
    }

    /// <summary>The middle value (the mean of the two middle values for an even count), or null for an empty list.</summary>
    public static double? Median(List<int> values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        var sorted = values.Order().ToList();
        var middle = sorted.Count / 2;
        return sorted.Count % 2 == 1
            ? sorted[middle]
            : (sorted[middle - 1] + sorted[middle]) / 2.0;
    }

    // LINQ's Average() throws on an empty list, so return null instead, like Median does.
    private static double? AverageOrNull(List<int> values) => values.Count > 0 ? values.Average() : null;

    // Whole days, matching ServiceRequest.DaysToClose. Closed requests without a closed date are skipped.
    private static List<int> DaysToClose(IEnumerable<DashboardRow> closedRows) =>
        closedRows
            .Where(r => r.ClosedDate.HasValue)
            .Select(r => (r.ClosedDate!.Value.Date - r.RequestedDate.Date).Days)
            .ToList();

    // Most common first; ties broken alphabetically so the order is stable.
    private static IEnumerable<CountItem> CountBy(IEnumerable<DashboardRow> rows, Func<DashboardRow, string> key) =>
        rows.GroupBy(key)
            .Select(g => new CountItem(g.Key, g.Count()))
            .OrderByDescending(c => c.Count)
            .ThenBy(c => c.Label);

    private static List<DepartmentStats> DepartmentBreakdown(IEnumerable<DashboardRow> rows) =>
        rows.Where(r => !string.IsNullOrEmpty(r.Department))
            .GroupBy(r => r.Department!)
            .Select(g =>
            {
                var closed = g.Where(IsClosed).ToList();
                var days = DaysToClose(closed);
                return new DepartmentStats(
                    g.Key,
                    Requests: g.Count(),
                    Closed: closed.Count,
                    AverageDaysToClose: AverageOrNull(days),
                    MedianDaysToClose: Median(days));
            })
            .OrderByDescending(d => d.Requests)
            .ThenBy(d => d.Department)
            .ToList();

    // The last WeeksShown weeks up to the newest request, including weeks with no requests.
    private static List<WeekCount> RequestsPerWeek(IReadOnlyCollection<DashboardRow> rows, DateTime? dataStart)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var newest = rows.Max(r => r.RequestedDate).Date;
        var lastWeek = WeekStart(newest);
        var firstWeek = lastWeek.AddDays(-7 * (WeeksShown - 1));
        var oldest = dataStart?.Date ?? rows.Min(r => r.RequestedDate).Date;

        var counts = rows
            .Where(r => r.RequestedDate >= firstWeek)
            .GroupBy(r => WeekStart(r.RequestedDate))
            .ToDictionary(g => g.Key, g => g.Count());

        return Enumerable.Range(0, WeeksShown)
            .Select(i => firstWeek.AddDays(7 * i))
            .Select(week => new WeekCount(
                week,
                counts.GetValueOrDefault(week),
                // Partial if the data starts after the week begins or ends before it finishes.
                IsPartial: week < oldest || week.AddDays(6) > newest))
            .ToList();
    }
}
