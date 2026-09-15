using PeopleRise.SharedKernel;
using System.ComponentModel.DataAnnotations.Schema;

namespace PeopleRise.Core.Domain;

/// <summary>Salary bands, held in the core (Core Spec §9: "salary bands with provenance" - the same
/// shape as grade assignment, §11.2's write-contract table: Core UI · Compensation, ManuallyEntered
/// · Designed). Bands exist without Compensation: a customer enters min/midpoint/max by hand
/// (ManuallyEntered) with spread/overlap derived read-only values; with Compensation the band is
/// Designed and the half-spread is a stored, editable input. Min/max are always derived from
/// midpoint +/- HalfSpreadPct; spread & overlap are derived outputs, never set directly.</summary>
internal class SalaryBand : Entity
{
    private const decimal DefaultHalfSpreadPct = 25m;   // editable per band, defaults to 25%

    public Guid GradeId { get; private set; }

    public Guid? JobFamilyId { get; private set; }

    public PayBasis Basis { get; private set; } = PayBasis.Basic;   // which basis this band prices

    public string Currency { get; private set; } = "";

    public decimal Midpoint { get; private set; }

    public decimal HalfSpreadPct { get; private set; } = DefaultHalfSpreadPct;

    public decimal MinAmount { get; private set; }

    public decimal MaxAmount { get; private set; }

    public decimal? OverlapPct { get; private set; }   // (this midpoint / previous grade's midpoint) - 1; null for the first grade

    public BandProvenance Provenance { get; private set; }

    public DateOnly EffectiveDate { get; private set; }

    public BandStatus Status { get; private set; } = BandStatus.Draft;

    [NotMapped]
    public decimal SpreadPct => MinAmount <= 0 ? 0m : (MaxAmount / MinAmount - 1m) * 100m;

    private SalaryBand() { }   // EF

    public static SalaryBand Create(Guid gradeId,
        string currency,
        decimal midpoint,
        decimal? previousGradeMidpoint,
        DateOnly effectiveDate,
        BandProvenance provenance,
        BandStatus status = BandStatus.Published,
        Guid? jobFamilyId = null,
        decimal halfSpreadPct = DefaultHalfSpreadPct,
        PayBasis basis = PayBasis.Basic)
    {
        var band = new SalaryBand
        {
            GradeId = gradeId,
            JobFamilyId = jobFamilyId,
            Currency = currency,
            EffectiveDate = effectiveDate,
            Status = status,
            Provenance = provenance,
            Basis = basis,
        };
        band.ApplyMidpoint(midpoint, previousGradeMidpoint, halfSpreadPct);
        return band;
    }

    /// <summary>Re-price the band: min/max/overlap are always derived from midpoint (never set
    /// directly). Provenance is never touched here - a re-price isn't a new source; a genuinely new
    /// source means a new record (see BandProvenance's writers), not an in-place change.</summary>
    public void Update(decimal midpoint, decimal? previousGradeMidpoint, string currency, DateOnly effectiveDate, decimal? halfSpreadPct = null)
    {
        Currency = currency;
        EffectiveDate = effectiveDate;
        ApplyMidpoint(midpoint, previousGradeMidpoint, halfSpreadPct ?? HalfSpreadPct);
    }

    public void Retire()
    {
        Status = BandStatus.Retired;
    }

    private void ApplyMidpoint(decimal midpoint, decimal? previousGradeMidpoint, decimal halfSpreadPct)
    {
        Midpoint = midpoint;
        HalfSpreadPct = halfSpreadPct;

        var half = halfSpreadPct / 100m;
        MinAmount = decimal.Round(midpoint * (1m - half), 4);
        MaxAmount = decimal.Round(midpoint * (1m + half), 4);

        OverlapPct = previousGradeMidpoint is { } prev && prev > 0
            ? decimal.Round((midpoint / prev - 1m) * 100m, 4)
            : null;
    }
}

public enum BandProvenance 
{ 
    Designed, 
    ManuallyEntered 
}

public enum BandStatus 
{ 
    Draft, 
    Published, 
    Retired 
}

