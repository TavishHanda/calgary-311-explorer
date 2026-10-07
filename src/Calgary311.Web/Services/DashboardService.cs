using Calgary311.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Calgary311.Web.Services;

/// <summary>Loads the rows matching a filter and calculates the dashboard from them.</summary>
public class DashboardService(AppDbContext db)
{
    public async Task<DashboardStats> GetStatsAsync(ServiceRequestFilter filter, CancellationToken cancellationToken = default)
    {
        // Select only the five columns the dashboard needs, so loading ~130,000 rows stays quick.
        var rows = await filter.Apply(db.ServiceRequests.AsNoTracking())
            .Select(r => new DashboardRow(r.ServiceName, r.Status, r.AgencyResponsible, r.RequestedDate, r.ClosedDate))
            .ToListAsync(cancellationToken);

        return DashboardCalculator.Calculate(rows, filter.From);
    }
}
