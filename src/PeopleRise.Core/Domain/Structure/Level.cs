using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;

internal class Level : Entity   // the five El-Delta levels
{
    public string Code { get; private set; } = "";

    public string NameEn { get; private set; } = "";

    public string? NameAr { get; private set; }

    public int Rank { get; private set; }

    public LevelStatus Status { get; private set; } = LevelStatus.Active;

    private Level() { }   // EF

    public static Level Create(string code, string nameEn, string? nameAr, int rank)
    {
        return new()
        {
            Code = code,
            NameEn = nameEn,
            NameAr = nameAr,
            Rank = rank
        };
    }

    public void Update(string code, string nameEn, string? nameAr, int rank)
    {
        Code = code;
        NameEn = nameEn;
        NameAr = nameAr;
        Rank = rank;
    }

    /// <summary>Closed, never deleted, once anything has referenced it (Core Spec §3.4).</summary>
    public void Close()
    {
        Status = LevelStatus.Closed;
    }
}

public enum LevelStatus
{
    Active,
    Closed
}
