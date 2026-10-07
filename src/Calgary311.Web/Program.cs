using Calgary311.Web;
using Calgary311.Web.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.Configure<OpenCalgaryOptions>(
    builder.Configuration.GetSection(OpenCalgaryOptions.SectionName));

// TODO (roadmap step 3): register the sync service here, e.g.
// builder.Services.AddHttpClient<ServiceRequestSync>();

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

// TODO (roadmap step 6): map the API endpoint here, e.g.
// app.MapGet("/api/requests", ...);

app.Run();
