using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;

internal class JobFamily : Entity   // horizontal cut; nullable on Job, added in the design phase
{
    public string Code { get; private set; } = "";

    public string NameEn { get; private set; } = "";

    public string? NameAr { get; private set; }

    public JobFamilyStatus Status { get; private set; } = JobFamilyStatus.Active;

    private JobFamily() { }   // EF

    public static JobFamily Create(string code, string nameEn, string? nameAr)
    {
        return new()
        {
            Code = code,
            NameEn = nameEn,
            NameAr = nameAr
        };
    }

    public void Update(string code, string nameEn, string? nameAr)
    {
        Code = code;
        NameEn = nameEn;
        NameAr = nameAr;
    }

    /// <summary>Closed, never deleted, once anything has referenced it (Core Spec §3.4).</summary>
    public void Close()
    {
        Status = JobFamilyStatus.Closed;
    }
}

public enum JobFamilyStatus
{
    Active,
    Closed
}
