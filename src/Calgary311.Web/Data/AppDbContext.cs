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

        // Indexes for the filters on the Browse page and the dashboard.
        request.HasIndex(r => r.RequestedDate);
        request.HasIndex(r => r.CommunityName);
        request.HasIndex(r => r.ServiceName);
        request.HasIndex(r => r.Status);

        request.Property(r => r.ServiceRequestId).HasMaxLength(50);
        request.Property(r => r.Status).HasMaxLength(50);
        request.Property(r => r.ServiceName).HasMaxLength(200);

        // Calculated in C#, not stored.
        request.Ignore(r => r.DaysToClose);
    }
}
