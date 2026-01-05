using Microsoft.EntityFrameworkCore;
using Project65.Core.Entities;

namespace Project65.Infrastructure.Data;

public class PostgresDbContext : AppDbContext
{
    public PostgresDbContext(DbContextOptions<PostgresDbContext> options)
        : base(options)
    {
    }

    // Required constructor for base class if it has one
    protected PostgresDbContext(DbContextOptions options) : base(options)
    {
    }
}
