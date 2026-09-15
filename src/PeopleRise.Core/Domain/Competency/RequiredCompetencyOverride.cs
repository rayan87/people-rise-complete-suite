using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;

/// <summary>Required profiles bind to the JOB (Core Spec §10): a job inherits its family-and-level
/// cell's CompetencyTemplate and may override individual competencies here - set a different
/// RequiredLevel, add one the template doesn't have, or exclude an inherited one (RequiredLevel
/// null). The effective required profile for a job is always the template's items merged with these
/// overrides, computed at read time (see RequiredProfileProjections) - never materialized as a
/// separate stored copy, so a template edit fans out to every job that inherits it without a sync
/// step. AuthoredFrameworkVersion records the competency seed version in force when this job's
/// FIRST override was authored - information only, never a resolution rule (Core Spec §10:
/// "framework versions are recorded, not pinned" - unlike an Evaluation's MethodologyVersion, a
/// required profile always resolves against the CURRENT framework).</summary>
internal class RequiredCompetencyOverride : Entity
{
    public Guid JobId { get; private set; }

    public Guid CompetencyId { get; private set; }

    public CompetencyDefinition? Competency { get; private set; }

    public int? RequiredLevel { get; private set; }   // null = excluded (removes an inherited template item)

    public int AuthoredFrameworkVersion { get; private set; }

    private RequiredCompetencyOverride() { }   // EF

    public static RequiredCompetencyOverride Create(Guid jobId, Guid competencyId, int? requiredLevel, int authoredFrameworkVersion)
    {
        return new()
        {
            JobId = jobId,
            CompetencyId = competencyId,
            RequiredLevel = requiredLevel,
            AuthoredFrameworkVersion = authoredFrameworkVersion,
        };
    }

    public void SetRequiredLevel(int? requiredLevel) => RequiredLevel = requiredLevel;
}
