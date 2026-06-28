namespace PeopleRise.Modules.JobReward.Domain;

/// <summary>A domain invariant was violated. Handlers map this to a validation error (400).</summary>
internal class DomainException(string message) : Exception(message);

/// <summary>An operation was attempted in the wrong state (e.g. editing a published version,
/// approving a draft). Handlers map this to a conflict (409).</summary>
internal sealed class DomainStateException(string message) : DomainException(message);
