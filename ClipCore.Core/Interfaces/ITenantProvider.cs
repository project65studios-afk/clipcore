namespace ClipCore.Core.Interfaces;

public interface ITenantProvider
{
    Guid? TenantId { get; }
}
