using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;

internal class Grade : Entity
{
    public string Code { get; private set; } = "";

    public string NameEn { get; private set; } = "";

    public string? NameAr { get; private set; }

    public int Rank { get; private set; }

    public Guid LevelId { get; private set; }

    public Level? Level { get; private set; }

    public GradeStatus Status { get; private set; } = GradeStatus.Active;

    private Grade() { }   // EF

    public static Grade Create(string code, string nameEn, string? nameAr, int rank, Guid levelId)
    {
        return new()
        {
            Code = code,
            NameEn = nameEn,
            NameAr = nameAr,
            Rank = rank,
            LevelId = levelId
        };
    }

    public void Update(string code, string nameEn, string? nameAr, int rank, Guid levelId)
    {
        Code = code;
        NameEn = nameEn;
        NameAr = nameAr;
        Rank = rank;
        LevelId = levelId;
    }

    /// <summary>Closed, never deleted, once a job has ever been assigned to it (Core Spec §6).</summary>
    public void Close()
    {
        Status = GradeStatus.Closed;
    }
}

public enum GradeSource
{
    ManuallyAssigned,
    Evaluated
}

public enum GradeStatus
{
    Active,
    Closed
}   // Core Spec §6: closed, never deleted, once a job has ever been assigned to it
