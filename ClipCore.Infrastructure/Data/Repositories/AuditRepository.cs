using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClipCore.Core.Entities;
using ClipCore.Core.Interfaces;

namespace ClipCore.Infrastructure.Data.Repositories
{
    public class AuditRepository : IAuditRepository
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public AuditRepository(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task AddAsync(AuditLog log)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            context.AuditLogs.Add(log);
            await context.SaveChangesAsync();
        }

        public async Task<List<AuditLog>> GetAllAsync(int count = 100)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetByUserIdAsync(string userId, int count = 100)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.AuditLogs
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetByEntityTypeAsync(string entityType, string entityId, int count = 100)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.AuditLogs
                .Where(l => l.EntityType == entityType && l.EntityId == entityId)
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .ToListAsync();
        }
    }
}
