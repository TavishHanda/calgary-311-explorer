using Calgary311.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Calgary311.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var request = modelBuilder.Entity<ServiceRequest>();

        // The City's ID is unique, which lets the sync update existing rows instead of duplicating them.
        request.HasIndex(r => r.ServiceRequestId).IsUnique();

        // RequestedDate is the index that does the most work: Browse and the API sort by it, and the
        // From/To filters use it. The text filters compare with NOCASE or LIKE '%...%', which SQLite can't
        // answer from these ordinary (case-sensitive) indexes, so those scan the table instead. At about
        // 130,000 rows that's still quick.
        request.HasIndex(r => r.RequestedDate);
        request.HasIndex(r => r.CommunityName);
        request.HasIndex(r => r.ServiceName);
        request.HasIndex(r => r.Status);

        request.Property(r => r.ServiceRequestId).HasMaxLength(50);
        request.Property(r => r.Status).HasMaxLength(50);
        request.Property(r => r.ServiceName).HasMaxLength(200);

        // Calculated in C#, not stored.
        request.Ignore(r => r.IsClosed);
        request.Ignore(r => r.DaysToClose);
    }
}
