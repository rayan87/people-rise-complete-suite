using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Domain;

/// <summary>
/// Versioning: evaluations pin a version, so re-tuning never re-grades old jobs
/// </summary>
internal class MethodologyVersion : Entity   
{
    public Guid MethodologyId { get; private set; }

    public Methodology? Methodology { get; private set; }

    public int VersionNo { get; private set; }

    public MethodologyVersionStatus Status { get; private set; } = MethodologyVersionStatus.Draft;

    public string? Note { get; private set; }

    public DateTime? PublishedAt { get; private set; }

    public ICollection<Factor>? Factors { get; private set; } 

    public ICollection<GradeMapping>? GradeMappings { get; private set; }

    private MethodologyVersion() { }   // EF

    public static MethodologyVersion CreateDraft(Guid methodologyId, 
        int versionNo, 
        string? note)
    {
        return new() 
        { 
            MethodologyId = methodologyId, 
            VersionNo = versionNo, 
            Note = note 
        };
    }

    /// <summary>
    /// Guard for every authoring write: a version is editable only while Draft.
    /// </summary>
    /// <exception cref="DomainStateException">If Status is Draft domain state exception is thrown.</exception>
    public void EnsureEditable()
    {
        if (Status != MethodologyVersionStatus.Draft)
        {
            throw new DomainStateException(
                $"Version is {Status}; only a Draft can be edited. Re-tuning publishes a new version.");
        }   
    }

    /// <summary>
    /// Publishes an editable version only if it has at least one factor and one grade mapping.
    /// </summary>
    /// <exception cref="DomainException">Throws a domain exception if version has no questions or grade mappings</exception>
    public void Publish()
    {
        EnsureEditable();

        if (Factors is null || Factors.Count == 0)
        {
            throw new DomainException("Cannot publish: the version has no questions.");
        }

        if (GradeMappings is null || GradeMappings.Count == 0)
        {
            throw new DomainException("Cannot publish: the version has no grade mappings.");
        }

        Status = MethodologyVersionStatus.Active;
        PublishedAt = DateTime.UtcNow;
    }

    public void Retire()
    {
        Status = MethodologyVersionStatus.Retired;
    }

    public Factor AddFactor(string code, 
        string nameEn,
        string? nameAr,
        decimal weight,
        int sortOrder)
    {
        EnsureEditable();

        if (Factors is null)
        {
            Factors = [];
        }

        var factor = Factor.Create(this, code, nameEn, nameAr, weight, sortOrder);
        Factors.Add(factor);
        return factor;
    }

    public Factor? UpdateFactor(Guid factorId,
        string code,
        string nameEn,
        string? nameAr,
        decimal weight,
        int sortOrder)
    {
        EnsureEditable();

        if (Factors is null)
        {
            throw new DomainException("Factors must be loaded first to update factor.");
        }

        var factor = Factors.FirstOrDefault(f => f.Id == factorId);
        
        if (factor is null)
        {
            return null;
        }

        factor.Update(code, nameEn, nameAr, weight, sortOrder);

        return factor;
    }


    public bool RemoveFactor(Guid factorId)
    {
        EnsureEditable();

        if (Factors is null)
        {
            throw new DomainException("Factors must be loaded first to remove the specified factorId.");
        }

        var factor = Factors.FirstOrDefault(f => f.Id == factorId);

        if (factor is null)
        {
            return false;
        }

        Factors.Remove(factor);

        return true;
    }

    public GradeMapping AddGradeMapping(Guid gradeId, int minScore, int maxScore)
    {
        EnsureEditable();

        if (GradeMappings is null)
        {
            GradeMappings = [];
        }

        var gradeMapping = GradeMapping.Create(this, gradeId, minScore, maxScore);
        GradeMappings.Add(gradeMapping);
        return gradeMapping;
    }

    public GradeMapping? UpdateGradeMapping(Guid gradeMappingId, 
        Guid gradeId, 
        int minScore, 
        int maxScore)
    {
        EnsureEditable();

        if (GradeMappings is null)
        {
            throw new DomainException("Grade mappings must be loaded first to update gradeMapping.");
        }

        var gradeMapping = GradeMappings.FirstOrDefault(f => f.Id == gradeMappingId);

        if (gradeMapping is null)
        {
            return null;
        }

        gradeMapping.Update(gradeId, minScore, maxScore);

        return gradeMapping;
    }

    public bool RemoveGradeMapping(Guid gradeMappingId)
    {
        EnsureEditable();

        if (GradeMappings is null)
        {
            throw new DomainException("Grade mappings must be loaded first to remove the specified gradeMappingId.");
        }

        var gradeMapping = GradeMappings.FirstOrDefault(f => f.Id == gradeMappingId);

        if (gradeMapping is null)
        {
            return false;
        }

        GradeMappings.Remove(gradeMapping);

        return true;
    }

}
