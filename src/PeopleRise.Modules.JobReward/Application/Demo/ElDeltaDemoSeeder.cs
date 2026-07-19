using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;

namespace PeopleRise.Modules.JobReward.Application.Demo;

/// <summary>
/// Seeds a fresh tenant DB with a realistic, bilingual (EN/AR) dataset for the pilot customer:
/// El-Delta, an Egyptian IT company (150-250 staff). Produces the full design-time chain - levels,
/// IT job families, a grade grid, a published weighted point-factor methodology, ~39 jobs, scored
/// evaluations, and EGP salary bands. All entities are created through their domain factories;
/// evaluations go through the real Draft -> Submit -> Approve transitions.
///
/// The methodology (6 factors, 26 questions, 130 answer options, all with bilingual help text) is
/// the real content authored by the consultant in the El-Delta tenant's Draft v2, captured here so
/// every fresh tenant seeds with the calibrated methodology instead of the earlier placeholder.
/// </summary>
internal static class ElDeltaDemoSeeder
{
    private sealed record OptionDef(string LabelEn, string? LabelAr, string? HelpTextEn, string? HelpTextAr);
    private sealed record QuestionDef(string TextEn, string? TextAr, string? HelpTextEn, string? HelpTextAr, decimal Weight, bool IsRequired, OptionDef[] Options);
    private sealed record FactorDef(string Code, string NameEn, string? NameAr, string? HelpTextEn, string? HelpTextAr, decimal Weight, QuestionDef[] Questions);

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

        // ---- Methodology: the calibrated El-Delta point-factor questionnaire, 200-1000 point budget.
        // 6 factors (weights 30/20/25/15/5/5 = 100%), 26 questions (weights sum to 100% per factor),
        // 130 answer options on the unified 1-5 rating scale - all with bilingual help text. ----
        var methodology = Methodology.Create("ELD-PF", "El-Delta Point-Factor", "نظام النقاط والعوامل — الدلتا");
        db.Methodologies.Add(methodology);
        var version = MethodologyVersion.CreateDraft(methodology.Id, 1, "Initial El-Delta methodology");
        db.MethodologyVersions.Add(version);

