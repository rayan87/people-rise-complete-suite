using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;

/// <summary>Records which version of a versioned provisioning seed (the ISIC list now; the
/// competency seed once that spine is built) has been applied to this tenant. A later list version
/// is offered, never applied - adopting it must not silently reclassify a customer who already chose
/// a code (Core Spec §4). One row per named seed.</summary>
internal class SeedVersion : Entity
{
    public string Name { get; private set; } = "";   // e.g. "Isic"
    public int Version { get; private set; }
    public DateTime AppliedAt { get; private set; }

    private SeedVersion() { }   // EF

    public static SeedVersion Create(string name, int version) =>
        new() { Name = name, Version = version, AppliedAt = DateTime.UtcNow };
}
