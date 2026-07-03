using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;

namespace PeopleRise.Modules.JobReward.Application.Demo;

/// <summary>
/// Seeds a fresh tenant DB with a realistic, bilingual (EN/AR) dataset for the pilot customer:
/// El-Delta, an Egyptian IT company (150–250 staff). Produces the full design-time chain — levels,
/// IT job families, a grade grid, a published point-factor methodology, ~39 jobs, scored evaluations,
/// and EGP salary bands. All entities are created through their domain factories; evaluations go
/// through the real Draft → Submit → Approve transitions.
/// </summary>
internal static class ElDeltaDemoSeeder
{
    public static async Task<DemoSeedSummary> SeedAsync(JobRewardDbContext db, CancellationToken ct = default)
    {
        // ---- Levels (El-Delta's five; C-level is out of evaluation scope) ----
        var levelDefs = new (string Code, string En, string Ar, int Rank, bool Scope)[]
        {
            ("BC",   "Blue Collar",            "ياقة زرقاء", 1, true),
            ("IC",   "Individual Contributor", "فرد مساهم",  2, true),
            ("SUP",  "Supervisory",            "إشرافي",     3, true),
            ("MGR",  "Managerial",             "إداري",      4, true),
            ("EXEC", "C-Level",                "تنفيذي",     5, false),
        };
        var levels = levelDefs.ToDictionary(d => d.Code, d => Level.Create(d.Code, d.En, d.Ar, d.Rank, d.Scope));
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

        // ---- Methodology: a 5-factor point-factor questionnaire (options score 4/8/12/16/20) ----
        var methodology = Methodology.Create("ELD-PF", "El-Delta Point-Factor", "نظام النقاط والعوامل — الدلتا");
        db.Methodologies.Add(methodology);
        var version = MethodologyVersion.CreateDraft(methodology.Id, 1, "Initial El-Delta methodology");
        db.MethodologyVersions.Add(version);

        var factorDefs = new (string Code, string NameEn, string NameAr, string QEn, string QAr, (string En, string Ar)[] Options)[]
        {
            ("KNOW", "Know-How (Education & Technical Expertise)", "المعرفة (التعليم والخبرة التقنية)",
                "Required education and technical depth", "المستوى التعليمي والعمق التقني المطلوب",
                [("Basic / vocational", "أساسي / مهني"), ("Diploma / some specialization", "دبلوم / بعض التخصص"),
                 ("Bachelor's in field", "بكالوريوس في المجال"), ("Bachelor's + deep specialization", "بكالوريوس + تخصص عميق"),
                 ("Master's / recognized expert", "ماجستير / خبير معتمد")]),
            ("EXP", "Experience", "الخبرة",
                "Years of relevant experience", "سنوات الخبرة ذات الصلة",
                [("Under 1 year", "أقل من سنة"), ("1–3 years", "1–3 سنوات"), ("3–6 years", "3–6 سنوات"),
                 ("6–10 years", "6–10 سنوات"), ("10+ years", "أكثر من 10 سنوات")]),
            ("PROB", "Problem Solving & Complexity", "حل المشكلات والتعقيد",
                "Nature of the problems handled", "طبيعة المشكلات التي يتم التعامل معها",
                [("Routine, well-defined", "روتيني ومحدد جيدًا"), ("Some analysis required", "يتطلب بعض التحليل"),
                 ("Varied, needs judgment", "متنوع ويحتاج إلى حكم"), ("Complex and ambiguous", "معقد وغامض"),
                 ("Strategic and novel", "استراتيجي وجديد")]),
            ("ACC", "Accountability & Impact", "المسؤولية والأثر",
                "Impact of the role's decisions", "أثر قرارات الوظيفة",
                [("Own tasks only", "المهام الخاصة فقط"), ("Team deliverables", "مخرجات الفريق"),
                 ("Function / project outcomes", "نتائج الوظيفة / المشروع"), ("Department results", "نتائج الإدارة"),
                 ("Organisation-wide / financial", "على مستوى المؤسسة / مالي")]),
            ("SUP", "Supervision & People Management", "الإشراف وإدارة الأفراد",
                "People-management responsibility", "مسؤولية إدارة الأفراد",
                [("None", "لا يوجد"), ("Guides juniors", "يوجه المبتدئين"), ("Leads a small team", "يقود فريقًا صغيرًا"),
                 ("Manages a team", "يدير فريقًا"), ("Manages managers", "يدير المديرين")]),
        };
        var pointScale = new[] { 4, 8, 12, 16, 20 };

        var factorBuilds = new List<(Factor Factor, Question Question, AnswerOption[] Options)>();
        var sort = 1;
        foreach (var d in factorDefs)
        {
            var factor = version.AddFactor(d.Code, d.NameEn, d.NameAr, 1m, sort++);
            var question = factor.AddQuestion(d.QEn, d.QAr, null, null, QuestionType.SingleChoice, 1);
            var options = d.Options
                .Select((o, i) => question.AddAnswerOption(o.En, o.Ar, pointScale[i], i + 1))
                .ToArray();

            factorBuilds.Add((factor, question, options));
        }

        // ---- Grade mappings (score 20..100 → grades G1..G11; G12 exec is out of eval scope) ----
        var mapDefs = new (string Grade, int Min, int Max)[]
        {
            ("G1", 20, 27), ("G2", 28, 34), ("G3", 35, 41), ("G4", 42, 48), ("G5", 49, 55),
            ("G6", 56, 62), ("G7", 63, 69), ("G8", 70, 76), ("G9", 77, 83), ("G10", 84, 91), ("G11", 92, 100),
        };

        foreach (var m in mapDefs)
            version.AddGradeMapping(grades[m.Grade].Id, m.Min, m.Max);

        Guid? ResolveGrade(int total) => mapDefs
            .Where(m => m.Min <= total && total <= m.Max)
            .Select(m => (Guid?)grades[m.Grade].Id)
            .FirstOrDefault();

        version.Publish();

        // ---- Jobs (bilingual titles) ----
        var jobDefs = new (string Code, string En, string Ar, string Level, string Family)[]
        {
            ("ENG-SE1", "Junior Software Engineer", "مهندس برمجيات مبتدئ", "IC", "ENG"),
            ("ENG-SE2", "Software Engineer", "مهندس برمجيات", "IC", "ENG"),
            ("ENG-SE3", "Senior Software Engineer", "مهندس برمجيات أول", "IC", "ENG"),
            ("ENG-TL",  "Tech Lead", "قائد تقني", "SUP", "ENG"),
            ("ENG-EM",  "Engineering Manager", "مدير هندسة", "MGR", "ENG"),
            ("QA-1",    "QA Engineer", "مهندس ضمان جودة", "IC", "QA"),
            ("QA-2",    "Senior QA Engineer", "مهندس ضمان جودة أول", "IC", "QA"),
            ("QA-L",    "QA Lead", "قائد ضمان الجودة", "SUP", "QA"),
            ("OPS-1",   "DevOps Engineer", "مهندس عمليات", "IC", "DEVOPS"),
            ("OPS-2",   "Senior DevOps Engineer", "مهندس عمليات أول", "IC", "DEVOPS"),
            ("OPS-L",   "Infrastructure Lead", "قائد البنية التحتية", "SUP", "DEVOPS"),
            ("OPS-M",   "Infrastructure Manager", "مدير البنية التحتية", "MGR", "DEVOPS"),
            ("DAT-AN",  "Data Analyst", "محلل بيانات", "IC", "DATA"),
            ("DAT-DE",  "Data Engineer", "مهندس بيانات", "IC", "DATA"),
            ("DAT-SE",  "Senior Data Engineer", "مهندس بيانات أول", "IC", "DATA"),
            ("DAT-L",   "Data & Analytics Lead", "قائد البيانات والتحليلات", "SUP", "DATA"),
            ("PRD-DS",  "Product Designer", "مصمم منتج", "IC", "PROD"),
            ("PRD-PM",  "Product Manager", "مدير منتج", "SUP", "PROD"),
            ("PRD-SPM", "Senior Product Manager", "مدير منتج أول", "MGR", "PROD"),
            ("SEC-1",   "Security Engineer", "مهندس أمن معلومات", "IC", "SEC"),
            ("SEC-L",   "Security Lead", "قائد أمن المعلومات", "SUP", "SEC"),
            ("SUP-1",   "IT Support Specialist", "أخصائي دعم فني", "BC", "ITSUP"),
            ("SUP-2",   "Senior IT Support Specialist", "أخصائي دعم فني أول", "IC", "ITSUP"),
            ("SUP-S",   "IT Support Supervisor", "مشرف دعم فني", "SUP", "ITSUP"),
            ("PMO-C",   "Project Coordinator", "منسق مشاريع", "IC", "PMO"),
            ("PMO-PM",  "Project Manager", "مدير مشروع", "SUP", "PMO"),
            ("PMO-L",   "PMO Lead", "قائد مكتب إدارة المشاريع", "MGR", "PMO"),
            ("HR-1",    "HR Specialist", "أخصائي موارد بشرية", "IC", "HR"),
            ("HR-M",    "HR Manager", "مدير موارد بشرية", "MGR", "HR"),
            ("FIN-1",   "Accountant", "محاسب", "IC", "FIN"),
            ("FIN-2",   "Senior Accountant", "محاسب أول", "IC", "FIN"),
            ("FIN-M",   "Finance Manager", "مدير مالي", "MGR", "FIN"),
            ("SAL-1",   "Sales Executive", "تنفيذي مبيعات", "IC", "SAL"),
            ("SAL-AM",  "Account Manager", "مدير حسابات", "SUP", "SAL"),
            ("SAL-M",   "Sales Manager", "مدير مبيعات", "MGR", "SAL"),
            ("ADM-1",   "Office Administrator", "مسؤول إداري", "BC", "ADM"),
            ("ADM-S",   "Admin Supervisor", "مشرف إداري", "SUP", "ADM"),
            ("EXE-CTO", "Chief Technology Officer", "الرئيس التنفيذي للتقنية", "EXEC", "ENG"),
            ("EXE-CEO", "Chief Executive Officer", "الرئيس التنفيذي", "EXEC", "ADM"),
        };
        var jobs = jobDefs.ToDictionary(
            d => d.Code, d => Job.Create(d.Code, d.En, d.Ar, levels[d.Level].Id, null, null, families[d.Family].Id));
        db.Jobs.AddRange(jobs.Values);

        // ---- Evaluations (representative jobs scored through the real workflow) ----
        var evalDefs = new (string Job, int[] Points, bool Approve)[]
        {
            ("SUP-1",   new[] { 4, 8, 4, 4, 4 },     true),   // 24
            ("ENG-SE1", new[] { 12, 4, 8, 8, 4 },    true),   // 36
            ("QA-1",    new[] { 12, 8, 8, 8, 4 },    true),   // 40
            ("ENG-SE2", new[] { 12, 12, 12, 8, 4 },  true),   // 48
            ("OPS-1",   new[] { 16, 12, 12, 8, 4 },  true),   // 52
            ("DAT-DE",  new[] { 16, 12, 12, 12, 4 }, true),   // 56
            ("SEC-1",   new[] { 16, 12, 16, 12, 4 }, true),   // 60
            ("ENG-SE3", new[] { 16, 16, 16, 12, 8 }, true),   // 68
            ("ENG-TL",  new[] { 16, 16, 16, 12, 12 }, true),  // 72
            ("ENG-EM",  new[] { 16, 16, 20, 16, 20 }, true),  // 88
            ("PMO-PM",  new[] { 12, 16, 12, 16, 16 }, false), // 72, left Submitted (pending approval)
            ("FIN-M",   new[] { 12, 16, 12, 20, 16 }, false), // 76, left Submitted
        };

        var evalCount = 0;
        foreach (var e in evalDefs)
        {
            var job = jobs[e.Job];
            var eval = Evaluation.CreateDraft(job.Id, version.Id, evaluatorEmployeeId: null);
            var total = 0;
            for (var i = 0; i < factorBuilds.Count; i++)
            {
                var (factor, question, options) = factorBuilds[i];
                var option = options.First(o => o.Points == e.Points[i]);
                db.EvaluationAnswers.Add(EvaluationAnswer.Create(eval.Id, question.Id, option.Id, option.Points));
                db.EvaluationFactorScores.Add(EvaluationFactorScore.Create(eval.Id, factor.Id, option.Points));
                total += option.Points;
            }
            var gradeId = ResolveGrade(total);
            eval.Submit(total, gradeId);
            if (e.Approve)
            {
                eval.Approve();
                if (gradeId is { } g) job.AssignGrade(g);
            }
            db.Evaluations.Add(eval);
            evalCount++;
        }

        // ---- Salary bands (Farouk's defaults: ±25% spread, 25% midpoint progression), EGP ----
        var effective = new DateOnly(2026, 1, 1);
        var bandCount = 0;
        foreach (var grade in grades.Values)
        {
            var midpoint = (decimal)(Math.Round(8000 * Math.Pow(1.25, grade.Rank - 1) / 100.0) * 100);
            db.SalaryBands.Add(SalaryBand.Create(grade.Id, "EGP", midpoint, spreadPct: 67m, overlapPct: 25m, effective));
            bandCount++;
        }

        await db.SaveChangesAsync(ct);

        return new DemoSeedSummary(
            Levels: levels.Count, JobFamilies: families.Count, Grades: grades.Count,
            Jobs: jobs.Count, Evaluations: evalCount, SalaryBands: bandCount);
    }
}
