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

    /// <summary>The floor of the total achievable score. Factor/question points are calculated from
    /// MaxPoints; with the unified 1-5 rating scale, the lowest possible total is MaxPoints / 5, so
    /// MinPoints is normally MaxPoints / 5 too - but it is stored (and editable) explicitly rather than
    /// derived, since it also anchors automatic grade-range tiling.</summary>
    public int MinPoints { get; private set; } = 200;

    public int MaxPoints { get; private set; } = 1000;

    public DateTime? PublishedAt { get; private set; }

    public ICollection<Factor>? Factors { get; private set; }

    public ICollection<GradeMapping>? GradeMappings { get; private set; }

    private MethodologyVersion() { }   // EF

    public static MethodologyVersion CreateDraft(Guid methodologyId,
        int versionNo,
        string? note,
        int minPoints = 200,
        int maxPoints = 1000)
    {
        EnsureValidPointBudget(minPoints, maxPoints);

        return new()
        {
            MethodologyId = methodologyId,
            VersionNo = versionNo,
            Note = note,
            MinPoints = minPoints,
            MaxPoints = maxPoints,
        };
    }

    /// <summary>Guard for every authoring write: a version is editable only while Draft.</summary>
    /// <exception cref="DomainStateException">If Status is Draft domain state exception is thrown.</exception>
    public void EnsureEditable()
    {
        if (Status != MethodologyVersionStatus.Draft)
        {
            throw new DomainStateException(
                $"Version is {Status}; only a Draft can be edited. Re-tuning publishes a new version.");
        }
    }

    public void SetPointBudget(int minPoints, int maxPoints)
    {
        EnsureEditable();
        EnsureValidPointBudget(minPoints, maxPoints);

        MinPoints = minPoints;
        MaxPoints = maxPoints;
    }

    private static void EnsureValidPointBudget(int minPoints, int maxPoints)
    {
        if (minPoints <= 0)
        {
            throw new DomainException("MinPoints must be positive.");
        }

        if (maxPoints <= minPoints)
        {
            throw new DomainException("MaxPoints must be greater than MinPoints.");
        }
    }

    /// <summary>
    /// Publishes an editable version only if its factors and questions are fully weighted (each set
    /// sums to 100%) and every assigned grade has a score range.
    /// </summary>
    /// <exception cref="DomainException">Thrown if the version isn't ready to publish.</exception>
    public void Publish()
    {
        EnsureEditable();

        if (Factors is null || Factors.Count == 0)
        {
            throw new DomainException("Cannot publish: the version has no factors.");
        }

        if (GradeMappings is null || GradeMappings.Count == 0)
        {
            throw new DomainException("Cannot publish: the version has no grade mappings.");
        }

        var factorWeightSum = Factors.Sum(f => f.Weight);
        if (Math.Abs(factorWeightSum - 100m) > 0.01m)
        {
            throw new DomainException($"Cannot publish: factor weights must sum to 100% (currently {factorWeightSum}%).");
        }

        foreach (var factor in Factors)
        {
            if (factor.Questions is null || factor.Questions.Count == 0)
            {
                throw new DomainException($"Cannot publish: factor '{factor.Code}' has no questions.");
            }

            var questionWeightSum = factor.Questions.Sum(q => q.Weight);
            if (Math.Abs(questionWeightSum - 100m) > 0.01m)
            {
                throw new DomainException(
                    $"Cannot publish: question weights in factor '{factor.Code}' must sum to 100% (currently {questionWeightSum}%).");
            }
        }

        if (GradeMappings.Any(g => g.MinScore is null || g.MaxScore is null))
        {
            throw new DomainException("Cannot publish: every assigned grade must have a score range set.");
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
        string? helpTextEn,
        string? helpTextAr,
        decimal weight,
        int sortOrder)
    {
        EnsureEditable();

        if (Factors is null)
        {
            Factors = [];
        }

        var factor = Factor.Create(this, code, nameEn, nameAr, helpTextEn, helpTextAr, weight, sortOrder);
        Factors.Add(factor);
        return factor;
    }

    public Factor? UpdateFactor(Guid factorId,
        string code,
        string nameEn,
        string? nameAr,
        string? helpTextEn,
        string? helpTextAr,
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

        factor.Update(code, nameEn, nameAr, helpTextEn, helpTextAr, weight, sortOrder);

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

    /// <summary>Creates a grade mapping. Pass minScore/maxScore null to assign the grade without a
    /// range yet (the first step of the two-step assign-then-range flow).</summary>
    public GradeMapping AddGradeMapping(Guid gradeId, int? minScore = null, int? maxScore = null)
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

    /// <summary>Step one of the two-step grade flow: attach a grade to this version with no score
    /// range yet.</summary>
    public GradeMapping AssignGrade(Guid gradeId) => AddGradeMapping(gradeId, null, null);

    /// <summary>Step two (manual mode): set a single grade mapping's score range by hand.</summary>
    public GradeMapping? SetGradeMappingRange(Guid gradeMappingId, int minScore, int maxScore)
    {
        EnsureEditable();

        if (GradeMappings is null)
        {
            throw new DomainException("Grade mappings must be loaded first to set a grade range.");
        }

        var gradeMapping = GradeMappings.FirstOrDefault(g => g.Id == gradeMappingId);

        if (gradeMapping is null)
        {
            return null;
        }

        gradeMapping.SetRange(minScore, maxScore);

        return gradeMapping;
    }

    /// <summary>Step two (automatic mode): tile the version's point budget evenly, with no gaps and
    /// no overlap, across the given grade mappings in rank order (lowest grade first). Callers resolve
    /// grade rank (Grade.Rank isn't visible to this aggregate) and pass mapping ids in that order.</summary>
    public void AutoAssignGradeRanges(IReadOnlyList<Guid> gradeMappingIdsInRankOrder)
    {
        EnsureEditable();

        if (GradeMappings is null)
        {
            throw new DomainException("Grade mappings must be loaded first to auto-assign ranges.");
        }

        if (gradeMappingIdsInRankOrder.Count == 0)
        {
            throw new DomainException("No grade mappings to assign ranges to.");
        }

        var byId = GradeMappings.ToDictionary(g => g.Id);

        if (gradeMappingIdsInRankOrder.Any(id => !byId.ContainsKey(id)))
        {
            throw new DomainException("One or more grade mappings do not belong to this version.");
        }

        var count = gradeMappingIdsInRankOrder.Count;
        var totalValues = MaxPoints - MinPoints + 1;   // inclusive integer range of achievable scores
        var baseWidth = totalValues / count;
        var remainder = totalValues % count;   // spread the leftover one point at a time across the first bands

        var cursor = MinPoints;
        for (var i = 0; i < count; i++)
        {
            var width = baseWidth + (i < remainder ? 1 : 0);
            var min = cursor;
            var max = cursor + width - 1;
            byId[gradeMappingIdsInRankOrder[i]].SetRange(min, max);
            cursor = max + 1;
        }
    }

    public GradeMapping? UpdateGradeMapping(Guid gradeMappingId,
        Guid gradeId,
        int? minScore,
        int? maxScore)
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
