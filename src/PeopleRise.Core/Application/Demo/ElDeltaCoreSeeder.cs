using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;

namespace PeopleRise.Core.Application.Demo;

/// <summary>Ids of the core rows the El-Delta demo seed created, keyed by their business code, so
/// JobReward's own seed phase (methodology, grade mappings, evaluations, salary bands) can resolve
/// them without re-querying or re-creating anything.</summary>
public sealed record ElDeltaCoreSeedResult(
    IReadOnlyDictionary<string, Guid> LevelIdsByCode,
    IReadOnlyDictionary<string, Guid> JobFamilyIdsByCode,
    IReadOnlyDictionary<string, Guid> GradeIdsByCode,
    IReadOnlyDictionary<string, Guid> JobIdsByCode);

/// <summary>Seeds the core slice of the El-Delta demo dataset: the five levels, the twelve IT job
/// families, the twelve-grade ladder, and the ~39 unGraded job definitions. Grading, evaluation,
/// methodology, and salary bands are JobReward's own seed phase (ElDeltaDemoSeeder), which runs
/// after this and consumes the ids returned here.</summary>
internal static class ElDeltaCoreSeeder
{
    public static async Task<ElDeltaCoreSeedResult> SeedAsync(CoreDbContext db, CancellationToken ct = default)
    {
        // ---- Levels (El-Delta's five; C-level is not run through the evaluation questionnaire) ----
        var levelDefs = new (string Code, string En, string Ar, int Rank)[]
        {
            ("BC",   "Blue Collar",            "ياقة زرقاء", 1),
            ("IC",   "Individual Contributor", "فرد مساهم",  2),
            ("SUP",  "Supervisory",            "إشرافي",     3),
            ("MGR",  "Managerial",             "إداري",      4),
            ("EXEC", "C-Level",                "تنفيذي",     5),
        };
        var levels = levelDefs.ToDictionary(d => d.Code, d => Level.Create(d.Code, d.En, d.Ar, d.Rank));
        db.Levels.AddRange(levels.Values);

        // ---- Job families ----
        var familyDefs = new (string Code, string En, string Ar)[]
        {
            ("ENG",    "Software Engineering",         "هندسة البرمجيات"),
            ("QA",     "Quality Assurance",            "ضمان الجودة"),
            ("DEVOPS", "DevOps & Infrastructure",      "العمليات والبنية التحتية"),
            ("DATA",   "Data & Analytics",             "البيانات والتحليلات"),
            ("PROD",   "Product & Design",             "المنتج والتصميم"),
            ("SEC",    "Information Security",          "أمن المعلومات"),
            ("ITSUP",  "IT Support",                   "الدعم الفني"),
            ("PMO",    "Project Management",           "إدارة المشاريع"),
            ("HR",     "Human Resources",              "الموارد البشرية"),
            ("FIN",    "Finance & Accounting",         "المالية والمحاسبة"),
            ("SAL",    "Sales & Business Development",  "المبيعات وتطوير الأعمال"),
            ("ADM",    "Administration",               "الإدارة"),
        };
        var families = familyDefs.ToDictionary(d => d.Code, d => JobFamily.Create(d.Code, d.En, d.Ar));
        db.JobFamilies.AddRange(families.Values);

        // ---- Grade grid (G1..G12 across the five levels) ----
        var gradeDefs = new (string Code, int Rank, string Level)[]
        {
            ("G1", 1, "BC"), ("G2", 2, "BC"),
            ("G3", 3, "IC"), ("G4", 4, "IC"), ("G5", 5, "IC"), ("G6", 6, "IC"),
            ("G7", 7, "SUP"), ("G8", 8, "SUP"),
            ("G9", 9, "MGR"), ("G10", 10, "MGR"), ("G11", 11, "MGR"),
            ("G12", 12, "EXEC"),
        };
        var grades = gradeDefs.ToDictionary(
            d => d.Code, d => Grade.Create(d.Code, $"Grade {d.Rank}", $"الدرجة {d.Rank}", d.Rank, levels[d.Level].Id));
        db.Grades.AddRange(grades.Values);

        // ---- Jobs (bilingual titles, ungraded - JobReward's seed phase grades them via evaluation).
        // BC=Blue Collar, IC=Individual Contributor, SUP=Supervisory, MGR=Managerial, EXEC=C-Level ----
        var jobDefs = new (string Code, string En, string Ar, string Family)[]
        {
            ("ENG-SE1", "Junior Software Engineer", "مهندس برمجيات مبتدئ", "ENG"),          // IC
            ("ENG-SE2", "Software Engineer", "مهندس برمجيات", "ENG"),                      // IC
            ("ENG-SE3", "Senior Software Engineer", "مهندس برمجيات أول", "ENG"),           // IC
            ("ENG-TL",  "Tech Lead", "قائد تقني", "ENG"),                                  // SUP
            ("ENG-EM",  "Engineering Manager", "مدير هندسة", "ENG"),                       // MGR
            ("QA-1",    "QA Engineer", "مهندس ضمان جودة", "QA"),                           // IC
            ("QA-2",    "Senior QA Engineer", "مهندس ضمان جودة أول", "QA"),                // IC
            ("QA-L",    "QA Lead", "قائد ضمان الجودة", "QA"),                              // SUP
            ("OPS-1",   "DevOps Engineer", "مهندس عمليات", "DEVOPS"),                      // IC
            ("OPS-2",   "Senior DevOps Engineer", "مهندس عمليات أول", "DEVOPS"),           // IC
            ("OPS-L",   "Infrastructure Lead", "قائد البنية التحتية", "DEVOPS"),           // SUP
            ("OPS-M",   "Infrastructure Manager", "مدير البنية التحتية", "DEVOPS"),        // MGR
            ("DAT-AN",  "Data Analyst", "محلل بيانات", "DATA"),                            // IC
            ("DAT-DE",  "Data Engineer", "مهندس بيانات", "DATA"),                          // IC
            ("DAT-SE",  "Senior Data Engineer", "مهندس بيانات أول", "DATA"),               // IC
            ("DAT-L",   "Data & Analytics Lead", "قائد البيانات والتحليلات", "DATA"),      // SUP
            ("PRD-DS",  "Product Designer", "مصمم منتج", "PROD"),                          // IC
            ("PRD-PM",  "Product Manager", "مدير منتج", "PROD"),                           // SUP
            ("PRD-SPM", "Senior Product Manager", "مدير منتج أول", "PROD"),                // MGR
            ("SEC-1",   "Security Engineer", "مهندس أمن معلومات", "SEC"),                  // IC
            ("SEC-L",   "Security Lead", "قائد أمن المعلومات", "SEC"),                     // SUP
            ("SUP-1",   "IT Support Specialist", "أخصائي دعم فني", "ITSUP"),               // BC
            ("SUP-2",   "Senior IT Support Specialist", "أخصائي دعم فني أول", "ITSUP"),    // IC
            ("SUP-S",   "IT Support Supervisor", "مشرف دعم فني", "ITSUP"),                 // SUP
            ("PMO-C",   "Project Coordinator", "منسق مشاريع", "PMO"),                      // IC
            ("PMO-PM",  "Project Manager", "مدير مشروع", "PMO"),                           // SUP
            ("PMO-L",   "PMO Lead", "قائد مكتب إدارة المشاريع", "PMO"),                    // MGR
            ("HR-1",    "HR Specialist", "أخصائي موارد بشرية", "HR"),                      // IC
            ("HR-M",    "HR Manager", "مدير موارد بشرية", "HR"),                           // MGR
            ("FIN-1",   "Accountant", "محاسب", "FIN"),                                     // IC
            ("FIN-2",   "Senior Accountant", "محاسب أول", "FIN"),                          // IC
            ("FIN-M",   "Finance Manager", "مدير مالي", "FIN"),                            // MGR
            ("SAL-1",   "Sales Executive", "تنفيذي مبيعات", "SAL"),                        // IC
            ("SAL-AM",  "Account Manager", "مدير حسابات", "SAL"),                          // SUP
            ("SAL-M",   "Sales Manager", "مدير مبيعات", "SAL"),                            // MGR
            ("ADM-1",   "Office Administrator", "مسؤول إداري", "ADM"),                     // BC
            ("ADM-S",   "Admin Supervisor", "مشرف إداري", "ADM"),                          // SUP
            ("EXE-CTO", "Chief Technology Officer", "الرئيس التنفيذي للتقنية", "ENG"),     // EXEC
            ("EXE-CEO", "Chief Executive Officer", "الرئيس التنفيذي", "ADM"),              // EXEC
        };
        var jobs = jobDefs.ToDictionary(
            d => d.Code, d => Job.Create(d.Code, d.En, d.Ar, null, null, families[d.Family].Id));
        db.Jobs.AddRange(jobs.Values);

        await db.SaveChangesAsync(ct);

        return new ElDeltaCoreSeedResult(
            levels.ToDictionary(kv => kv.Key, kv => kv.Value.Id),
            families.ToDictionary(kv => kv.Key, kv => kv.Value.Id),
            grades.ToDictionary(kv => kv.Key, kv => kv.Value.Id),
            jobs.ToDictionary(kv => kv.Key, kv => kv.Value.Id));
    }
}
