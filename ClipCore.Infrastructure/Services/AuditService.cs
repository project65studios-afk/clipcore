using System;
using System.Threading.Tasks;
using ClipCore.Core.Entities;
using ClipCore.Core.Interfaces;

namespace ClipCore.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly IAuditRepository _auditRepository;

        public AuditService(IAuditRepository auditRepository)
        {
            _auditRepository = auditRepository;
        }

        public async Task LogActionAsync(string? userId, string? userEmail, string action, string? entityType = null, string? entityId = null, string? details = null, string? ipAddress = null)
        {
            var log = new AuditLog
            {
                UserId = userId,
                UserEmail = userEmail,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress
            };

            await _auditRepository.AddAsync(log);
        }
    }
}
