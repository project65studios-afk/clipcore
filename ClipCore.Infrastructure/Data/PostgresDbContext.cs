using Microsoft.EntityFrameworkCore;
using ClipCore.Core.Entities;

namespace ClipCore.Infrastructure.Data;

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
