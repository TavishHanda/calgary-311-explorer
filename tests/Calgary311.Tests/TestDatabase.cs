using Calgary311.Web.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Calgary311.Tests;

/// <summary>
/// A real SQLite database held in memory, with the app's migrations applied, for tests that need a database.
/// An in-memory SQLite database lives only as long as its connection stays open, so this keeps the
/// connection open until Dispose.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    /// <param name="connectionString">
    /// The default is a private database for this object only. Pass a named shared-cache connection
    /// string when other code (like the web app in RequestsApiTests) needs to open the same database.
    /// </param>
    public TestDatabase(string connectionString = "DataSource=:memory:")
    {
        _connection = new SqliteConnection(connectionString);
        _connection.Open();

        Db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        // Migrate (not EnsureCreated) builds the tables from the real migrations, the same way the app does
        // at startup. That tests the migrations too, and lets the app's own startup Migrate() see an
        // up-to-date database in the API tests instead of tables it doesn't know the history of.
        Db.Database.Migrate();
    }

    public AppDbContext Db { get; }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
