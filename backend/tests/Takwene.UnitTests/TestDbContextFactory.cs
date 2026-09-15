using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Takwene.Infrastructure.Persistence;

namespace Takwene.UnitTests;

public static class TestDbContextFactory
{
    public static (TakweneDbContext Context, SqliteConnection Connection) CreateInMemoryDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TakweneDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new TakweneDbContext(options);
        context.Database.EnsureCreated();

        return (context, connection);
    }
}
