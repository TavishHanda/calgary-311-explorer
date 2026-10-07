using Calgary311.Web.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Calgary311.Tests;

/// <summary>
/// A real SQLite database held in memory, with the app's tables created, for tests that need a database.
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
        Db.Database.EnsureCreated();
    }

    public AppDbContext Db { get; }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
