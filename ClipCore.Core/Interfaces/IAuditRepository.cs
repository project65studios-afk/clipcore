using System.Collections.Generic;
using System.Threading.Tasks;
using ClipCore.Core.Entities;

namespace ClipCore.Core.Interfaces
{
    public interface IAuditRepository
    {
        Task AddAsync(AuditLog log);
        Task<List<AuditLog>> GetAllAsync(int count = 100);
        Task<List<AuditLog>> GetByUserIdAsync(string userId, int count = 100);
        Task<List<AuditLog>> GetByEntityTypeAsync(string entityType, string entityId, int count = 100);
    }
}
