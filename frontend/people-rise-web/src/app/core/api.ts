import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { API_BASE } from './config';
import {
  Level, Grade, JobFamily, Job, Methodology, MethodologyVersion,
  MethodologyVersionDetail, EvaluationResult, EvaluationListItem, AnswerSelection, SalaryBandRow, QuestionType,
} from './models';

/** Thin typed wrapper over the People Rise API. Headers are added by the session interceptor. */
@Injectable({ providedIn: 'root' })
export class Api {
  constructor(private http: HttpClient) {}

  private get<T>(p: string) { return firstValueFrom(this.http.get<T>(`${API_BASE}${p}`)); }
  private post<T>(p: string, b: unknown) { return firstValueFrom(this.http.post<T>(`${API_BASE}${p}`, b)); }
  private put<T>(p: string, b: unknown) { return firstValueFrom(this.http.put<T>(`${API_BASE}${p}`, b)); }

  // structure
  levels() { return this.get<Level[]>('/levels'); }
  createLevel(b: { code: string; nameEn: string; nameAr: string | null; rank: number; inEvalScope: boolean }) { return this.post<Level>('/levels', b); }
  updateLevel(id: string, b: { code: string; nameEn: string; nameAr: string | null; rank: number; inEvalScope: boolean }) { return this.put<Level>(`/levels/${id}`, b); }
  deleteLevel(id: string) { return firstValueFrom(this.http.delete(`${API_BASE}/levels/${id}`)); }
  grades() { return this.get<Grade[]>('/grades'); }
  createGrade(b: { code: string; nameEn: string; nameAr: string | null; rank: number; levelId: string | null }) { return this.post<Grade>('/grades', b); }
  updateGrade(id: string, b: { code: string; nameEn: string; nameAr: string | null; rank: number; levelId: string | null }) { return this.put<Grade>(`/grades/${id}`, b); }
  deleteGrade(id: string) { return firstValueFrom(this.http.delete(`${API_BASE}/grades/${id}`)); }
  families() { return this.get<JobFamily[]>('/job-families'); }
  createFamily(b: { code: string; nameEn: string; nameAr: string | null }) { return this.post<JobFamily>('/job-families', b); }
  updateFamily(id: string, b: { code: string; nameEn: string; nameAr: string | null }) { return this.put<JobFamily>(`/job-families/${id}`, b); }
  deleteFamily(id: string) { return firstValueFrom(this.http.delete(`${API_BASE}/job-families/${id}`)); }

  jobs() { return this.get<Job[]>('/jobs'); }
  job(id: string) { return this.get<Job>(`/jobs/${id}`); }
  createJob(b: { code: string; titleEn: string; titleAr: string | null; levelId: string; descriptionEn: string | null; descriptionAr: string | null; jobFamilyId: string | null }) { return this.post<Job>('/jobs', b); }
  updateJob(id: string, b: { code: string; titleEn: string; titleAr: string | null; levelId: string; descriptionEn: string | null; descriptionAr: string | null; jobFamilyId: string | null }) { return this.put<Job>(`/jobs/${id}`, b); }
  deleteJob(id: string) { return firstValueFrom(this.http.delete(`${API_BASE}/jobs/${id}`)); }

