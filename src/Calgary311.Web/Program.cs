using Calgary311.Web;
using Calgary311.Web.Api;
using Calgary311.Web.Data;
using Calgary311.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.Configure<OpenCalgaryOptions>(
    builder.Configuration.GetSection(OpenCalgaryOptions.SectionName));

builder.Services.AddScoped<DashboardService>();
builder.Services.AddHostedService<DailySyncService>();

// A typed HttpClient: ASP.NET creates the HttpClient, points it at the API and injects it into ServiceRequestSync.
builder.Services.AddHttpClient<ServiceRequestSync>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<OpenCalgaryOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    if (!string.IsNullOrEmpty(options.AppToken))
    {
        client.DefaultRequestHeaders.Add("X-App-Token", options.AppToken);
    }
});

var app = builder.Build();

// Before app.Run() starts the background sync, so the tables exist by the time it needs them.
app.MigrateDatabase();

if (!app.Environment.IsDevelopment())
{
    // Outside development, an unhandled error shows the friendly Error page instead of a blank 500.
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();

app.MapRequestsApi();

app.Run();
