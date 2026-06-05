using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PlexNotifierr.Core.Models;

namespace PlexNotifierr.TestSupport;

/// <summary>
/// A real (relational) SQLite database backed by an in-memory connection, shared across every
/// <see cref="PlexNotifierrDbContext"/> handed out by this instance.
///
/// We use SQLite in-memory rather than the EF Core InMemory provider because the latter does not
/// honour relational query/constraint semantics, which this codebase relies on (composite keys,
/// includes, foreign keys). The single connection is kept open for the lifetime of the instance:
/// closing it would drop the schema and all data.
///
/// It implements <see cref="IDbContextFactory{TContext}"/> so it can be injected directly into
/// services that take a factory (e.g. <c>SubscriptionService</c>), and also exposes
/// <see cref="CreateContext"/> for jobs that take a <see cref="PlexNotifierrDbContext"/> directly.
/// </summary>
public sealed class SqliteInMemoryDatabase : IDbContextFactory<PlexNotifierrDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<PlexNotifierrDbContext> _options;

    public SqliteInMemoryDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<PlexNotifierrDbContext>()
            .UseSqlite(_connection)
            .Options;

        // The DbContext constructor calls Database.EnsureCreated(); creating one context here
        // materialises the schema on the shared connection before any test touches it.
        using var schema = CreateContext();
    }

    /// <summary>Creates a fresh context over the shared connection. Caller owns disposal.</summary>
    public PlexNotifierrDbContext CreateContext() => new(_options);

    /// <inheritdoc />
    public PlexNotifierrDbContext CreateDbContext() => CreateContext();

    /// <summary>
    /// Seeds data on a short-lived context and saves. Use this to arrange state without leaking a
    /// tracking context into the system-under-test.
    /// </summary>
    public void Seed(Action<PlexNotifierrDbContext> seed)
    {
        using var context = CreateContext();
        seed(context);
        context.SaveChanges();
    }

    public void Dispose() => _connection.Dispose();
}
