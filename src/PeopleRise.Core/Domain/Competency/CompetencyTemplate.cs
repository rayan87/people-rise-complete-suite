using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;

/// <summary>A family-and-level "cell" template (Core Spec §10) - the same coordinate system the
/// grading grid uses (Core Spec §5). JobFamilyId null is the level-only fallback that applies while
/// a job has no family. One template per (LevelId, JobFamilyId) cell, enforced at the DB level.</summary>
internal class CompetencyTemplate : Entity
{
    public Guid LevelId { get; private set; }

    public Level? Level { get; private set; }

    public Guid? JobFamilyId { get; private set; }   // null = the level-only fallback cell

    public JobFamily? JobFamily { get; private set; }

    private CompetencyTemplate() { }   // EF

    public static CompetencyTemplate Create(Guid levelId, Guid? jobFamilyId) =>
        new() { LevelId = levelId, JobFamilyId = jobFamilyId };
}

/// <summary>One competency required at one level within a template's cell.</summary>
internal class CompetencyTemplateItem : Entity
{
    public Guid TemplateId { get; private set; }

    public Guid CompetencyId { get; private set; }

    public CompetencyDefinition? Competency { get; private set; }

    public int RequiredLevel { get; private set; }   // 1-5

    private CompetencyTemplateItem() { }   // EF

    public static CompetencyTemplateItem Create(Guid templateId, Guid competencyId, int requiredLevel) =>
        new() { TemplateId = templateId, CompetencyId = competencyId, RequiredLevel = requiredLevel };

    public void SetRequiredLevel(int requiredLevel) => RequiredLevel = requiredLevel;
}