        var factorDefs = new FactorDef[]
        {
            new("KNW", "Know-How (Education & Technical Experise)", "المعرفة (التعليم والخبرة التقنية)", null, null, 30m,
                new QuestionDef[]
                {
                    new("Required education", "المستوى التعليمي المطلوب", "Measure minimum formal education required", "قياس أقل مستوى تعليمي مطلوب", 20m, true,
                        new OptionDef[]
                        {
                            new("High School or Below", "ثانوي أو أقل", "Requires basic secondary education.", "يتطلب تعليمًا ثانويًا أساسيًا."),
                            new("Diploma", "دبلوم", "Requires vocational/technical diploma.", "يتطلب دبلومًا مهنيًا/تقنيًا."),
                            new("Bachelor's Degree", "بكالوريوس", "Requires bachelor's degree.", "يتطلب بكالوريوس"),
                            new("Bachelor + Professional Certificatations", "بكالوريوس + شهادات مهنية", "Requires degree plus certification/postgrad.", "يتطلب بكالوريوس بالإضافة إلى شهادة أو دراسات عليا"),
                            new("Postgraduate Degree", "ماجستير أو دراسات عليا", "Requires master's/doctorate or equivalent.", "تتطلب ماجيستير أو دكتوراه أو ما يعادله."),
                        }),
                    new("Years of experience", "سنوات الخبرة", null, null, 25m, true,
                        new OptionDef[]
                        {
                            new("Less than a year", "أقل من سنة", "Entry level.", "مبتدأ"),
                            new("1-3 years", "1-3 سنوات", "Basic independent.", "مستقل أساسي."),
                            new("3-6 years", "3-6 سنوات", "Experienced.", "ذو خبرة."),
                            new("6-10 years", "6-10 سنوات", "Senior.", "محترف."),
                            new("10+ years", "10+ سنوات", "Highly experienced.", "ذو خبرة واسعة."),
                        }),
                    new("Technical and special knowledge", "المعرفة الفنية والتخصصية", "It measures the required knowledge for the role, not the employee's personal proficiency.", "تهدف لقياس المعرفة المطلوبة للوظيفة ، وليس كفاءة موظف بعينه", 25m, true,
                        new OptionDef[]
                        {
                            new("Basic", "معرفة أساسية", "Requires basic general knowledge. Tasks are routine and performed using established procedures with close guidance.", "تتطلب الوظيفة معرفة عامة ومبادئ أساسية، مع تنفيذ مهام روتينية وفق إجراءات وتعليمات واضحة."),
                            new("Working Knowledge", null, "Requires practical knowledge of standard methods, tools, and processes to perform day-to-day responsibilities independently.", "تتطلب الوظيفة معرفة جيدة بالإجراءات والأدوات وأساليب العمل القياسية، مع القدرة على أداء المهام باستقلالية."),
                            new("Advanced Knowledge", "معرفة متقدمة", "Requires in-depth technical or specialized knowledge to analyze issues, solve non-routine problems, and provide technical guidance.", "تتطلب الوظيفة معرفة فنية أو تخصصية متعمقة لتحليل المشكلات، ومعالجة الحالات غير الروتينية، وتقديم الدعم الفني للآخرين."),
                            new("Expert Knowledge", "خبرة تخصصية عالية", "Requires recognized expertise in a specialized field to design solutions, develop standards, and advise others on complex matters.", "تتطلب الوظيفة خبرة عميقة في مجال تخصصي، لتصميم الحلول، ووضع المعايير، وتقديم الاستشارات في الموضوعات المعقدة."),
                            new("Strategic Authority", "مرجعية استراتيجية", "Requires organization-wide or industry-leading expertise to define technical direction, establish policies, and influence long-term strategy.", "تتطلب الوظيفة مستوى رفيعًا من الخبرة يؤهل شاغلها لوضع التوجهات والسياسات الفنية، واعتماد المعايير، والتأثير في القرارات الاستراتيجية على مستوى الجهة."),
                        }),
                    new("Knowledge of Policies and Procedures", "المعرفة بالسياسات والإجراءات", "Measure required knowledge of organizational policies.", "قياس المعرفة المطلوبة بالسياسيات والإجراءات المرتبطة بالمؤسسة", 15m, true,
                        new OptionDef[]
                        {
                            new("Basic", "أساسية", "Requires knowledge of a limited number of clearly defined policies and procedures to perform routine job duties.", "تتطلب الوظيفة الإلمام بعدد محدود من السياسات والإجراءات الواضحة والمحددة لتأدية المهام اليومية."),
                            new("Working Knowledge", "عملية", "Requires a good understanding of departmental policies and procedures to perform work independently and consistently.", "تتطلب الوظيفة معرفة جيدة بالسياسات والإجراءات الخاصة بالإدارة أو القسم، وتطبيقها بشكل مستقل."),
                            new("Advanced Knowledge", "متقدمة", "Requires comprehensive knowledge of policies and procedures across multiple functions, including the ability to interpret and apply them in non-routine situations.", "تتطلب الوظيفة فهماً شاملاً للسياسات والإجراءات عبر أكثر من إدارة، مع القدرة على تفسيرها وتطبيقها في الحالات غير الاعتيادية."),
                            new("Expert Knowledge", "متخصصة", "Requires extensive knowledge of complex policies and procedures, including contributing to their development, review, or improvement, and providing guidance to others.", "تتطلب الوظيفة الإلمام بسياسات وإجراءات معقدة، مع المساهمة في تطويرها أو مراجعتها، وتقديم الإرشاد للآخرين بشأنها."),
                            new("Strategic Authority", "استراتيجية", "Requires responsibility for establishing, approving, or governing organization-wide policies and procedures to support strategic and regulatory objectives.", "تتطلب الوظيفة وضع أو اعتماد السياسات والإجراءات المؤسسية، وضمان مواءمتها مع الأنظمة والتوجهات الاستراتيجية للجهة."),
                        }),
                    new("Breadth and Diversity of Knowledge", "اتساع وتنوع المعرفة", null, null, 15m, true,
                        new OptionDef[]
                        {
                            new("Limited", "محدودة", "Requires knowledge of a single subject area or a narrow range of related tasks.", "يتطلب معرفة بمجال موضوعي واحد أو نطاق ضيق من المهام ذات الصلة."),
                            new("Moderate", "متوسطة", "Requires knowledge across several related topics or processes within the same functional area.", "تتطلب الوظيفة معرفة في العديد من المواضيع أو العمليات ذات الصلة ضمن نفس المجال الوظيفي."),
                            new("Broad", "واسعة", "Requires knowledge spanning multiple disciplines or functional areas to perform the role effectively.", "تتطلب الوظيفة معرفة تشمل تخصصات أو مجالات وظيفية متعددة لأداء الدور بفعالية."),
                            new("Extensive", "شامل", "Requires integration of knowledge from diverse disciplines to solve complex business or technical challenges.", "يتطلب الأمر دمج المعرفة من تخصصات متنوعة لحل التحديات العمل (business) أو التقنية المعقدة."),
                            new("Enterprise-Wide", "على مستوى المؤسسة", "Requires comprehensive knowledge across multiple business functions, enabling organization-wide decision-making and strategic integration.", "تتطلب الوظيفة معرفة شاملة عبر وظائف الأعمال المتعددة، مما يتيح اتخاذ القرارات على مستوى المؤسسة والتكامل الاستراتيجي."),
                        }),
                }),
            new("PS", "Provlem Solving", "حل المشكلات", "Measures the complexity of problems encountered by the job and the level of analytical thinking, judgment, and creativity required to identify effective solutions and make decisions within established guidelines or in the absence of predefined procedures.", "يقيس مدى تعقيد المشكلات التي تتعامل معها الوظيفة، ومستوى التحليل والتفكير والابتكار المطلوب لتحديد الحلول المناسبة واتخاذ القرارات في ظل توفر أو محدودية التوجيه والإجراءات", 20m,
                new QuestionDef[]
                {
                    new("What is the nature of the problems encountered in this job?", "ما طبيعة المشكلات التي تتعامل معها الوظيفة؟", null, null, 25m, true,
                        new OptionDef[]
                        {
                            new("Routine Problems", "مشكلات روتينية", "Problems are recurring and predictable, with clearly defined procedures or standard solutions that can be applied with minimal analysis or judgment.", "المشكلات متكررة ومتوقعة، ولها إجراءات أو حلول محددة يمكن تطبيقها مباشرة دون الحاجة إلى تحليل كبير."),
                            new("Operational Problems", "مشكلات تشغيلية", "Problems require selecting the most appropriate solution from established alternatives or procedures, involving limited analysis and professional judgment.", "تتطلب اختيار الحل المناسب من بين بدائل أو إجراءات معروفة، مع قدر محدود من التحليل والحكم المهني"),
                            new("Analytical Problems", "مشكلات تحليلية", "Problems require identifying root causes, evaluating multiple alternatives, and making decisions in non-routine situations using professional knowledge and experience.", "تتطلب تحليل الأسباب، وتقييم عدة بدائل، واتخاذ قرارات في مواقف غير روتينية بالاعتماد على الخبرة والمعرفة."),
                            new("Complex Problems", "مشكلات معقدة", "Problems require integrating information from multiple sources and developing innovative solutions for situations where precedents or established procedures may not exist.", "تتطلب دمج معلومات من مصادر متعددة، والابتكار في تطوير حلول جديدة للمواقف التي لا تتوفر لها سوابق أو إجراءات واضحة."),
                            new("Strategic Problems", "مشكلات استراتيجية", "Problems are highly ambiguous and have broad organizational impact, requiring strategic thinking and the development of new approaches, policies, or solutions that influence long-term outcomes.", "تتسم بالغموض والتأثير الواسع على المؤسسة، وتتطلب التفكير الاستراتيجي، ووضع حلول أو مناهج جديدة تؤثر على التوجهات أو السياسات أو النتائج طويلة المدى."),
                        }),
                    new("Level of Analysis Required", "درجة التحليل المطلوبة", "Measures the level of analysis, evaluation, and logical reasoning required to understand problems, identify root causes, assess alternatives, and determine appropriate decisions or solutions.", "يقيس مستوى التحليل والتقييم والتفكير المنطقي المطلوب لفهم المشكلات، وتحديد أسبابها، وتقييم البدائل، والوصول إلى القرارات أو الحلول المناسبة.", 25m, true,
                        new OptionDef[]
                        {
                            new("Limited Analysis", "تحليل محدود", "Requires following clearly defined instructions or procedures with minimal analysis or verification.", "يتطلب تطبيق تعليمات أو إجراءات واضحة، مع الحاجة إلى قدر بسيط من التحليل أو التحقق."),
                            new("Basic Analysis", "تحليل أساسي", "Requires reviewing information, comparing known alternatives, and selecting the most appropriate solution based on established criteria", "يتطلب مراجعة المعلومات، والمقارنة بين بدائل معروفة، واختيار الحل الأنسب وفق معايير محددة."),
                            new("Advanced Analysis", "تحليل متقدم", "Requires analyzing data, identifying root causes, and evaluating multiple options to make decisions in non-routine situations.", "يتطلب تحليل البيانات، وتحديد الأسباب الجذرية، وتقييم عدة خيارات لاتخاذ قرارات في مواقف غير روتينية."),
                            new("Comprehensive Analysis", "تحليل شامل", "Requires integrating information from multiple sources, assessing the impact of decisions, and developing innovative solutions to complex problems.", "يتطلب دمج وتحليل معلومات من مصادر متعددة، وتقييم تأثير القرارات، وتطوير حلول مبتكرة للمشكلات المعقدة."),
                            new("Strategic Analysis", "تحليل استراتيجي", "Requires analyzing complex and ambiguous issues with organization-wide impact, anticipating outcomes, and formulating solutions or strategies that support long-term organizational objectives", "يتطلب تحليل قضايا معقدة وغامضة ذات تأثير مؤسسي، وتوقع النتائج، وصياغة حلول أو توجهات تدعم الأهداف الاستراتيجية."),
                        }),
                    new("Level of Innovation in Solutions", "مستوى الابتكار في الحلول", "Measures the extent to which the job requires developing new solutions, improving existing methods, or creating innovative approaches to address business challenges and achieve organizational objectives", "يقيس مدى الحاجة إلى تطوير أو ابتكار حلول جديدة، أو تحسين الأساليب القائمة، أو تصميم مناهج غير تقليدية لمعالجة المشكلات وتحقيق أهداف العمل.", 25m, true,
                        new OptionDef[]
                        {
                            new("No Innovation Required", "لا يتطلب ابتكارًا", "The job relies on applying established procedures or standard solutions and does not require developing new approaches", "تعتمد الوظيفة على تطبيق إجراءات أو حلول معتمدة مسبقًا، ولا تتطلب تطوير أساليب أو أفكار جديدة."),
                            new("Limited Innovation", "ابتكار محدود", "The job requires minor improvements or adapting existing solutions to meet specific work requirements.", "تتطلب الوظيفة إجراء تحسينات بسيطة أو تكييف الحلول القائمة بما يتناسب مع متطلبات العمل."),
                            new("Moderate Innovation", "ابتكار متوسط", "The job requires developing new solutions for non-routine situations or making significant improvements to processes or services", "تتطلب الوظيفة تطوير حلول جديدة للمواقف غير الروتينية أو تحسين العمليات والخدمات بشكل ملحوظ."),
                            new("High Innovation", "ابتكار متقدم", "The job requires creating new methods, models, or approaches to address complex challenges and improve organizational performance.", "تتطلب الوظيفة ابتكار أساليب أو نماذج عمل جديدة لمعالجة التحديات المعقدة وتحسين الأداء المؤسسي."),
                            new("Strategic Innovation", "ابتكار استراتيجي", "The job requires creating breakthrough solutions, concepts, or business models that significantly influence the organization or industry and support long-term strategic objectives.", "تتطلب الوظيفة ابتكار حلول أو مفاهيم أو نماذج عمل جديدة تُحدث تأثيرًا جوهريًا على المؤسسة أو القطاع، وتدعم تحقيق الأهداف الاستراتيجية طويلة المدى."),
                        }),
                    new("Independence of Thinking", "استقلالية التفكير", "Measures the degree to which the job requires independent thinking, judgment, and decision-making without relying on detailed instructions or close supervision.", "يقيس مدى استقلالية الوظيفة في تحليل المواقف، وتقييم البدائل، واتخاذ القرارات، دون الاعتماد على تعليمات تفصيلية أو إشراف مباشر.", 25m, true,
                        new OptionDef[]
                        {
                            new("Limited Independence", "استقلالية محدودة", "Work is performed according to clearly defined instructions and procedures, with close supervision for most tasks and decisions", "يتم تنفيذ العمل وفق تعليمات وإجراءات محددة، مع الحاجة إلى إشراف مباشر في معظم المهام والقرارات."),
                            new("Basic Independence", "استقلالية أساسية", "Work is performed independently within established procedures and policies, while unusual situations are referred to a supervisor.", "يتم تنفيذ المهام باستقلالية ضمن إجراءات وسياسات واضحة، مع الرجوع إلى المسؤول في الحالات غير الاعتيادية."),
                            new("Moderate Independence", "استقلالية متوسطة", "The job requires professional judgment and independent decision-making in non-routine situations within established policies.", "تتطلب الوظيفة ممارسة الحكم المهني واتخاذ القرارات اليومية في المواقف غير الروتينية ضمن إطار السياسات العامة."),
                            new("High Independence", "استقلالية عالية", "The job requires a high degree of independent judgment in analyzing issues and making decisions, with limited guidance or supervision.", "تتطلب الوظيفة قدراً كبيراً من الاستقلالية في تحليل القضايا واتخاذ القرارات، مع محدودية التوجيه والإشراف المباشر."),
                            new("Strategic Independence", "استقلالية استراتيجية", "The job requires full independence in addressing complex issues and making high-impact decisions, including establishing principles or directions that guide others.", "تتطلب الوظيفة استقلالية كاملة في التفكير واتخاذ القرارات بشأن القضايا المعقدة، مع وضع التوجهات أو المبادئ التي يسترشد بها الآخرون."),
                        }),
                }),
            new("RI", "Responsibility and Impact", "المسؤولية والتأثير", "Measures the level of accountability for results and the extent to which the job's decisions and actions impact people, operations, resources, and the achievement of departmental or organizational objectives", "يقيس مدى مسؤولية الوظيفة عن النتائج، وحجم تأثير قراراتها وإجراءاتها على الأفراد، والعمليات، والموارد، وتحقيق أهداف الإدارة أو المؤسسة.", 25m,
                new QuestionDef[]
                {
                    new("Accountability for Results", "المسؤولية عن النتائج", "Measures the degree of accountability the job has for achieving expected results and objectives, including responsibility for the quality of outputs, effective use of resources, and attainment of performance targets.", "يقيس مدى مساءلة الوظيفة عن تحقيق النتائج والأهداف، ومستوى المسؤولية عن جودة المخرجات، وكفاءة استخدام الموارد، وتحقيق مؤشرات الأداء المستهدفة.", 20m, true,
                        new OptionDef[]
                        {
                            new("Limited Accountability", "مسؤولية محدودة", "Accountability is limited to completing assigned tasks in accordance with established instructions. Results are routinely reviewed, and any issues can be easily identified and corrected by a supervisor", "تقتصر المسؤولية على إنجاز المهام الموكلة وفق التعليمات، ويكون تأثير النتائج محدودًا ويمكن مراجعته بسهولة من قبل المشرف."),
                            new("Operational Accountability", "مسؤولية تشغيلية", "The job is accountable for the quality and accuracy of day-to-day work, compliance with procedures, and the achievement of operational results within a defined scope.", "تتحمل الوظيفة مسؤولية جودة ودقة الأعمال اليومية، والالتزام بالإجراءات، وتحقيق النتائج التشغيلية ضمن نطاق العمل المحدد."),
                            new("Departmental Accountability", "مسؤولية على مستوى الإدارة", "The job is accountable for achieving the objectives of a department or functional area, ensuring the effective use of resources, and delivering agreed performance targets.", "تتحمل الوظيفة مسؤولية تحقيق أهداف قسم أو وظيفة محددة، وضمان الاستخدام الفعال للموارد، وتحقيق مؤشرات الأداء المتفق عليها."),
                            new("Organizational Accountability", "مسؤولية مؤسسية", "The job is accountable for results that span multiple departments or functions, ensuring organizational objectives are achieved while improving operational performance and efficiency.", "تتحمل الوظيفة مسؤولية نتائج تمتد عبر عدة إدارات أو وظائف، مع ضمان تحقيق الأهداف المؤسسية وتحسين الأداء والكفاءة التشغيلية."),
                            new("Strategic Accountability", "مسؤولية استراتيجية", "The job holds ultimate accountability for achieving results with strategic organizational impact, including long-term objectives, optimal use of resources, and the creation of sustainable organizational value.", "تتحمل الوظيفة المسؤولية النهائية عن تحقيق نتائج ذات أثر استراتيجي على المؤسسة، بما في ذلك تحقيق الأهداف طويلة المدى، وضمان الاستخدام الأمثل للموارد، وتعزيز القيمة المؤسسية."),
                        }),
                    new("Financial Impact", "التأثير المالي", "Measures the extent to which the job's decisions and actions influence the organization's financial performance, including cost management, revenue generation, efficient use of resources, and protection of organizational assets.", "يقيس مدى تأثير قرارات وإجراءات الوظيفة على الأداء المالي للمؤسسة، بما في ذلك إدارة التكاليف، وتعظيم الإيرادات، وتحسين كفاءة استخدام الموارد، وحماية الأصول.", 20m, true,
                        new OptionDef[]
                        {
                            new("Limited Financial Impact", "تأثير مالي محدود", "The job has little or no direct financial impact. Its financial responsibility is limited to the proper use of assigned resources and equipment.", "لا يترتب على قرارات الوظيفة تأثير مالي مباشر، ويقتصر أثرها على الاستخدام السليم للموارد والأدوات المخصصة للعمل."),
                            new("Operational Financial Impact", "تأثير مالي تشغيلي", "The job influences operating costs or resource utilization within a team or department. Errors may result in limited financial losses or additional operating costs.", "تؤثر قرارات الوظيفة على التكاليف التشغيلية أو كفاءة استخدام الموارد ضمن نطاق الفريق أو القسم، وقد تؤدي الأخطاء إلى خسائر مالية محدودة."),
                            new("Significant Financial Impact", "تأثير مالي ملحوظ", "The job directly influences budgets, contracts, or projects and contributes to cost control, cost savings, or revenue support", "تؤثر الوظيفة بشكل مباشر على إدارة ميزانيات أو عقود أو مشروعات، وتسهم في التحكم في التكاليف أو تحقيق وفورات أو دعم الإيرادات."),
                            new("Major Financial Impact", "تأثير مالي كبير", "The job significantly influences the financial performance of multiple departments or business units and is accountable for achieving meaningful financial outcomes and efficient resource investment.", "تؤثر قرارات الوظيفة على الأداء المالي لعدة إدارات أو وحدات تنظيمية، وتتحمل مسؤولية تحقيق نتائج مالية مؤثرة وتحسين كفاءة الاستثمار في الموارد."),
                            new("Strategic Financial Impact", "تأثير مالي استراتيجي", "The job has a direct impact on the organization's overall financial performance through strategic financial decisions, management of significant investments, or long-term value creation.", "تؤثر قرارات الوظيفة بشكل مباشر على الأداء المالي للمؤسسة ككل، من خلال تحديد التوجهات المالية، أو إدارة استثمارات كبيرة، أو تحقيق أثر مالي طويل المدى."),
                        }),
                    new("Impact on Customers or Stakeholders", "التأثير على العملاء أو أصحاب المصلحة", "Measures the extent to which the job's decisions and actions influence customer satisfaction, stakeholder relationships, service quality, and value delivery to internal and external stakeholders.", "يقيس مدى تأثير قرارات وإجراءات الوظيفة على رضا العملاء أو أصحاب المصلحة، وجودة الخدمات المقدمة، وبناء العلاقات، وتحقيق القيمة للأطراف ذات العلاقة داخل المؤسسة وخارجها.", 20m, true,
                        new OptionDef[]
                        {
                            new("Limited Impact", "تأثير محدود", "The job provides limited services or support to a small number of customers or stakeholders. Any negative impact is minor and can be easily corrected.", "يقتصر تأثير الوظيفة على تقديم خدمات أو دعم محدود لعدد قليل من العملاء أو أصحاب المصلحة، ويمكن معالجة أي أثر سلبي بسهولة."),
                            new("Operational Impact", "تأثير تشغيلي", "The job influences service quality or responsiveness for customers or stakeholders within a team or department.", "تؤثر الوظيفة على جودة الخدمة أو سرعة الاستجابة للعملاء أو أصحاب المصلحة ضمن نطاق فريق أو إدارة محددة."),
                            new("Significant Impact", "تأثير مؤثر", "The job has a direct impact on customer or stakeholder satisfaction, service quality, and the continuity of key relationships.", "تؤثر الوظيفة بشكل مباشر على رضا العملاء أو أصحاب المصلحة، وجودة الخدمات المقدمة، واستمرارية العلاقات معهم."),
                            new("Broad Impact", "تأثير واسع", "The job influences the customer or stakeholder experience across multiple departments or services, contributing to organizational reputation and trust.", "تؤثر قرارات الوظيفة على تجربة العملاء أو أصحاب المصلحة عبر عدة إدارات أو خدمات، وتسهم في تحسين السمعة المؤسسية وتعزيز الثقة."),
                            new("Strategic Impact", "تأثير استراتيجي", "The job shapes strategic relationships with key customers or stakeholders, supports long-term partnerships, delivers sustainable value, and strengthens the organization's reputation.", "تؤثر الوظيفة على العلاقات الاستراتيجية مع العملاء أو أصحاب المصلحة الرئيسيين، وتسهم في بناء الشراكات، وتحقيق القيمة المستدامة، وتعزيز الصورة المؤسسية على المدى الطويل."),
                        }),
                    new("Decision-Making Authority", "سلطة اتخاذ القرار", "Measures the level of authority delegated to the job to make decisions, approve actions, and determine solutions, as well as the extent to which decisions require higher-level approval", "يقيس مستوى الصلاحيات الممنوحة للوظيفة لاتخاذ القرارات، ومدى استقلاليتها في اعتماد الإجراءات أو الموافقات أو الحلول، وحدود الرجوع إلى المستويات الإدارية الأعلى.", 20m, true,
                        new OptionDef[]
                        {
                            new("Limited Authority", "صلاحية محدودة", "The job follows established instructions and procedures with very limited authority to make decisions. Most decisions require supervisor approval.", "تنفذ الوظيفة المهام وفق تعليمات وإجراءات محددة، ولا تمتلك صلاحية اتخاذ قرارات إلا في نطاق محدود للغاية، مع الرجوع إلى المشرف في معظم الحالات."),
                            new("Operational Authority", "صلاحية تشغيلية", "The job has authority to make routine operational decisions within established policies and procedures. Non-routine decisions are escalated to higher management.", "تمتلك الوظيفة صلاحية اتخاذ القرارات اليومية المتعلقة بتنفيذ العمل ضمن السياسات والإجراءات المعتمدة، مع تصعيد القرارات غير الاعتيادية."),
                            new("Managerial Authority", "صلاحية إدارية", "The job has authority to make decisions affecting operations, resources, or teams within its area of responsibility. Major decisions require higher-level approval.", "تمتلك الوظيفة صلاحية اتخاذ قرارات تؤثر على سير العمل أو الموارد أو الفريق ضمن نطاق المسؤولية، مع الرجوع للإدارة العليا في القرارات الجوهرية."),
                            new("Broad Authority", "صلاحية واسعة", "The job has broad authority to make significant decisions affecting multiple departments or functions, exercising substantial independent judgment within delegated authority.", "تمتلك الوظيفة صلاحية اتخاذ قرارات مهمة ذات تأثير على عدة إدارات أو وظائف، مع استقلالية كبيرة في ممارسة الحكم المهني ضمن الصلاحيات الممنوحة."),
                            new("Strategic Authority", "صلاحية استراتيجية", "The job has final authority to make or approve strategic decisions and establish organizational directions or policies with long-term impact.", "تمتلك الوظيفة السلطة النهائية لاتخاذ القرارات الاستراتيجية أو اعتمادها، وتحديد التوجهات والسياسات التي تؤثر على المؤسسة على المدى الطويل."),
                        }),
                    new("Impact of Error", "أثر الخطأ", "Measures the potential consequences of errors made in the job before they are detected or corrected, including their impact on operations, results, resources, customers, and the organization's reputation", "يقيس حجم وتأثير الأخطاء المحتملة الناتجة عن أداء الوظيفة قبل اكتشافها أو تصحيحها، ومدى انعكاسها على العمليات، والنتائج، والموارد، والعملاء، والسمعة المؤسسية.", 20m, true,
                        new OptionDef[]
                        {
                            new("Minor Impact", "أثر محدود", "Errors have minimal consequences and can be easily detected and corrected, with little or no impact on operations, cost, or service quality.", "يترتب على الخطأ آثار محدودة يمكن اكتشافها وتصحيحها بسهولة، مع تأثير بسيط على سير العمل ودون خسائر أو آثار جوهرية."),
                            new("Operational Impact", "أثر تشغيلي", "Errors may result in rework, minor delays, or additional operating costs, with impacts generally contained within the team or department.", "قد تؤدي الأخطاء إلى إعادة تنفيذ العمل، أو تأخير محدود، أو زيادة في التكاليف التشغيلية، ويمكن احتواؤها داخل الفريق أو الإدارة."),
                            new("Significant Impact", "أثر ملحوظ", "Errors have a noticeable impact on service quality, operational efficiency, or resource utilization and may affect multiple departments or customers.", "تؤثر الأخطاء على جودة الخدمات أو كفاءة العمليات أو استخدام الموارد، وقد تمتد آثارها إلى أكثر من إدارة أو إلى العملاء."),
                            new("Major Impact", "أثر كبير", "Errors may cause substantial financial or operational losses, business disruption, or reputational damage, requiring significant effort and resources to resolve.", "قد ينتج عن الأخطاء خسائر مالية أو تشغيلية كبيرة، أو تعطيل الأعمال، أو الإضرار بسمعة المؤسسة، وتتطلب جهودًا وموارد كبيرة لمعالجتها."),
                            new("Critical Impact", "أثر جسيم", "Errors may result in severe strategic, legal, regulatory, or financial consequences, with long-term effects on business continuity, organizational reputation, or stakeholder confidence.", "قد تؤدي الأخطاء إلى عواقب استراتيجية أو قانونية أو تنظيمية جسيمة، أو خسائر كبيرة، أو تأثير طويل المدى على استمرارية الأعمال أو سمعة المؤسسة."),
                        }),
                }),
            new("LED", "Leadership and Management", "القيادة والإدارة", "Measures the level of leadership and management responsibility required by the job, including leading, developing, and directing people, managing resources, and achieving operational and strategic objectives.", "يقيس مستوى المسؤولية القيادية والإدارية التي تتطلبها الوظيفة، بما في ذلك قيادة الأفراد، وتوجيههم، وتطويرهم، وإدارة الموارد، وتحقيق الأهداف التشغيلية والاستراتيجية.", 15m,
                new QuestionDef[]
                {
                    new("Team Size or Supervisory Scope", "حجم الفريق أو نطاق الإشراف", "Measures the supervisory responsibility of the job based on the size of the team, number of employees, or organizational units under its supervision.", "يقيس حجم المسؤولية الإشرافية للوظيفة، من حيث عدد الأفراد أو الفرق أو الوحدات التنظيمية التي تقع ضمن نطاق إشرافها وإدارتها.", 30m, true,
                        new OptionDef[]
                        {
                            new("No Supervisory Responsibility", "لا توجد مسؤولية إشرافية", "The job has no direct responsibility for supervising employees or managing teams.", "لا تتضمن الوظيفة أي مسؤولية مباشرة عن الإشراف على الموظفين أو إدارة فرق العمل."),
                            new("Limited Supervision", "إشراف محدود", "The job supervises a small number of employees or coordinates the work of a small team within a single organizational unit.", "تشرف الوظيفة على عدد محدود من الموظفين أو تنسق أعمال فريق صغير ضمن وحدة تنظيمية واحدة."),
                            new("Team Supervision", "إشراف على فريق", "The job supervises one or more teams and is responsible for work allocation, performance monitoring, and achieving team objectives.", "تشرف الوظيفة على فريق عمل أو أكثر، مع تحمل مسؤولية توزيع المهام، ومتابعة الأداء، وتحقيق أهداف الفريق."),
                            new("Multi-Team / Department Supervision", "إشراف على عدة فرق أو إدارات", "The job oversees multiple teams or departments, coordinating activities to achieve shared objectives and ensure effective use of resources.", "تشرف الوظيفة على عدة فرق أو إدارات، وتنسق العمل بينها لضمان تحقيق الأهداف المشتركة والاستخدام الفعال للموارد."),
                            new("Enterprise-Wide Supervisory Scope", "إشراف مؤسسي واسع", "The job has supervisory responsibility across major organizational divisions or business units, providing leadership to senior managers and ensuring the achievement of enterprise-wide objectives.", "تمتد مسؤولية الإشراف لتشمل عدة قطاعات أو وحدات تنظيمية رئيسية، مع مسؤولية شاملة عن توجيه القيادات وتحقيق الأهداف المؤسسية."),
                        }),
                    new("Diversity of Functions Supervised", "تنوع الوظائف التي يتم الإشراف عليها", "Measures the diversity of functions, disciplines, or organizational units supervised by the job, and the level of coordination and management required across teams with different responsibilities and expertise.", "يقيس مدى تنوع الوظائف أو التخصصات أو الوحدات التنظيمية التي تقع ضمن نطاق إشراف الوظيفة، وما يتطلبه ذلك من تنسيق وإدارة لفرق ذات مهام وخبرات مختلفة.", 30m, true,
                        new OptionDef[]
                        {
                            new("No Supervisory Responsibility", "لا توجد مسؤولية إشرافية", "The job has no responsibility for supervising employees or teams.", "لا تتضمن الوظيفة الإشراف على موظفين أو فرق عمل."),
                            new("Supervision of a Single Function", "الإشراف على وظيفة واحدة", "The job supervises employees performing the same function or discipline, with limited diversity in responsibilities.", "تشرف الوظيفة على فريق أو مجموعة من الموظفين الذين يؤدون نفس التخصص أو الوظيفة، مع محدودية التنوع في طبيعة الأعمال."),
                            new("Supervision of Related Functions", "الإشراف على وظائف مترابطة", "The job supervises teams performing related functions or disciplines, requiring ongoing coordination to achieve shared objectives.", "تشرف الوظيفة على فريق أو فرق تعمل في تخصصات أو وظائف مترابطة تتطلب تنسيقًا مستمرًا لتحقيق الأهداف المشتركة."),
                            new("Supervision of Multiple Functions", "الإشراف على وظائف متعددة", "The job supervises multiple functions or disciplines, requiring coordination across teams with different expertise, priorities, and responsibilities.", "تشرف الوظيفة على عدة وظائف أو تخصصات مختلفة، وتتطلب إدارة أولويات متنوعة والتنسيق بين فرق ذات خبرات ومسؤوليات متباينة."),
                            new("Supervision of Diverse Enterprise Functions", "الإشراف على وظائف مؤسسية متنوعة", "The job supervises a broad range of organizational functions or business units, ensuring integration across diverse disciplines to achieve enterprise-wide strategic objectives.", "تشرف الوظيفة على مجموعة واسعة من الوظائف أو الوحدات التنظيمية على مستوى المؤسسة، بما يتطلب تحقيق التكامل بين تخصصات متعددة ودعم تحقيق الأهداف الاستراتيجية."),
                        }),
                    new("Responsibility for Employee Development and Performance Management", "مسؤولية تطوير وتقييم العاملين", "Measures the extent to which the job is responsible for developing employees, providing coaching and guidance, managing performance, identifying development needs, and supporting professional growth to achieve organizational objectives.", "يقيس مدى مسؤولية الوظيفة عن تطوير العاملين، وتوجيههم، وتقييم أدائهم، وتحديد احتياجاتهم التدريبية، ودعم نموهم المهني لتحقيق أهداف العمل.", 20m, true,
                        new OptionDef[]
                        {
                            new("No Responsibility", "لا توجد مسؤولية", "The job has no responsibility for employee development or performance management.", "لا تتضمن الوظيفة أي مسؤولية عن تطوير أو تقييم أداء العاملين."),
                            new("Indirect Support", "دعم غير مباشر", "The job provides informal coaching, on-the-job training, or knowledge sharing but has no formal responsibility for performance evaluation or employee development.", "تقدم الوظيفة التوجيه أو التدريب أثناء العمل ومشاركة المعرفة، دون تحمل مسؤولية رسمية عن تقييم الأداء أو التطوير."),
                            new("Responsibility for a Team", "مسؤولية عن فريق", "The job is responsible for developing team members, providing feedback, participating in performance evaluations, and identifying training and development needs.", "تتحمل الوظيفة مسؤولية تطوير أعضاء الفريق، وتقديم التغذية الراجعة، والمشاركة في تقييم الأداء، وتحديد الاحتياجات التدريبية."),
                            new("Comprehensive Management Responsibility", "مسؤولية إدارية شاملة", "The job has full responsibility for managing the performance and development of multiple teams or functions, including goal setting, performance reviews, development planning, and capability building.", "تتحمل الوظيفة المسؤولية الكاملة عن إدارة أداء عدة فرق أو وحدات، بما يشمل تحديد الأهداف، وتقييم الأداء، ووضع خطط التطوير، ودعم بناء القدرات."),
                            new("Strategic Responsibility", "مسؤولية استراتيجية", "The job is responsible for shaping organizational capability by establishing performance management and talent development strategies, frameworks, and policies that support long-term organizational success.", "تقود الوظيفة تطوير الكفاءات المؤسسية، وتضع السياسات والأطر الخاصة بإدارة الأداء وتطوير المواهب، بما يدعم تحقيق الأهداف الاستراتيجية للمؤسسة."),
                        }),
                    new("Planning and Resource Allocation", "التخطيط وتوزيع الموارد", "Measures the extent to which the job is responsible for planning activities, setting priorities, and allocating human, financial, physical, and time resources to achieve objectives efficiently and effectively.", "يقيس مدى مسؤولية الوظيفة عن تخطيط الأنشطة، وتحديد الأولويات، وتوزيع الموارد البشرية والمالية والمادية والزمنية، بما يضمن تحقيق الأهداف بكفاءة وفعالية.", 20m, true,
                        new OptionDef[]
                        {
                            new("No Planning Responsibility", "لا توجد مسؤولية تخطيطية", "The job follows predefined plans and schedules with no responsibility for planning activities or allocating resources.", "تقتصر الوظيفة على تنفيذ المهام وفق خطط أو جداول معدة مسبقًا، دون مسؤولية عن تخطيط العمل أو تخصيص الموارد."),
                            new("Limited Planning", "تخطيط محدود", "The job requires planning day-to-day or short-term activities and organizing available resources within a defined area of responsibility.", "تتطلب الوظيفة تخطيط الأنشطة اليومية أو قصيرة المدى، وتنظيم استخدام الموارد المتاحة ضمن نطاق عمل محدد."),
                            new("Operational Planning", "تخطيط تشغيلي", "The job requires developing operational plans, setting priorities, and allocating resources across activities or projects to achieve operational objectives.", "تتطلب الوظيفة إعداد خطط تشغيلية، وتحديد الأولويات، وتوزيع الموارد بين الأنشطة أو المشاريع لضمان تحقيق الأهداف التشغيلية."),
                            new("Integrated Planning", "تخطيط متكامل", "The job requires planning and coordinating resources across multiple teams or departments, balancing priorities and resource constraints to achieve optimal results.", "تتطلب الوظيفة تخطيط وتنسيق الموارد عبر عدة فرق أو إدارات، وتحقيق التوازن بين الأولويات والموارد المتاحة لضمان أفضل النتائج."),
                            new("Strategic Resource Planning", "تخطيط استراتيجي للموارد", "The job requires establishing strategic plans and determining organization-wide resource allocation priorities to support long-term objectives and maximize resource effectiveness.", "تتطلب الوظيفة وضع الخطط الاستراتيجية وتحديد أولويات تخصيص الموارد على مستوى المؤسسة، بما يدعم تحقيق الأهداف طويلة المدى وتعظيم الاستفادة من الموارد."),
                        }),
                }),
            new("COMM", "Communication", "التواصل", "Measures the level of communication required by the job, including information exchange, coordination, influencing, negotiation, and relationship management with internal and external stakeholders.", "يقيس مستوى مهارات التواصل المطلوبة في الوظيفة، من حيث تبادل المعلومات، والتنسيق، والإقناع، والتفاوض، وبناء العلاقات مع الأطراف الداخلية والخارجية لتحقيق أهداف العمل.", 5m,
                new QuestionDef[]
                {
                    new("Nature of Communication", "طبيعة التواصل", "Measures the nature and level of communication required by the job, including information exchange, coordination, persuasion, negotiation, and organizational representation to achieve objectives and build effective relationships with stakeholders.", "يقيس طبيعة ومستوى التواصل الذي تتطلبه الوظيفة، من حيث تبادل المعلومات، والتنسيق، والإقناع، والتفاوض، وتمثيل المؤسسة، لتحقيق الأهداف وبناء العلاقات مع الأطراف ذات العلاقة.", 40m, true,
                        new OptionDef[]
                        {
                            new("Information Exchange", "تبادل معلومات", "Communication is limited to exchanging routine information, data, or instructions clearly and accurately.", "يقتصر التواصل على تبادل المعلومات الروتينية أو نقل البيانات والتعليمات بشكل واضح ودقيق."),
                            new("Work Coordination", "تنسيق الأعمال", "Communication is required to coordinate activities, monitor work progress, and ensure collaboration among individuals or teams.", "يتطلب التواصل لتنسيق الأنشطة، ومتابعة تنفيذ الأعمال، وضمان التعاون بين الأفراد أو الفرق."),
                            new("Explanation and Persuasion", "شرح وإقناع", "Communication involves explaining ideas or recommendations, responding to inquiries, and persuading others to accept proposed solutions or viewpoints.", "يتطلب التواصل لشرح الأفكار أو التوصيات، والإجابة عن الاستفسارات، وإقناع الآخرين بوجهات النظر أو الحلول المقترحة."),
                            new("Negotiation and Relationship Management", "تفاوض وإدارة العلاقات", "Communication requires negotiating, managing relationships, resolving differences, and building consensus among stakeholders.", "يتطلب التواصل لإدارة العلاقات، والتفاوض، وتسوية الاختلافات، وتحقيق توافق بين الأطراف ذات العلاقة."),
                            new("Strategic Representation and Influence", "تمثيل وتأثير استراتيجي", "Communication requires representing the organization before internal or external parties, influencing key decisions, and building strategic partnerships and relationships.", "يتطلب التواصل لتمثيل المؤسسة أمام الجهات الداخلية أو الخارجية، والتأثير في القرارات، وبناء شراكات وعلاقات استراتيجية."),
                        }),
                    new("Diversity of Communication Contacts", "تنوع الأطراف التي يتم التواصل معها", "Measures the diversity of individuals, groups, and organizations with whom the job must communicate, both internally and externally, and the ability to interact effectively across different roles, levels, and interests.", "يقيس مدى تنوع الجهات والأطراف التي تتطلب الوظيفة التواصل معها، سواء داخل المؤسسة أو خارجها، وما يترتب على ذلك من الحاجة إلى التكيف مع مستويات مختلفة من المسؤوليات والخبرات والاهتمامات.", 30m, true,
                        new OptionDef[]
                        {
                            new("Limited Contacts", "أطراف محدودة", "Communication is limited to a small number of colleagues within the same team or organizational unit.", "يقتصر التواصل على عدد محدود من الزملاء داخل نفس الفريق أو الوحدة التنظيمية."),
                            new("Multiple Internal Contacts", "أطراف داخلية متعددة", "The job requires communication with multiple teams or departments within the organization to coordinate work and accomplish tasks.", "تتطلب الوظيفة التواصل مع فرق أو إدارات مختلفة داخل المؤسسة لتنسيق الأعمال وإنجاز المهام."),
                            new("Internal and External Contacts", "أطراف داخلية وخارجية", "The job requires regular communication with internal stakeholders as well as external customers, suppliers, partners, or other organizations.", "تتطلب الوظيفة التواصل مع أطراف داخلية بالإضافة إلى عملاء أو موردين أو شركاء أو جهات خارجية بشكل منتظم."),
                            new("Diverse and Senior-Level Contacts", "أطراف متنوعة وعالية المستوى", "The job requires interaction with a wide range of parties, including senior management, key customers, government or regulatory bodies, and strategic partners.", "تتطلب الوظيفة التواصل مع جهات متعددة تشمل الإدارة العليا والعملاء الرئيسيين والجهات الحكومية أو التنظيمية أو الشركاء الاستراتيجيين."),
                            new("Enterprise-Wide and Strategic Contacts", "شبكة علاقات مؤسسية واسعة", "The job requires managing a broad network of internal and external relationships at executive and strategic levels to support organizational objectives and long-term partnerships.", "تتطلب الوظيفة إدارة شبكة واسعة من العلاقات مع أطراف داخلية وخارجية على مستويات تنفيذية واستراتيجية، بما يدعم تحقيق أهداف المؤسسة وبناء الشراكات طويلة المدى."),
                        }),
                    new("Persuasion and Negotiation", "الإقناع والتفاوض", "Measures the extent to which the job requires influencing others, gaining commitment to ideas or recommendations, and negotiating agreements or solutions to achieve business objectives.", "يقيس مدى حاجة الوظيفة إلى التأثير على الآخرين، وإقناعهم بالأفكار أو التوصيات، والتفاوض للوصول إلى اتفاقات أو حلول تحقق أهداف العمل.", 30m, true,
                        new OptionDef[]
                        {
                            new("No Persuasion or Negotiation Required", "لا يتطلب إقناعًا أو تفاوضًا", "Communication is limited to exchanging information or following instructions, with no need to influence or negotiate with others.", "يقتصر التواصل على نقل المعلومات أو تنفيذ التعليمات، دون الحاجة إلى التأثير على الآخرين أو التفاوض معهم."),
                            new("Limited Persuasion", "إقناع محدود", "The job requires explaining ideas or procedures and gaining acceptance in routine situations.", "تتطلب الوظيفة شرح الأفكار أو الإجراءات وإقناع الآخرين بقبولها في مواقف روتينية."),
                            new("Operational Persuasion and Negotiation", "إقناع وتفاوض تشغيلي", "The job requires influencing others and negotiating to resolve issues, coordinate work, and reach agreements within the job's area of responsibility.", "تتطلب الوظيفة إقناع الآخرين والتفاوض معهم لحل المشكلات أو تنسيق الأعمال وتحقيق اتفاقات ضمن نطاق العمل."),
                            new("Significant Negotiation", "تفاوض مؤثر", "The job requires leading negotiations with internal or external parties on significant issues and balancing multiple interests to reach mutually beneficial outcomes.", "تتطلب الوظيفة إدارة مفاوضات مع أطراف داخلية أو خارجية بشأن قضايا مؤثرة، وتحقيق توافق بين مصالح متعددة."),
                            new("Strategic Negotiation", "تفاوض استراتيجي", "The job requires leading high-impact strategic negotiations involving major agreements, partnerships, or critical organizational matters.", "تتطلب الوظيفة قيادة مفاوضات استراتيجية ذات تأثير كبير على المؤسسة، مثل الاتفاقيات الرئيسية أو الشراكات أو القضايا عالية الأهمية."),
                        }),
                }),
            new("COND", "Working Environment and Conditions", "ظروف وبيئة العمل", "Measures the nature of the working environment and conditions required by the job, including exposure to environmental or operational factors, physical demands, occupational risks, and workplace-related pressures.", "يقيس طبيعة وظروف بيئة العمل التي تتطلبها الوظيفة، بما في ذلك التعرض للعوامل البيئية أو التشغيلية، والمتطلبات البدنية، ومستوى المخاطر، والضغوط الناتجة عن بيئة العمل.", 5m,
                new QuestionDef[]
                {
                    new("Nature of the Work Environment", "طبيعة بيئة العمل", "Measures the nature of the environment in which the job is performed and the extent to which it involves operational, field, or unusual environmental conditions that may affect job performance or require special precautions.", "يقيس طبيعة البيئة التي تُؤدى فيها الوظيفة، ومدى تعرض شاغل الوظيفة لظروف تشغيلية أو ميدانية أو بيئية غير اعتيادية قد تؤثر على أداء العمل أو تتطلب احتياطات خاصة.", 20m, true,
                        new OptionDef[]
                        {
                            new("Standard Office Environment", "بيئة مكتبية اعتيادية", "Work is performed entirely in a safe, stable, and comfortable office environment with no unusual operational or environmental conditions.", "يتم أداء العمل بالكامل في بيئة مكتبية مستقرة وآمنة ومريحة، دون التعرض لظروف تشغيلية أو بيئية غير اعتيادية."),
                            new("Mixed Work Environment", "بيئة عمل مختلطة", "Work is primarily office-based but occasionally requires travel, site visits, or exposure to light operational conditions.", "يتم أداء العمل في بيئة مكتبية مع الحاجة أحيانًا للتنقل أو زيارة مواقع عمل أو التعامل مع ظروف تشغيلية بسيطة."),
                            new("Operational or Field Environment", "بيئة تشغيلية أو ميدانية", "The job regularly requires working in operational or field environments with exposure to conditions that require compliance with safety procedures.", "تتطلب الوظيفة العمل بانتظام في مواقع تشغيلية أو ميدانية، مع التعرض لظروف بيئية تتطلب الالتزام بإجراءات السلامة."),
                            new("Demanding Work Environment", "بيئة عمل صعبة", "The job frequently involves demanding environments such as noisy, hot, industrial, or construction settings, requiring continuous safety awareness and precautions.", "تتطلب الوظيفة العمل في بيئات تتسم بظروف تشغيلية أو بيئية صعبة، مثل الضوضاء، أو الحرارة، أو المواقع الصناعية أو الإنشائية، مع الحاجة إلى احتياطات مستمرة."),
                            new("Highly Hazardous or Exceptional Environment", "بيئة عالية الخطورة أو استثنائية", "The job requires continuous work in highly hazardous or exceptional environments that may significantly affect health or safety, requiring strict adherence to advanced protective measures and safety controls.", "تتطلب الوظيفة العمل المستمر في بيئات عالية الخطورة أو في ظروف استثنائية قد تؤثر على السلامة أو الصحة، مع تطبيق ضوابط وإجراءات حماية متقدمة."),
                        }),
                    new("Level of Occupational Risk", "مستوى المخاطر المهنية", "Measures the level of exposure to occupational hazards inherent in the job, including physical, chemical, biological, electrical, mechanical, or other workplace risks, and the need to comply with occupational health and safety requirements.", "يقيس مستوى تعرض شاغل الوظيفة للمخاطر المهنية أثناء أداء مهام العمل، مثل المخاطر الفيزيائية أو الكيميائية أو البيولوجية أو الكهربائية أو الميكانيكية أو غيرها، ومدى الحاجة إلى تطبيق إجراءات السلامة والصحة المهنية.", 20m, true,
                        new OptionDef[]
                        {
                            new("Minimal Occupational Risk", "مخاطر مهنية ضئيلة", "The job is performed in a safe environment with little or no exposure to occupational hazards and requires only basic safety practices.", "تُؤدى الوظيفة في بيئة آمنة، مع تعرض محدود أو معدوم للمخاطر المهنية، ولا تتطلب سوى الالتزام بإجراءات السلامة الأساسية."),
                            new("Low Occupational Risk", "مخاطر مهنية منخفضة", "The job involves occasional exposure to minor operational hazards that can be effectively managed through standard safety procedures.", "قد تتضمن الوظيفة تعرضًا محدودًا لمخاطر تشغيلية بسيطة يمكن التحكم فيها بسهولة من خلال إجراءات السلامة المعتادة."),
                            new("Moderate Occupational Risk", "مخاطر مهنية متوسطة", "The job involves regular exposure to occupational hazards requiring adherence to safety procedures and the use of appropriate protective equipment.", "تتطلب الوظيفة التعرض المنتظم لبعض المخاطر المهنية التي تستلزم الالتزام بإجراءات وقائية واستخدام معدات الحماية المناسبة."),
                            new("High Occupational Risk", "مخاطر مهنية مرتفعة", "The job frequently exposes employees to significant occupational hazards that may affect health or safety, requiring enhanced safety controls and protective measures.", "تتضمن الوظيفة التعرض المتكرر لمخاطر مهنية ذات تأثير محتمل على السلامة أو الصحة، مما يتطلب تطبيق ضوابط وإجراءات سلامة متقدمة."),
                            new("Very High Occupational Risk", "مخاطر مهنية عالية جدًا", "The job involves continuous exposure to severe occupational hazards that may pose significant risks to health or safety, requiring strict compliance with advanced safety procedures, specialized protective equipment, and rigorous controls.", "تتضمن الوظيفة التعرض المستمر لمخاطر مهنية جسيمة قد تهدد السلامة أو الصحة، وتتطلب التزامًا صارمًا بإجراءات السلامة، ومعدات حماية متخصصة، وضوابط رقابية مشددة."),
                        }),
                    new("Physical Demands", "الضغوط الجسدية", "Measures the level of physical effort and demands required to perform the job, including prolonged standing or walking, lifting and carrying loads, repetitive movements, or working in physically demanding positions.", "يقيس مستوى الجهد والمتطلبات البدنية اللازمة لأداء مهام الوظيفة، بما في ذلك الوقوف أو المشي لفترات طويلة، أو حمل ونقل الأوزان، أو أداء حركات متكررة، أو العمل في أوضاع بدنية مجهدة.", 20m, true,
                        new OptionDef[]
                        {
                            new("Minimal Physical Demands", "متطلبات بدنية محدودة", "The job is primarily performed while seated or with minimal movement and requires little physical effort.", "تُؤدى الوظيفة في الغالب أثناء الجلوس أو الحركة المحدودة، ولا تتطلب مجهودًا بدنيًا يُذكر."),
                            new("Light Physical Demands", "متطلبات بدنية خفيفة", "The job requires intermittent standing, walking, or movement, with occasional lifting or carrying of light loads.", "تتطلب الوظيفة الوقوف أو المشي أو الحركة بشكل متقطع، مع رفع أو حمل أوزان خفيفة عند الحاجة."),
                            new("Moderate Physical Demands", "متطلبات بدنية متوسطة", "The job requires regular standing or movement, lifting moderate loads, or performing physically demanding tasks on a recurring basis.", "تتطلب الوظيفة الوقوف أو الحركة لفترات منتظمة، أو رفع أوزان متوسطة، أو أداء أعمال بدنية بشكل متكرر."),
                            new("High Physical Demands", "متطلبات بدنية مرتفعة", "The job requires substantial physical effort, including lifting or moving heavy loads or working for extended periods in physically demanding positions.", "تتطلب الوظيفة مجهودًا بدنيًا كبيرًا، مثل رفع أو نقل أوزان ثقيلة، أو العمل لفترات طويلة في أوضاع بدنية مجهدة."),
                            new("Very High Physical Demands", "متطلبات بدنية شاقة", "The job requires continuous, strenuous physical effort as an integral part of daily work activities.", "تتطلب الوظيفة مجهودًا بدنيًا مستمرًا وشاقًا، مع التعرض لإجهاد بدني مرتفع بشكل يومي كجزء أساسي من طبيعة العمل."),
                        }),
                    new("Mental and Psychological Demands", "الضغوط الذهنية والنفسية", "Measures the level of concentration, analytical thinking, sustained attention, ability to cope with work pressure, complex or sensitive situations, and the psychological demands required to perform the job effectively.", "يقيس مستوى التركيز، والتحليل، والانتباه المستمر، والقدرة على التعامل مع ضغوط العمل، والمواقف المعقدة أو الحساسة، والمتطلبات النفسية اللازمة لأداء مهام الوظيفة بفعالية.", 20m, true,
                        new OptionDef[]
                        {
                            new("Minimal Mental and Psychological Demands", "ضغوط ذهنية ونفسية محدودة", "The job requires minimal concentration, involves routine tasks, and exposes the employee to little work-related pressure.", "تتطلب الوظيفة مستوى منخفضًا من التركيز، مع ضغوط عمل محدودة ومهام روتينية يمكن أداؤها بسهولة."),
                            new("Light Mental and Psychological Demands", "ضغوط خفيفة", "The job requires regular concentration to perform daily tasks and involves predictable, manageable work pressures.", "تتطلب الوظيفة تركيزًا منتظمًا للتعامل مع المهام اليومية، مع ضغوط عمل يمكن التنبؤ بها وإدارتها بسهولة."),
                            new("Moderate Mental and Psychological Demands", "ضغوط متوسطة", "The job requires sustained concentration, information analysis, and the ability to manage multiple priorities or respond promptly to changing situations.", "تتطلب الوظيفة تركيزًا مستمرًا، وتحليلًا للمعلومات، والتعامل مع أولويات متعددة أو مواقف تتطلب سرعة الاستجابة."),
                            new("High Mental and Psychological Demands", "ضغوط مرتفعة", "The job requires frequent decision-making under time or operational pressure and managing complex or sensitive situations requiring high levels of concentration and emotional control.", "تتطلب الوظيفة اتخاذ قرارات متكررة في ظل ضغوط زمنية أو تشغيلية، والتعامل مع مواقف معقدة أو حساسة تتطلب مستوى عاليًا من التركيز وضبط النفس."),
                            new("Very High Mental and Psychological Demands", "ضغوط عالية جدًا", "The job requires sustained high levels of concentration and professional judgment while operating under continuous pressure and managing highly complex or sensitive situations with significant consequences.", "تتطلب الوظيفة المحافظة على مستويات عالية من التركيز والحكم المهني لفترات طويلة، مع تحمل ضغوط مستمرة ومسؤولية التعامل مع مواقف بالغة التعقيد أو الحساسية قد يترتب عليها آثار كبيرة."),
                        }),
                    new("Exposure to Unusual Environmental Conditions", "التعرض للظروف غير الاعتيادية", "Measures the extent to which the job requires exposure to unusual environmental conditions, such as extreme temperatures, noise, dust, humidity, vibration, or working in open, confined, or elevated areas, and the need for special precautions.", "يقيس مدى تعرض شاغل الوظيفة لظروف بيئية غير اعتيادية أثناء أداء العمل، مثل درجات الحرارة المرتفعة أو المنخفضة، والضوضاء، والغبار، والرطوبة، والاهتزازات، أو العمل في أماكن مفتوحة أو مرتفعة أو ضيقة، وما يتطلبه ذلك من احتياطات خاصة.", 20m, true,
                        new OptionDef[]
                        {
                            new("No Unusual Environmental Conditions", "لا توجد ظروف غير اعتيادية", "The job is performed in a stable and comfortable environment with no exposure to unusual environmental conditions.", "تُؤدى الوظيفة في بيئة مستقرة ومريحة، دون التعرض لظروف بيئية غير اعتيادية."),
                            new("Limited Exposure", "تعرض محدود", "The job occasionally involves brief or infrequent exposure to unusual environmental conditions.", "قد تتطلب الوظيفة التعرض العرضي لبعض الظروف البيئية غير الاعتيادية لفترات قصيرة أو بشكل غير متكرر."),
                            new("Regular Exposure", "تعرض منتظم", "The job regularly involves exposure to one or more unusual environmental conditions, requiring adherence to standard safety measures.", "تتطلب الوظيفة التعرض المنتظم لواحد أو أكثر من الظروف البيئية غير الاعتيادية، مع تطبيق إجراءات السلامة المناسبة."),
                            new("Frequent Exposure", "تعرض متكرر", "The job frequently requires working under difficult or uncomfortable environmental conditions that affect the nature of the work and require additional precautions.", "تتطلب الوظيفة العمل بشكل متكرر في ظروف بيئية صعبة أو غير مريحة تؤثر على طبيعة أداء العمل، مع الحاجة إلى احتياطات إضافية."),
                            new("Continuous Exposure to Severe Conditions", "تعرض مستمر لظروف قاسية", "The job requires continuous work under severe or exceptional environmental conditions, requiring specialized protective equipment and strict safety procedures.", "تتطلب الوظيفة العمل المستمر في ظروف بيئية قاسية أو استثنائية تتطلب معدات حماية خاصة وإجراءات سلامة مشددة لضمان سلامة العاملين."),
                        }),
                }),
        };

