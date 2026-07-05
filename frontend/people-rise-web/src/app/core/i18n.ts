import { Injectable, computed, effect, signal } from '@angular/core';

export type Lang = 'en' | 'ar';
const KEY = 'pr.lang';

const DICT: Record<Lang, Record<string, string>> = {
  en: {
    'app.subtitle': 'Job & Reward Design',
    'nav.dashboard': 'Dashboard', 'nav.jobs': 'Jobs',
    'nav.evaluation': 'Job evaluation', 'nav.salary': 'Salary builder', 'nav.settings': 'Settings',
    'tenant.active': 'Active client', 'tenant.select': '— select a client —',
    'common.code': 'Code', 'common.status': 'Status', 'common.save': 'Save', 'common.cancel': 'Cancel',
    'common.edit': 'Edit', 'common.add': 'Add', 'common.create': 'Create', 'common.loading': 'Loading…',
    'common.none': 'None yet.', 'common.refresh': 'Refresh', 'common.open': 'Open', 'common.back': 'Back',
    'common.nameEn': 'Name (English)', 'common.nameAr': 'Name (Arabic)', 'common.optional': 'optional',
    'common.description': 'Description', 'common.selectClient': 'Select a client above first.',
    'field.level': 'Level', 'field.family': 'Family', 'field.grade': 'Grade', 'field.rank': 'Rank',
    'field.title': 'Title', 'field.band': 'Salary band', 'field.inScope': 'In eval scope',
    'field.weight': 'Weight', 'field.sort': 'Sort', 'field.points': 'Points', 'field.help': 'Help text',
    'yes': 'Yes', 'no': 'No',
    'dash.title': 'Dashboard', 'dash.subtitle': "Overview of this client's job & reward design.",
    'dash.jobs': 'Jobs', 'dash.evaluated': 'Evaluated', 'dash.grades': 'Grades', 'dash.bands': 'Salary bands',
    'dash.recent': 'Recent evaluations', 'dash.noTenant': 'Select a client above to begin.',
    'jobs.title': 'Jobs', 'jobs.none': 'No jobs yet.', 'jobs.notGraded': 'not graded yet',
    'jobs.new': 'New job', 'jobs.titleEn': 'Title (English)', 'jobs.titleAr': 'Title (Arabic)',
    'job.evaluations': 'Evaluations', 'job.noBand': 'No salary band yet — the job needs a grade first.',
    'job.evaluate': 'New evaluation', 'job.noEvals': 'No evaluations yet.',
    'job.setGrade': 'Set grade', 'job.gradeAssignedDirectly': 'Grade assigned directly — no evaluation on file.',
    'job.setGradeHint': 'Assign a grade directly, without running an evaluation.',
    'job.confirmSetGrade': 'Assign grade',
    'job.provenance.evaluated': 'Evaluated', 'job.provenance.assigned': 'Assigned',
    'salary.title': 'Salary builder', 'salary.subtitle': 'Price each grade into a band (min / midpoint / max).',
    'salary.generate': 'Generate bands', 'salary.generateHint': 'Build a band for every grade from a base midpoint, spread and progression.',
    'salary.base': 'Base midpoint', 'salary.spread': 'Spread % (max/min−1)', 'salary.progression': 'Progression %',
    'salary.currency': 'Currency', 'salary.effective': 'Effective date', 'salary.midpoint': 'Midpoint',
    'salary.min': 'Min', 'salary.max': 'Max', 'salary.noBand': 'No band', 'salary.addBand': 'Add band', 'salary.run': 'Generate',
    'eval.methodologies': 'Methodologies', 'eval.evaluations': 'Evaluations', 'eval.newMethodology': 'New methodology',
    'eval.newVersion': 'New draft version', 'eval.openEditor': 'Open editor', 'eval.publish': 'Publish version',
    'eval.factors': 'Factors & questions', 'eval.gradeMappings': 'Grade mappings', 'eval.addFactor': 'Add factor',
    'eval.addQuestion': 'Add question', 'eval.addOption': 'Add option', 'eval.addMapping': 'Add mapping',
    'eval.questionEn': 'Question (English)', 'eval.questionAr': 'Question (Arabic)', 'eval.labelEn': 'Answer (English)', 'eval.labelAr': 'Answer (Arabic)',
    'eval.questionnaire': 'Questionnaire', 'eval.submit': 'Submit & score', 'eval.approve': 'Approve', 'eval.answered': 'answered',
    'eval.score': 'total score', 'eval.breakdown': 'Per-factor breakdown', 'eval.audit': 'Audit trail',
    'eval.newEvaluation': 'New evaluation', 'eval.pickJob': 'Job', 'eval.pickVersion': 'Methodology version (Active)',
    'eval.start': 'Start evaluation', 'eval.noActive': 'No Active versions. Publish one first.', 'eval.readonly': 'This version is read-only.',
    'eval.draftHint': 'Draft — add and edit factors, questions, options and grade mappings, then publish.',
    'eval.singleChoice': 'Single choice', 'eval.multipleChoice': 'Multiple choice',
    'settings.title': 'Settings', 'settings.appearance': 'Appearance', 'settings.theme': 'Theme',
    'settings.language': 'Language', 'settings.light': 'Light', 'settings.dark': 'Dark',
    'settings.data': 'Demo data', 'settings.seed': 'Create El-Delta demo client',
    'settings.seedHint': 'Provisions a sample Egyptian IT company (≈150–250 staff) with full demo data.',
    'nav.jobLibrary': 'Job Library', 'nav.grading': 'Grading Structure',
    'nav.taxonomy': 'Levels, Families & Grades', 'nav.methodology': 'Methodologies',
    'nav.groupDesign': 'Job & Reward Design', 'nav.groupConfig': 'Configuration',
    'common.delete': 'Delete',
    'confirm.ok': 'Confirm', 'confirm.delete': 'Delete',
    'confirm.job.title': 'Delete job?', 'confirm.job.body': 'This cannot be undone.',
    'confirm.publish.title': 'Publish this version?',
    'confirm.publish.body': 'Publishing is permanent — the version becomes read-only and future evaluations will pin to it.',
    'confirm.generate.title': 'Replace all bands?',
    'confirm.generate.body': 'This replaces all existing bands, including manual edits. Continue?',
    'toast.saved': 'Saved', 'toast.created': 'Created', 'toast.deleted': 'Deleted',
    'toast.published': 'Version published', 'toast.approved': 'Approved',
    'toast.submitted': 'Submitted & scored', 'toast.generated': 'Bands generated',
    'eval.rank': 'Rank', 'eval.sortScore': 'Score ↓', 'eval.sortDate': 'Date',
  },
  ar: {
    'app.subtitle': 'تصميم الوظائف والأجور',
    'nav.dashboard': 'لوحة التحكم', 'nav.jobs': 'الوظائف',
    'nav.evaluation': 'تقييم الوظائف', 'nav.salary': 'باني الأجور', 'nav.settings': 'الإعدادات',
    'tenant.active': 'العميل الحالي', 'tenant.select': '— اختر عميلاً —',
    'common.code': 'الرمز', 'common.status': 'الحالة', 'common.save': 'حفظ', 'common.cancel': 'إلغاء',
    'common.edit': 'تعديل', 'common.add': 'إضافة', 'common.create': 'إنشاء', 'common.loading': 'جارٍ التحميل…',
    'common.none': 'لا يوجد بعد.', 'common.refresh': 'تحديث', 'common.open': 'فتح', 'common.back': 'رجوع',
    'common.nameEn': 'الاسم (إنجليزي)', 'common.nameAr': 'الاسم (عربي)', 'common.optional': 'اختياري',
    'common.description': 'الوصف', 'common.selectClient': 'اختر عميلاً بالأعلى أولاً.',
    'field.level': 'المستوى', 'field.family': 'العائلة', 'field.grade': 'الدرجة', 'field.rank': 'الترتيب',
    'field.title': 'المسمى', 'field.band': 'نطاق الأجر', 'field.inScope': 'ضمن نطاق التقييم',
    'field.weight': 'الوزن', 'field.sort': 'الترتيب', 'field.points': 'النقاط', 'field.help': 'نص المساعدة',
    'yes': 'نعم', 'no': 'لا',
    'dash.title': 'لوحة التحكم', 'dash.subtitle': 'نظرة عامة على تصميم الوظائف والأجور لهذا العميل.',
    'dash.jobs': 'الوظائف', 'dash.evaluated': 'مُقيّمة', 'dash.grades': 'الدرجات', 'dash.bands': 'نطاقات الأجور',
    'dash.recent': 'أحدث التقييمات', 'dash.noTenant': 'اختر عميلاً بالأعلى للبدء.',
    'jobs.title': 'الوظائف', 'jobs.none': 'لا توجد وظائف بعد.', 'jobs.notGraded': 'لم تُقيّم بعد',
    'jobs.new': 'وظيفة جديدة', 'jobs.titleEn': 'المسمى (إنجليزي)', 'jobs.titleAr': 'المسمى (عربي)',
    'job.evaluations': 'التقييمات', 'job.noBand': 'لا يوجد نطاق أجر — تحتاج الوظيفة إلى درجة أولاً.',
    'job.evaluate': 'تقييم جديد', 'job.noEvals': 'لا توجد تقييمات بعد.',
    'job.setGrade': 'تحديد الدرجة', 'job.gradeAssignedDirectly': 'تم إسناد الدرجة مباشرة — لا يوجد تقييم مسجّل.',
    'job.setGradeHint': 'إسناد الدرجة مباشرة، دون إجراء تقييم.',
    'job.confirmSetGrade': 'إسناد الدرجة',
    'job.provenance.evaluated': 'عبر التقييم', 'job.provenance.assigned': 'إسناد مباشر',
    'salary.title': 'باني الأجور', 'salary.subtitle': 'سعّر كل درجة في نطاق (الأدنى / الوسط / الأعلى).',
    'salary.generate': 'توليد النطاقات', 'salary.generateHint': 'بناء نطاق لكل درجة من وسط أساسي ومدى وتدرّج.',
    'salary.base': 'الوسط الأساسي', 'salary.spread': '٪ المدى (الأعلى/الأدنى−1)', 'salary.progression': '٪ التدرّج',
    'salary.currency': 'العملة', 'salary.effective': 'تاريخ السريان', 'salary.midpoint': 'الوسط',
    'salary.min': 'الأدنى', 'salary.max': 'الأعلى', 'salary.noBand': 'لا يوجد نطاق', 'salary.addBand': 'إضافة نطاق', 'salary.run': 'توليد',
    'eval.methodologies': 'المنهجيات', 'eval.evaluations': 'التقييمات', 'eval.newMethodology': 'منهجية جديدة',
    'eval.newVersion': 'نسخة مسودة جديدة', 'eval.openEditor': 'فتح المحرر', 'eval.publish': 'نشر النسخة',
    'eval.factors': 'العوامل والأسئلة', 'eval.gradeMappings': 'ربط الدرجات', 'eval.addFactor': 'إضافة عامل',
    'eval.addQuestion': 'إضافة سؤال', 'eval.addOption': 'إضافة إجابة', 'eval.addMapping': 'إضافة ربط',
    'eval.questionEn': 'السؤال (إنجليزي)', 'eval.questionAr': 'السؤال (عربي)', 'eval.labelEn': 'الإجابة (إنجليزي)', 'eval.labelAr': 'الإجابة (عربي)',
    'eval.questionnaire': 'الاستبيان', 'eval.submit': 'إرسال واحتساب', 'eval.approve': 'اعتماد', 'eval.answered': 'تمت الإجابة',
    'eval.score': 'النتيجة الإجمالية', 'eval.breakdown': 'التفصيل حسب العامل', 'eval.audit': 'سجل التدقيق',
    'eval.newEvaluation': 'تقييم جديد', 'eval.pickJob': 'الوظيفة', 'eval.pickVersion': 'نسخة المنهجية (نشطة)',
    'eval.start': 'بدء التقييم', 'eval.noActive': 'لا توجد نسخ نشطة. انشر واحدة أولاً.', 'eval.readonly': 'هذه النسخة للقراءة فقط.',
    'eval.draftHint': 'مسودة — أضف وعدّل العوامل والأسئلة والإجابات وربط الدرجات، ثم انشر.',
    'eval.singleChoice': 'اختيار واحد', 'eval.multipleChoice': 'اختيار متعدد',
    'settings.title': 'الإعدادات', 'settings.appearance': 'المظهر', 'settings.theme': 'السمة',
    'settings.language': 'اللغة', 'settings.light': 'فاتح', 'settings.dark': 'داكن',
    'settings.data': 'بيانات تجريبية', 'settings.seed': 'إنشاء عميل El-Delta التجريبي',
    'settings.seedHint': 'يُنشئ شركة تقنية مصرية نموذجية (≈150–250 موظفًا) ببيانات تجريبية كاملة.',
    'nav.jobLibrary': 'مكتبة الوظائف', 'nav.grading': 'هيكل الدرجات',
    'nav.taxonomy': 'المستويات والعائلات والدرجات', 'nav.methodology': 'المنهجيات',
    'nav.groupDesign': 'تصميم الوظائف والأجور', 'nav.groupConfig': 'الضبط والإعداد',
    'common.delete': 'حذف',
    'confirm.ok': 'تأكيد', 'confirm.delete': 'حذف',
    'confirm.job.title': 'حذف الوظيفة؟', 'confirm.job.body': 'لا يمكن التراجع عن هذا.',
    'confirm.publish.title': 'نشر هذه النسخة؟',
    'confirm.publish.body': 'النشر دائم — تصبح النسخة للقراءة فقط وستُستخدم في التقييمات المستقبلية.',
    'confirm.generate.title': 'استبدال جميع النطاقات؟',
    'confirm.generate.body': 'سيؤدي ذلك إلى استبدال النطاقات الحالية بما فيها التعديلات اليدوية. هل تريد المتابعة؟',
    'toast.saved': 'تم الحفظ', 'toast.created': 'تم الإنشاء', 'toast.deleted': 'تم الحذف',
    'toast.published': 'تم نشر النسخة', 'toast.approved': 'تم الاعتماد',
    'toast.submitted': 'تم الإرسال والاحتساب', 'toast.generated': 'تم توليد النطاقات',
    'eval.rank': 'الترتيب', 'eval.sortScore': 'النتيجة ↓', 'eval.sortDate': 'التاريخ',
  },
};

/** Default English. Switches the whole interface (incl. RTL) and resolves bilingual entity names. */
@Injectable({ providedIn: 'root' })
export class I18n {
  readonly lang = signal<Lang>((localStorage.getItem(KEY) as Lang) || 'en');
  readonly dir = computed<'ltr' | 'rtl'>(() => (this.lang() === 'ar' ? 'rtl' : 'ltr'));

  constructor() {
    effect(() => {
      const l = this.lang();
      document.documentElement.setAttribute('lang', l);
      document.documentElement.setAttribute('dir', this.dir());
      localStorage.setItem(KEY, l);
    });
  }

  toggle() { this.lang.update((l) => (l === 'en' ? 'ar' : 'en')); }
  set(l: Lang) { this.lang.set(l); }

  /** Static UI string. */
  t(key: string): string { return DICT[this.lang()][key] ?? DICT.en[key] ?? key; }

  /** Bilingual entity name: Arabic when UI is Arabic AND an Arabic value exists; else English. */
  name(en?: string | null, ar?: string | null): string {
    if (this.lang() === 'ar' && ar && ar.trim().length) return ar;
    return en ?? '';
  }
}
