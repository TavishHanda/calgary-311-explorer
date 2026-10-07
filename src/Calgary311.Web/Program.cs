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

if (!app.Environment.IsDevelopment())
{
    // TODO (optional): add an Error page and call app.UseExceptionHandler("/Error") here.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();

app.MapRequestsApi();

app.Run();
