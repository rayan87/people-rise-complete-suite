using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;

/// <summary>The catalogue of pay elements (basic, transport allowance, call allowance, bonus, ...).
/// Core Spec §9: three products write per-employee amounts against these (Personnel from a
/// contract, Compensation from a policy rule, Payroll from a run) - the mirror test's canonical
/// case. The core owns the element; each product owns its own view of it (allowance policy rules
/// are Compensation's, fiscal treatment is Payroll's - neither lives here).</summary>
internal class PayElement : Entity
{
    public string Code { get; private set; } = "";
    public string NameEn { get; private set; } = "";
    public string? NameAr { get; private set; }
    public PayBasis Basis { get; private set; }   // which basis this element counts toward

    private PayElement() { }   // EF

    public static PayElement Create(string code, string nameEn, string? nameAr, PayBasis basis)
    {
        return new() 
        { 
            Code = code, 
            NameEn = nameEn, 
            NameAr = nameAr, 
            Basis = basis 
        };
    }
        
    public void Update(string code, string nameEn, string? nameAr, PayBasis basis)
    { 
        Code = code; 
        NameEn = nameEn; 
        NameAr = nameAr; 
        Basis = basis; 
    }
}

public enum PayBasis 
{ 
    Basic, 
    TotalFixed, 
    TotalCash 
}   // Core Spec §9: "pay basis is explicit everywhere"