        var factorBuilds = new List<(Factor Factor, List<(Question Question, AnswerOption[] Options)> Questions)>();
        var factorSort = 1;
        foreach (var fd in factorDefs)
        {
            var factor = version.AddFactor(fd.Code, fd.NameEn, fd.NameAr, fd.HelpTextEn, fd.HelpTextAr, fd.Weight, factorSort++);

            var questionBuilds = new List<(Question, AnswerOption[])>();
            var questionSort = 1;
            foreach (var qd in fd.Questions)
            {
                var question = factor.AddQuestion(qd.TextEn, qd.TextAr, qd.HelpTextEn, qd.HelpTextAr, QuestionType.SingleChoice, qd.Weight, qd.IsRequired, questionSort++);
                var options = qd.Options
                    .Select((od, i) => question.AddAnswerOption(od.LabelEn, od.LabelAr, od.HelpTextEn, od.HelpTextAr, rating: i + 1, sortOrder: i + 1))
                    .ToArray();
                questionBuilds.Add((question, options));
            }

            factorBuilds.Add((factor, questionBuilds));
        }

        // ---- Grade mappings: assign G1..G11 (rank order), then auto-tile the point budget across them.
        // G12 (exec) is out of evaluation scope. ----
        var gradeCodesInRankOrder = new[] { "G1", "G2", "G3", "G4", "G5", "G6", "G7", "G8", "G9", "G10", "G11" };
        var gradeMappings = gradeCodesInRankOrder.Select(code => version.AssignGrade(grades[code].Id)).ToList();
        version.AutoAssignGradeRanges(gradeMappings.Select(m => m.Id).ToList());

