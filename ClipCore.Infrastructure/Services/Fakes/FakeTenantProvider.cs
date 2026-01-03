using ClipCore.Core.Interfaces;

namespace ClipCore.Infrastructure.Services.Fakes;

public class FakeTenantProvider : ITenantProvider
{
    // Return a fixed GUID for development/testing so we always see data
    public Guid? TenantId => Guid.Parse("11111111-1111-1111-1111-111111111111");
}
