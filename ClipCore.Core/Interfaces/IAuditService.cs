using System.Threading.Tasks;

namespace ClipCore.Core.Interfaces
{
    public interface IAuditService
    {
        Task LogActionAsync(string? userId, string? userEmail, string action, string? entityType = null, string? entityId = null, string? details = null, string? ipAddress = null);
    }
}
