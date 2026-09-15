using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;


/// <summary>An employee's amount for one pay element, effective-dated, with provenance (Core Spec
/// §9 - "the mirror test's canonical case"). Written today only through the Core UI path
/// (SetPayElementAmountCommand, provenance Manual); Personnel/Compensation/Payroll each get their
/// own writer with their own provenance value once those products exist.</summary>
internal class EmployeePayElementAmount : Entity
{
    public Guid EmployeeId { get; private set; }   // id only - Employee lives alongside, in Core

    public Guid PayElementId { get; private set; }

    public PayElement? PayElement { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = "";

    public DateOnly EffectiveDate { get; private set; }

    public DateOnly? EndDate { get; private set; }   // null = current

    public PayProvenance Provenance { get; private set; }

    private EmployeePayElementAmount() { }   // EF

    public static EmployeePayElementAmount Create(Guid employeeId, 
        Guid payElementId, decimal amount, string currency,
        DateOnly effectiveDate, PayProvenance provenance)
    {
        return new()
        {
            EmployeeId = employeeId,
            PayElementId = payElementId,
            Amount = amount,
            Currency = currency,
            EffectiveDate = effectiveDate,
            Provenance = provenance,
        };
    }

    public void End(DateOnly endDate)
    {
        EndDate = endDate;
    }
}

public enum PayProvenance
{
    Contract,
    Policy,
    Import,
    PayrollRun,
    Manual
}   // Core Spec §9 / §3.1

