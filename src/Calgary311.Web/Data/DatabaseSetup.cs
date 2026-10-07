using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Calgary311.Web.Data;

public static class DatabaseSetup
{
    /// <summary>
    /// Creates the database if it doesn't exist and applies any migrations it doesn't have yet.
    /// Runs at startup, so a fresh server (where you can't run `dotnet ef database update`) sets itself up.
    /// The same thing the command does, and nothing happens if the database is already up to date.
    /// </summary>
    public static void MigrateDatabase(this WebApplication app)
    {
        // Startup code runs outside any web request, so it makes its own scope to get the scoped DbContext.
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        CreateFolderFor(db.Database.GetConnectionString());
        db.Database.Migrate();
    }

    // SQLite creates the database file but not the folder it goes in (e.g. /home/data on Azure).
    private static void CreateFolderFor(string? connectionString)
    {
        var settings = new SqliteConnectionStringBuilder(connectionString);
        if (settings.Mode == SqliteOpenMode.Memory || settings.DataSource is "" or ":memory:")
        {
            return;
        }

        var folder = Path.GetDirectoryName(Path.GetFullPath(settings.DataSource));
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }
    }
}
