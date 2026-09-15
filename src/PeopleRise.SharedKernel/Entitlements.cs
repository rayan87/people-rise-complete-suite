namespace PeopleRise.SharedKernel;

/// <summary>Answers "does this tenant have product X" (Core Spec §11.4). Built now, before there is
/// anything to entitle, because retrofitting it later would mean auditing every endpoint. The core
/// never consults it - reads are never gated on entitlement; it exists for products to call.
/// Wired at module registration (see CoreModule.AddCoreModule), returning true for everything until
/// there is a real accounts/subscription structure to back it (Decision Log §2: deferred, cheap to
/// add later since it lives entirely in the control plane).</summary>
public interface IEntitlementService
{
    Task<bool> HasProductAsync(string productKey, CancellationToken ct = default);
}

public sealed class AlwaysEntitledService : IEntitlementService
{
    public Task<bool> HasProductAsync(string productKey, CancellationToken ct = default) => Task.FromResult(true);
}
