using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Calgary311.Tests;

// What a fresh server sees: no database file and no folder for it. Starting the app should create both.
public class DatabaseSetupTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"calgary311-tests-{Guid.NewGuid()}");

    public void Dispose()
    {
        // SQLite keeps pooled connections open, which would lock the file on Windows.
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public async Task Startup_CreatesTheDatabaseAndItsFolder()
    {
        var databasePath = Path.Combine(_folder, "data", "calgary311.db");

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder
                .UseSetting("ConnectionStrings:Default", $"Data Source={databasePath}")
                .UseSetting("OpenCalgary:AutoSync", "false"));
        using var client = factory.CreateClient();

        var home = await client.GetStringAsync("/");

        Assert.True(File.Exists(databasePath));
        Assert.Contains("Requests stored", home);
    }
}
