using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;

namespace PeopleRise.Core.Application.Provisioning;

/// <summary>Seeds the starting competency framework (Core Spec §10): five to eight universal
/// bilingual behavioural competencies, shipped as a versioned data file in the provisioning path,
/// exactly like the ISIC list (IsicSeeder). Unlike ISIC, these are fully editable and deletable -
/// this is a deliberate starting point, not a standard.</summary>
internal static class CompetencySeeder
{
    public const string SeedName = "Competency";
    public const int CurrentVersion = 1;

    public static async Task SeedAsync(CoreDbContext db, CancellationToken ct = default)
    {
        if (await db.SeedVersions.AnyAsync(v => v.Name == SeedName, ct))
        {
            return;   // idempotent - already applied
        }

        var defs = new (string Code, string En, string Ar, string DescEn, string DescAr)[]
        {
            ("COMM", "Communication", "التواصل",
                "Conveys information clearly and listens effectively, adjusting style to the audience.",
                "ينقل المعلومات بوضوح ويستمع بفعالية، ويكيّف أسلوبه مع الجمهور."),
            ("TEAM", "Teamwork & Collaboration", "العمل الجماعي والتعاون",
                "Works effectively with others toward shared goals, sharing credit and support.",
                "يعمل بفعالية مع الآخرين لتحقيق أهداف مشتركة، ويشارك الفضل والدعم."),
            ("PROB", "Problem Solving", "حل المشكلات",
                "Identifies issues, analyses causes, and works through to a practical resolution.",
                "يحدد المشكلات، ويحلل الأسباب، ويتوصل إلى حل عملي."),
            ("ADAPT", "Adaptability", "القدرة على التكيف",
                "Adjusts approach and stays effective when priorities, tools, or conditions change.",
                "يعدّل أسلوبه ويظل فعالاً عند تغير الأولويات أو الأدوات أو الظروف."),
            ("OWN", "Ownership & Accountability", "الملكية والمساءلة",
                "Takes responsibility for outcomes, follows through, and learns from mistakes.",
                "يتحمل مسؤولية النتائج، ويتابع تنفيذها، ويتعلم من الأخطاء."),
            ("LEAD", "Leadership & Influence", "القيادة والتأثير",
                "Guides, motivates, and earns the trust of others toward a shared direction.",
                "يوجّه الآخرين ويحفزهم ويكسب ثقتهم لتحقيق اتجاه مشترك."),
            ("PLAN", "Planning & Organization", "التخطيط والتنظيم",
                "Structures work realistically, sets priorities, and manages time against deadlines.",
                "ينظم العمل بواقعية، ويحدد الأولويات، ويدير الوقت في مواجهة المواعيد النهائية."),
            ("CUST", "Customer & Stakeholder Focus", "التركيز على العملاء وأصحاب المصلحة",
                "Understands stakeholder needs and delivers work that genuinely serves them.",
                "يفهم احتياجات أصحاب المصلحة وينجز عملاً يخدمهم فعلياً."),
        };

        foreach (var (code, en, ar, descEn, descAr) in defs)
        {
            db.CompetencyDefinitions.Add(CompetencyDefinition.Create(code, en, ar, descEn, descAr, CompetencyProvenance.Seeded));
        }

        db.SeedVersions.Add(SeedVersion.Create(SeedName, CurrentVersion));

        await db.SaveChangesAsync(ct);
    }
}
