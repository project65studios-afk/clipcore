using System.Collections.Generic;
using System.Threading.Tasks;
using Project65.Core.Entities;

namespace Project65.Core.Interfaces
{
    public interface IAuditRepository
    {
        Task AddAsync(AuditLog log);
        Task<List<AuditLog>> GetAllAsync(int count = 100);
        Task<List<AuditLog>> GetByUserIdAsync(string userId, int count = 100);
        Task<List<AuditLog>> GetByEntityTypeAsync(string entityType, string entityId, int count = 100);
    }
}
