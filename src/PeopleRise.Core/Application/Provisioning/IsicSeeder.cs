using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;

namespace PeopleRise.Core.Application.Provisioning;

/// <summary>Seeds the ISIC Rev. 4 reference list (Core Spec §4: section + division level, seeded
/// into every tenant at provisioning). The full standard at this cut: 21 sections (A-U) and their
/// 88 divisions - "roughly a hundred entries", per the spec's own description of the right
/// granularity. A later list version (e.g. ISIC Rev. 5) is offered, never auto-applied to an
/// existing tenant (Core Spec §4) - SeedVersion records which version each tenant has.</summary>
internal static class IsicSeeder
{
    public const string SeedName = "Isic";
    public const int CurrentVersion = 2;

    public static async Task SeedAsync(CoreDbContext db, CancellationToken ct = default)
    {
        if (await db.SeedVersions.AnyAsync(v => v.Name == SeedName, ct))
        {
            return;   // idempotent - already applied
        }

        var sections = new (string Code, string En, string Ar)[]
        {
            ("A", "Agriculture, forestry and fishing", "الزراعة والحراجة وصيد الأسماك"),
            ("B", "Mining and quarrying", "التعدين واستغلال المحاجر"),
            ("C", "Manufacturing", "الصناعات التحويلية"),
            ("D", "Electricity, gas, steam and air conditioning supply", "إمدادات الكهرباء والغاز والبخار وتكييف الهواء"),
            ("E", "Water supply; sewerage, waste management and remediation activities", "إمدادات المياه؛ الصرف الصحي؛ إدارة النفايات ومعالجتها"),
            ("F", "Construction", "التشييد والبناء"),
            ("G", "Wholesale and retail trade; repair of motor vehicles and motorcycles", "تجارة الجملة والتجزئة؛ إصلاح المركبات الآلية والدراجات النارية"),
            ("H", "Transportation and storage", "النقل والتخزين"),
            ("I", "Accommodation and food service activities", "أنشطة الإقامة والخدمات الغذائية"),
            ("J", "Information and communication", "المعلومات والاتصالات"),
            ("K", "Financial and insurance activities", "الأنشطة المالية وأنشطة التأمين"),
            ("L", "Real estate activities", "الأنشطة العقارية"),
            ("M", "Professional, scientific and technical activities", "الأنشطة المهنية والعلمية والتقنية"),
            ("N", "Administrative and support service activities", "الأنشطة الإدارية وخدمات الدعم"),
            ("O", "Public administration and defence; compulsory social security", "الإدارة العامة والدفاع؛ الضمان الاجتماعي الإلزامي"),
            ("P", "Education", "التعليم"),
            ("Q", "Human health and social work activities", "أنشطة صحة الإنسان والعمل الاجتماعي"),
            ("R", "Arts, entertainment and recreation", "الفنون والترفيه والتسلية"),
            ("S", "Other service activities", "أنشطة الخدمات الأخرى"),
            ("T", "Activities of households as employers; undifferentiated goods- and services-producing activities of households for own use", "أنشطة الأسر المعيشية بوصفها أرباب عمل"),
            ("U", "Activities of extraterritorial organizations and bodies", "أنشطة المنظمات والهيئات غير الإقليمية"),
        };
        foreach (var (code, en, ar) in sections)
            db.IndustryClassifications.Add(IndustryClassification.Create(code, IsicLevel.Section, null, en, ar));

        var divisions = new (string Code, string ParentCode, string En, string Ar)[]
        {
            // A - Agriculture, forestry and fishing
            ("01", "A", "Crop and animal production, hunting and related service activities", "إنتاج المحاصيل والإنتاج الحيواني والصيد وما يتصل بذلك من أنشطة خدمية"),
            ("02", "A", "Forestry and logging", "الحراجة وقطع الأخشاب"),
            ("03", "A", "Fishing and aquaculture", "صيد الأسماك وتربية الأحياء المائية"),
            // B - Mining and quarrying
            ("05", "B", "Mining of coal and lignite", "تعدين الفحم والفحم الحجري اللين"),
            ("06", "B", "Extraction of crude petroleum and natural gas", "استخراج النفط الخام والغاز الطبيعي"),
            ("07", "B", "Mining of metal ores", "تعدين ركاز الفلزات"),
            ("08", "B", "Other mining and quarrying", "أنشطة التعدين واستغلال المحاجر الأخرى"),
            ("09", "B", "Mining support service activities", "الأنشطة الخدمية الداعمة للتعدين"),
            // C - Manufacturing
            ("10", "C", "Manufacture of food products", "صناعة المنتجات الغذائية"),
            ("11", "C", "Manufacture of beverages", "صناعة المشروبات"),
            ("12", "C", "Manufacture of tobacco products", "صناعة منتجات التبغ"),
            ("13", "C", "Manufacture of textiles", "صناعة المنسوجات"),
            ("14", "C", "Manufacture of wearing apparel", "صناعة الملابس"),
            ("15", "C", "Manufacture of leather and related products", "صناعة الجلود والمنتجات ذات الصلة"),
            ("16", "C", "Manufacture of wood and of products of wood and cork, except furniture", "صناعة الخشب ومنتجات الخشب والفلين، باستثناء الأثاث"),
            ("17", "C", "Manufacture of paper and paper products", "صناعة الورق ومنتجات الورق"),
            ("18", "C", "Printing and reproduction of recorded media", "الطباعة واستنساخ وسائط الإعلام المسجلة"),
            ("19", "C", "Manufacture of coke and refined petroleum products", "صناعة فحم الكوك ومنتجات النفط المكرر"),
            ("20", "C", "Manufacture of chemicals and chemical products", "صناعة الكيماويات والمنتجات الكيماوية"),
            ("21", "C", "Manufacture of pharmaceuticals, medicinal chemical and botanical products", "صناعة المستحضرات الصيدلانية والمنتجات الكيماوية الطبية والنباتية"),
            ("22", "C", "Manufacture of rubber and plastics products", "صناعة المطاط ومنتجات اللدائن"),
            ("23", "C", "Manufacture of other non-metallic mineral products", "صناعة منتجات المعادن اللافلزية الأخرى"),
            ("24", "C", "Manufacture of basic metals", "صناعة الفلزات القاعدية"),
            ("25", "C", "Manufacture of fabricated metal products, except machinery and equipment", "صناعة منتجات الفلزات المشكّلة، باستثناء الآلات والمعدات"),
            ("26", "C", "Manufacture of computer, electronic and optical products", "صناعة الحاسوب والمنتجات الإلكترونية والبصرية"),
            ("27", "C", "Manufacture of electrical equipment", "صناعة المعدات الكهربائية"),
            ("28", "C", "Manufacture of machinery and equipment n.e.c.", "صناعة الآلات والمعدات غير المصنفة في موضع آخر"),
            ("29", "C", "Manufacture of motor vehicles, trailers and semi-trailers", "صناعة المركبات الآلية والمقطورات ونصف المقطورات"),
            ("30", "C", "Manufacture of other transport equipment", "صناعة معدات النقل الأخرى"),
            ("31", "C", "Manufacture of furniture", "صناعة الأثاث"),
            ("32", "C", "Other manufacturing", "الصناعات التحويلية الأخرى"),
            ("33", "C", "Repair and installation of machinery and equipment", "إصلاح وتركيب الآلات والمعدات"),
            // D - Electricity, gas, steam and air conditioning supply
            ("35", "D", "Electricity, gas, steam and air conditioning supply", "إمدادات الكهرباء والغاز والبخار وتكييف الهواء"),
            // E - Water supply; sewerage, waste management and remediation activities
            ("36", "E", "Water collection, treatment and supply", "تجميع المياه ومعالجتها وإمدادها"),
            ("37", "E", "Sewerage", "الصرف الصحي"),
            ("38", "E", "Waste collection, treatment and disposal activities; materials recovery", "جمع النفايات ومعالجتها والتخلص منها؛ استرداد المواد"),
            ("39", "E", "Remediation activities and other waste management services", "أنشطة المعالجة وخدمات إدارة النفايات الأخرى"),
            // F - Construction
            ("41", "F", "Construction of buildings", "تشييد المباني"),
            ("42", "F", "Civil engineering", "الأعمال الهندسية المدنية"),
            ("43", "F", "Specialized construction activities", "أنشطة التشييد المتخصصة"),
            // G - Wholesale and retail trade; repair of motor vehicles and motorcycles
            ("45", "G", "Wholesale and retail trade and repair of motor vehicles and motorcycles", "تجارة الجملة والتجزئة وإصلاح المركبات الآلية والدراجات النارية"),
            ("46", "G", "Wholesale trade, except of motor vehicles and motorcycles", "تجارة الجملة، باستثناء المركبات الآلية والدراجات النارية"),
            ("47", "G", "Retail trade, except of motor vehicles and motorcycles", "تجارة التجزئة، باستثناء المركبات الآلية والدراجات النارية"),
            // H - Transportation and storage
            ("49", "H", "Land transport and transport via pipelines", "النقل البري والنقل عبر الأنابيب"),
            ("50", "H", "Water transport", "النقل المائي"),
            ("51", "H", "Air transport", "النقل الجوي"),
            ("52", "H", "Warehousing and support activities for transportation", "التخزين وأنشطة الدعم للنقل"),
            ("53", "H", "Postal and courier activities", "أنشطة البريد والبريد السريع"),
            // I - Accommodation and food service activities
            ("55", "I", "Accommodation", "الإقامة"),
            ("56", "I", "Food and beverage service activities", "أنشطة خدمات الأطعمة والمشروبات"),
            // J - Information and communication
            ("58", "J", "Publishing activities", "أنشطة النشر"),
            ("59", "J", "Motion picture, video and television programme production, sound recording and music publishing activities", "إنتاج الأفلام السينمائية والفيديو وبرامج التلفزيون، وتسجيل الصوت ونشر الموسيقى"),
            ("60", "J", "Programming and broadcasting activities", "أنشطة البرمجة والبث الإذاعي"),
            ("61", "J", "Telecommunications", "الاتصالات"),
            ("62", "J", "Computer programming, consultancy and related activities", "برمجة الحاسوب والاستشارات وما يتصل بذلك من أنشطة"),
            ("63", "J", "Information service activities", "أنشطة خدمات المعلومات"),
            // K - Financial and insurance activities
            ("64", "K", "Financial service activities, except insurance and pension funding", "الأنشطة المالية الخدمية، باستثناء التأمين وتمويل المعاشات التقاعدية"),
            ("65", "K", "Insurance, reinsurance and pension funding, except compulsory social security", "التأمين وإعادة التأمين وتمويل المعاشات التقاعدية، باستثناء الضمان الاجتماعي الإلزامي"),
            ("66", "K", "Activities auxiliary to financial service and insurance activities", "الأنشطة المساعدة للخدمات المالية وأنشطة التأمين"),
            // L - Real estate activities
            ("68", "L", "Real estate activities", "الأنشطة العقارية"),
            // M - Professional, scientific and technical activities
            ("69", "M", "Legal and accounting activities", "الأنشطة القانونية والمحاسبية"),
            ("70", "M", "Activities of head offices; management consultancy activities", "أنشطة المكاتب الرئيسية؛ أنشطة الاستشارات الإدارية"),
            ("71", "M", "Architectural and engineering activities; technical testing and analysis", "الأنشطة المعمارية والهندسية؛ الاختبار والتحليل التقني"),
            ("72", "M", "Scientific research and development", "البحث والتطوير العلمي"),
            ("73", "M", "Advertising and market research", "الإعلان وبحوث السوق"),
            ("74", "M", "Other professional, scientific and technical activities", "الأنشطة المهنية والعلمية والتقنية الأخرى"),
            ("75", "M", "Veterinary activities", "الأنشطة البيطرية"),
            // N - Administrative and support service activities
            ("77", "N", "Rental and leasing activities", "أنشطة التأجير التمويلي والتشغيلي"),
            ("78", "N", "Employment activities", "أنشطة التوظيف"),
            ("79", "N", "Travel agency, tour operator, reservation service and related activities", "وكالات السفر ومنظمي الرحلات وخدمات الحجز وما يتصل بها من أنشطة"),
            ("80", "N", "Security and investigation activities", "أنشطة الأمن والتحري"),
            ("81", "N", "Services to buildings and landscape activities", "خدمات المباني وتنسيق المواقع"),
            ("82", "N", "Office administrative, office support and other business support activities", "الأنشطة الإدارية المكتبية وخدمات الدعم المكتبي وأنشطة دعم الأعمال الأخرى"),
            // O - Public administration and defence; compulsory social security
            ("84", "O", "Public administration and defence; compulsory social security", "الإدارة العامة والدفاع؛ الضمان الاجتماعي الإلزامي"),
            // P - Education
            ("85", "P", "Education", "التعليم"),
            // Q - Human health and social work activities
            ("86", "Q", "Human health activities", "أنشطة صحة الإنسان"),
            ("87", "Q", "Residential care activities", "أنشطة الرعاية المؤسسية"),
            ("88", "Q", "Social work activities without accommodation", "أنشطة العمل الاجتماعي دون إقامة"),
            // R - Arts, entertainment and recreation
            ("90", "R", "Creative, arts and entertainment activities", "الأنشطة الإبداعية والفنية والترفيهية"),
            ("91", "R", "Libraries, archives, museums and other cultural activities", "المكتبات والمحفوظات والمتاحف والأنشطة الثقافية الأخرى"),
            ("92", "R", "Gambling and betting activities", "أنشطة المقامرة والمراهنات"),
            ("93", "R", "Sports activities and amusement and recreation activities", "الأنشطة الرياضية وأنشطة التسلية والترفيه"),
            // S - Other service activities
            ("94", "S", "Activities of membership organizations", "أنشطة منظمات العضوية"),
            ("95", "S", "Repair of computers and personal and household goods", "إصلاح أجهزة الحاسوب والسلع الشخصية والمنزلية"),
            ("96", "S", "Other personal service activities", "أنشطة الخدمات الشخصية الأخرى"),
            // T - Activities of households as employers
            ("97", "T", "Activities of households as employers of domestic personnel", "أنشطة الأسر المعيشية بوصفها أرباب عمل للعاملين المنزليين"),
            ("98", "T", "Undifferentiated goods- and services-producing activities of private households for own use", "أنشطة إنتاج السلع والخدمات غير المميزة للأسر المعيشية الخاصة للاستخدام الخاص"),
            // U - Activities of extraterritorial organizations and bodies
            ("99", "U", "Activities of extraterritorial organizations and bodies", "أنشطة المنظمات والهيئات غير الإقليمية"),
        };
        foreach (var (code, parent, en, ar) in divisions)
            db.IndustryClassifications.Add(IndustryClassification.Create(code, IsicLevel.Division, parent, en, ar));

        db.SeedVersions.Add(SeedVersion.Create(SeedName, CurrentVersion));

        await db.SaveChangesAsync(ct);
    }
}