        Guid? ResolveGrade(int total) => gradeMappings
            .Where(m => m.MinScore <= total && total <= m.MaxScore)
            .Select(m => (Guid?)m.GradeId)
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
        // Ratings (1-5, unified scale) per factor, in KNW/PS/RI/LED/COMM/COND order (factorBuilds order).
        // Each factor may have several questions; the same rating is applied to every question in that
        // factor for a given evaluation - a deliberate simplification for demo data (real evaluators
        // answer each question independently). Scores are still computed for real via each question's
        // actual weight, so factor weights (30/20/25/15/5/5) genuinely drive the totals.
        var evalDefs = new (string Job, int[] Ratings, bool Approve)[]
        {
            ("SUP-1",   new[] { 1, 1, 1, 1, 1, 1 }, true),
            ("ENG-SE1", new[] { 2, 2, 2, 1, 2, 1 }, true),
            ("QA-1",    new[] { 2, 2, 2, 1, 2, 1 }, true),
            ("ENG-SE2", new[] { 3, 2, 2, 1, 2, 1 }, true),
            ("OPS-1",   new[] { 3, 3, 3, 1, 2, 2 }, true),
            ("DAT-DE",  new[] { 3, 3, 3, 1, 3, 1 }, true),
            ("SEC-1",   new[] { 4, 3, 3, 1, 3, 2 }, true),
            ("ENG-SE3", new[] { 4, 4, 3, 2, 3, 1 }, true),
            ("ENG-TL",  new[] { 4, 4, 4, 3, 4, 1 }, true),
            ("ENG-EM",  new[] { 4, 4, 4, 5, 4, 1 }, true),
            ("PMO-PM",  new[] { 3, 3, 4, 3, 5, 1 }, false), // left Submitted (pending approval)
            ("FIN-M",   new[] { 3, 3, 4, 4, 4, 1 }, false), // left Submitted
        };

