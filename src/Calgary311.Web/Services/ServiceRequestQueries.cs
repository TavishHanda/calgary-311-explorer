using System.Linq.Expressions;
using Calgary311.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Calgary311.Web.Services;

/// <summary>
/// Query pieces shared by the Browse page, the dashboard and the JSON API.
/// They are extension methods: the "this" on the first parameter lets them be called like LINQ's own
/// methods, e.g. query.NewestFirst(). They only add to the query; EF still sends one SQL statement.
/// </summary>
public static class ServiceRequestQueries
{
    /// <summary>Newest request first. The unique ID breaks ties, so paging gives a stable order.</summary>
    public static IQueryable<ServiceRequest> NewestFirst(this IQueryable<ServiceRequest> requests) =>
        requests
            .OrderByDescending(r => r.RequestedDate)
            .ThenByDescending(r => r.ServiceRequestId);

    /// <summary>One page of results. Page numbers start at 1.</summary>
    public static IQueryable<ServiceRequest> GetPage(this IQueryable<ServiceRequest> requests, int page, int pageSize) =>
        requests
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

    /// <summary>How many pages it takes to show totalCount items. Zero when there are no items.</summary>
    public static int PageCount(int totalCount, int pageSize) =>
        (int)Math.Ceiling(totalCount / (double)pageSize);

    /// <summary>Every distinct, non-empty value of one column, sorted, for a filter dropdown.</summary>
    /// <param name="column">
    /// An Expression rather than a plain Func, so EF can read which column it is and put it in the SQL.
    /// </param>
    public static Task<List<string>> DistinctValuesAsync(
        this IQueryable<ServiceRequest> requests,
        Expression<Func<ServiceRequest, string?>> column,
        CancellationToken cancellationToken = default) =>
        requests
            .Select(column)
            .Where(v => v != null && v != "")
            .Distinct()
            .OrderBy(v => v)
            .Select(v => v!)
            .ToListAsync(cancellationToken);
}
