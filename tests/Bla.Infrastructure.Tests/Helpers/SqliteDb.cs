using Bla.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Bla.Infrastructure.Tests.Helpers;

// SQLite in-memory database for repository tests: exercises the real EF Core
// mappings without requiring Docker/SQL Server. The database lives as long
// as the connection stays open.
public sealed class SqliteDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public BlaDbContext Context { get; }

    public SqliteDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Context = CreateContext();
        Context.Database.EnsureCreated();
    }

    // A fresh context over the same database: lets tests read back with a
    // clean change tracker, proving data was actually persisted.
    public BlaDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BlaDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new BlaDbContext(options);
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
