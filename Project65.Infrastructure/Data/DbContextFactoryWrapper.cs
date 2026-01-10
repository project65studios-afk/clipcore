using Microsoft.EntityFrameworkCore;

namespace Project65.Infrastructure.Data;

public class DbContextFactoryWrapper : IDbContextFactory<AppDbContext>
{
    private readonly IDbContextFactory<PostgresDbContext> _postgresFactory;

    public DbContextFactoryWrapper(IDbContextFactory<PostgresDbContext> postgresFactory)
    {
        _postgresFactory = postgresFactory;
    }

    public AppDbContext CreateDbContext()
    {
        return _postgresFactory.CreateDbContext();
    }
}
