// TypeScript shapes mirroring the bilingual backend DTOs.

export interface TenantAccess { tenantId: string; name: string; ownerType: string | number; status: string | number; role: string | number; }

export interface Level { id: string; code: string; nameEn: string; nameAr: string | null; rank: number; inEvalScope: boolean; }
export interface Grade { id: string; code: string; nameEn: string; nameAr: string | null; rank: number; levelId: string | null; levelCode: string | null; }
export interface JobFamily { id: string; code: string; nameEn: string; nameAr: string | null; }

export interface JobBand { currency: string; minAmount: number; midpoint: number; maxAmount: number; }
export interface Job {
  id: string; code: string; titleEn: string; titleAr: string | null;
  descriptionEn: string | null; descriptionAr: string | null;
  levelId: string; levelCode: string | null; levelNameEn: string | null; levelNameAr: string | null;
  jobFamilyId: string | null; jobFamilyCode: string | null; jobFamilyNameEn: string | null; jobFamilyNameAr: string | null;
  gradeId: string | null; gradeCode: string | null; gradeNameEn: string | null; gradeNameAr: string | null;
  status: string; band: JobBand | null;
}

export interface MethodologyVersion { id: string; versionNo: number; status: string; note: string | null; publishedAt: string | null; }
export interface Methodology { id: string; code: string; nameEn: string; nameAr: string | null; versions: MethodologyVersion[]; }

export type QuestionType = 'SingleChoice' | 'MultipleChoice';

export interface AnswerOption { id: string; labelEn: string; labelAr: string | null; points: number; sortOrder: number; }
export interface QuestionDetail { id: string; questionTextEn: string; questionTextAr: string | null; helpTextEn: string | null; helpTextAr: string | null; questionType: QuestionType; sortOrder: number; options: AnswerOption[]; }
export interface FactorDetail { id: string; code: string; nameEn: string; nameAr: string | null; weight: number; sortOrder: number; questions: QuestionDetail[]; }
export interface GradeMapping { id: string; gradeId: string; gradeCode: string | null; minScore: number; maxScore: number; }
export interface MethodologyVersionDetail {
  id: string; methodologyId: string; methodologyCode: string; methodologyNameEn: string; methodologyNameAr: string | null;
  versionNo: number; status: string; note: string | null; publishedAt: string | null;
  factors: FactorDetail[]; gradeMappings: GradeMapping[];
}

export interface FactorScore { factorId: string; factorCode: string; factorNameEn: string; factorNameAr: string | null; score: number; }
export interface AnswerAudit { questionId: string; questionTextEn: string; questionTextAr: string | null; answerOptionId: string; answerLabelEn: string; answerLabelAr: string | null; pointsSnapshot: number; }
export interface EvaluationResult {
  id: string; jobId: string; jobCode: string; jobTitleEn: string; jobTitleAr: string | null;
  methodologyVersionId: string; status: string; totalScore: number | null;
  recommendedGradeId: string | null; recommendedGradeCode: string | null; recommendedGradeNameEn: string | null; recommendedGradeNameAr: string | null;
  submittedAt: string | null; approvedAt: string | null; factorScores: FactorScore[]; answers: AnswerAudit[];
}
export interface EvaluationListItem {
  id: string; jobId: string; jobCode: string; jobTitleEn: string; jobTitleAr: string | null;
  methodologyVersionId: string; status: string; totalScore: number | null;
  recommendedGradeId: string | null; recommendedGradeCode: string | null; createdAt: string;
}
export interface AnswerSelection { questionId: string; answerOptionIds: string[]; }

export interface SalaryBandInfo { id: string; currency: string; minAmount: number; midpoint: number; maxAmount: number; spreadPct: number; overlapPct: number; effectiveDate: string; status: string; }
export interface SalaryBandRow { gradeId: string; gradeCode: string; gradeNameEn: string; gradeNameAr: string | null; rank: number; levelCode: string | null; band: SalaryBandInfo | null; }