        var evalCount = 0;
        foreach (var e in evalDefs)
        {
            var job = jobs[e.Job];
            var eval = Evaluation.CreateDraft(job.Id, version.Id, evaluatorEmployeeId: null);
            var total = 0;
            for (var i = 0; i < factorBuilds.Count; i++)
            {
                var (factor, questions) = factorBuilds[i];
                var rating = e.Ratings[i];
                var factorScore = 0;

                foreach (var (question, options) in questions)
                {
                    var option = options.First(o => o.Rating == rating);
                    var questionPoints = version.MaxPoints * factor.Weight / 100m * question.Weight / 100m;
                    var answerScore = (int)Math.Round(questionPoints * (rating / 5m), MidpointRounding.AwayFromZero);

                    db.EvaluationAnswers.Add(EvaluationAnswer.Create(eval.Id, question.Id, option.Id, option.Rating));
                    factorScore += answerScore;
                }

                db.EvaluationFactorScores.Add(EvaluationFactorScore.Create(eval.Id, factor.Id, factorScore));
                total += factorScore;
            }
            var gradeId = ResolveGrade(total);
            eval.Submit(total, gradeId);
            if (e.Approve)
            {
                eval.Approve();
                if (gradeId is { } g) job.AssignGrade(g, GradeSource.Evaluated);
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