  // methodology authoring
  methodologies() { return this.get<Methodology[]>('/methodologies'); }
  createMethodology(b: { code: string; nameEn: string; nameAr: string | null }) { return this.post<Methodology>('/methodologies', b); }
  updateMethodology(id: string, b: { nameEn: string; nameAr: string | null }) { return this.put<Methodology>(`/methodologies/${id}`, b); }
  deleteMethodology(id: string) { return firstValueFrom(this.http.delete(`${API_BASE}/methodologies/${id}`)); }
  createVersion(mid: string, b: { note: string | null }) { return this.post<MethodologyVersion>(`/methodologies/${mid}/versions`, b); }
  version(id: string) { return this.get<MethodologyVersionDetail>(`/methodology-versions/${id}`); }
  publishVersion(id: string) { return this.post<MethodologyVersion>(`/methodology-versions/${id}/publish`, {}); }
  deleteVersion(id: string) { return firstValueFrom(this.http.delete(`${API_BASE}/methodology-versions/${id}`)); }
  addFactor(vid: string, b: { code: string; nameEn: string; nameAr: string | null; sortOrder: number; weight: number | null }) { return this.post<unknown>(`/methodology-versions/${vid}/factors`, b); }
  updateFactor(vid: string, fid: string, b: { code: string; nameEn: string; nameAr: string | null; sortOrder: number; weight: number | null }) { return this.put<unknown>(`/methodology-versions/${vid}/factors/${fid}`, b); }
  addQuestion(fid: string, b: { questionTextEn: string; questionTextAr: string | null; helpTextEn: string | null; helpTextAr: string | null; questionType: QuestionType; sortOrder: number }) { return this.post<unknown>(`/factors/${fid}/questions`, b); }
  updateQuestion(fid: string, qid: string, b: { questionTextEn: string; questionTextAr: string | null; helpTextEn: string | null; helpTextAr: string | null; questionType: QuestionType; sortOrder: number }) { return this.put<unknown>(`/factors/${fid}/questions/${qid}`, b); }
  addOption(qid: string, b: { labelEn: string; labelAr: string | null; points: number; sortOrder: number }) { return this.post<unknown>(`/questions/${qid}/options`, b); }
  updateOption(qid: string, oid: string, b: { labelEn: string; labelAr: string | null; points: number; sortOrder: number }) { return this.put<unknown>(`/questions/${qid}/options/${oid}`, b); }
  deleteFactor(vid: string, fid: string) { return firstValueFrom(this.http.delete(`${API_BASE}/methodology-versions/${vid}/factors/${fid}`)); }
  deleteQuestion(fid: string, qid: string) { return firstValueFrom(this.http.delete(`${API_BASE}/factors/${fid}/questions/${qid}`)); }
  deleteOption(qid: string, oid: string) { return firstValueFrom(this.http.delete(`${API_BASE}/questions/${qid}/options/${oid}`)); }
  addGradeMapping(vid: string, b: { gradeId: string; minScore: number; maxScore: number }) { return this.post<unknown>(`/methodology-versions/${vid}/grade-mappings`, b); }
  updateGradeMapping(vid: string, mid: string, b: { gradeId: string; minScore: number; maxScore: number }) { return this.put<unknown>(`/methodology-versions/${vid}/grade-mappings/${mid}`, b); }
  deleteGradeMapping(vid: string, mid: string) { return firstValueFrom(this.http.delete(`${API_BASE}/methodology-versions/${vid}/grade-mappings/${mid}`)); }

  // evaluations
  evaluations() { return this.get<EvaluationListItem[]>('/evaluations'); }
  evaluation(id: string) { return this.get<EvaluationResult>(`/evaluations/${id}`); }
  createEvaluation(b: { jobId: string; methodologyVersionId: string; evaluatorEmployeeId: string | null }) { return this.post<{ id: string }>('/evaluations', b); }
  submitAnswers(id: string, answers: AnswerSelection[]) { return this.post<EvaluationResult>(`/evaluations/${id}/answers`, { answers }); }
  approve(id: string) { return this.post<EvaluationResult>(`/evaluations/${id}/approve`, {}); }

  // salary builder
  salaryBands() { return this.get<SalaryBandRow[]>('/salary-bands'); }
  createSalaryBand(b: { gradeId: string; currency: string; midpoint: number; spreadPct: number; overlapPct: number; effectiveDate: string }) { return this.post<SalaryBandRow>('/salary-bands', b); }
  updateSalaryBand(id: string, b: { currency: string; midpoint: number; spreadPct: number; overlapPct: number; effectiveDate: string }) { return this.put<SalaryBandRow>(`/salary-bands/${id}`, b); }
  generateBands(b: { baseMidpoint: number; spreadPct: number; progressionPct: number; currency: string; effectiveDate: string }) { return this.post<SalaryBandRow[]>('/salary-bands/generate', b); }

  // demo
  seedElDelta() { return this.post<{ id: string; name: string }>('/admin/demo/el-delta', {}); }
}
