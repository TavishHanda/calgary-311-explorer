using Calgary311.Web.Data;
using Calgary311.Web.Models;
using Calgary311.Web.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Calgary311.Web.Api;

/// <summary>
/// The JSON API. These are minimal API endpoints: plain methods mapped to a URL, without a controller class.
/// </summary>
public static class RequestsEndpoints
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 500;

    /// <summary>Called from Program.cs to register the endpoints.</summary>
    public static void MapRequestsApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/requests");
        group.MapGet("/", ListRequestsAsync);
        group.MapGet("/{id}", GetRequestAsync);
    }

    // GET /api/requests?community=Panorama Hills&status=Open&page=2&pageSize=100
    // [AsParameters] fills the filter's properties from the query string; db is injected from DI.
    private static async Task<Ok<PagedResponse<ServiceRequestDto>>> ListRequestsAsync(
        [AsParameters] ServiceRequestFilter filter,
        AppDbContext db,
        int page = 1,
        int pageSize = DefaultPageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = filter.Apply(db.ServiceRequests.AsNoTracking());
        var totalCount = await query.CountAsync();

        // Past the last page there's nothing to fetch. Checking first (in long arithmetic) also stops a huge
        // page number from overflowing int in GetPage and wrapping round to a negative offset, which SQLite
        // treats as 0, so ?page=100000000 would have returned page 1.
        var offset = (long)(page - 1) * pageSize;
        List<ServiceRequest> requests = offset < totalCount
            ? await query.NewestFirst().GetPage(page, pageSize).ToListAsync()
            : [];

        var totalPages = ServiceRequestQueries.PageCount(totalCount, pageSize);
        var items = requests.Select(ServiceRequestDto.From).ToList();
        return TypedResults.Ok(new PagedResponse<ServiceRequestDto>(page, pageSize, totalCount, totalPages, items));
    }

    // GET /api/requests/26-00697212
    // Results<Ok<...>, NotFound> declares both possible responses in the method's type.
    private static async Task<Results<Ok<ServiceRequestDto>, NotFound>> GetRequestAsync(string id, AppDbContext db)
    {
        var request = await db.ServiceRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ServiceRequestId == id);

        return request is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ServiceRequestDto.From(request));
    }
}
