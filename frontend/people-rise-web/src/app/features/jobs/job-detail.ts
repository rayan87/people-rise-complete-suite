import { Component, HostListener, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Api } from '../../core/api';
import { I18n } from '../../core/i18n';
import { ToastService } from '../../core/toast';
import { Job, EvaluationListItem, Grade } from '../../core/models';

@Component({
  selector: 'pr-job-detail',
  imports: [RouterLink, DecimalPipe, FormsModule],
  template: `
    <a routerLink="/jobs" class="muted">← {{ i18n.t('nav.jobs') }}</a>
    @if (error()) { <div class="alert error">{{ error() }}</div> }

    @if (job(); as j) {
      <div class="head" style="margin-top:.5rem">
        <div>
          <h1>{{ i18n.name(j.titleEn, j.titleAr) }}</h1>
          <p class="faint">{{ j.code }}</p>
        </div>
        <div class="spacer"></div>
        @if (j.status !== 'Archived') {
          <button class="accent" (click)="openGradeModal(j)">{{ i18n.t('job.setGrade') }}</button>
          <button (click)="evaluate(j.id)">+ {{ i18n.t('job.evaluate') }}</button>
        }
      </div>

      <div class="chips" style="margin-bottom:1.25rem">
        <span class="chip">{{ i18n.t('field.level') }}: {{ i18n.name(j.levelNameEn, j.levelNameAr) }}</span>
        @if (j.jobFamilyCode) { <span class="chip">{{ i18n.t('field.family') }}: {{ i18n.name(j.jobFamilyNameEn, j.jobFamilyNameAr) }}</span> }
        <span class="chip {{ j.gradeCode ? 'good' : '' }}">{{ i18n.t('field.grade') }}: {{ j.gradeCode ?? '—' }}</span>
        @if (j.gradeSource) { <span class="chip">{{ i18n.t('job.provenance.' + j.gradeSource.toLowerCase()) }}</span> }
        <span class="badge {{ j.status.toLowerCase() }}">{{ j.status }}</span>
      </div>

      @if (showGradeModal()) {
        <div class="modal-backdrop" (click)="closeGradeModal()">
          <div class="modal" role="dialog" aria-modal="true" aria-labelledby="grade-modal-title" (click)="$event.stopPropagation()">
            <h2 id="grade-modal-title">{{ i18n.t('job.setGrade') }}</h2>
            <p class="muted">{{ i18n.t('job.setGradeHint') }}</p>
            <div class="field">
              <select [(ngModel)]="modalGradeId">
                <option [ngValue]="null">{{ i18n.t('field.grade') }}…</option>
                @for (g of grades(); track g.id) { <option [ngValue]="g.id">{{ g.code }} — {{ i18n.name(g.nameEn, g.nameAr) }}</option> }
              </select>
            </div>
            <div class="modal-actions">
              <button class="subtle" (click)="closeGradeModal()">{{ i18n.t('common.cancel') }}</button>
              <button [disabled]="!modalGradeId" (click)="confirmSetGrade(j.id)">{{ i18n.t('job.confirmSetGrade') }}</button>
            </div>
          </div>
        </div>
      }

      <div class="row">
        <div class="card">
          <h2>{{ i18n.t('field.band') }}</h2>
          @if (j.band; as b) {
            <div class="bandbar">
              <div class="track"><div class="mid"></div></div>
              <div class="ends">
                <span class="faint">{{ b.currency }} {{ b.minAmount | number:'1.0-0' }}</span>
                <strong>{{ b.midpoint | number:'1.0-0' }}</strong>
                <span class="faint">{{ b.maxAmount | number:'1.0-0' }}</span>
              </div>
            </div>
          } @else { <p class="muted">{{ i18n.t('job.noBand') }}</p> }
        </div>

        <div class="card">
          <h2>{{ i18n.t('job.evaluations') }}</h2>
          @if (j.gradeSource === 'Assigned') { <p class="muted">{{ i18n.t('job.gradeAssignedDirectly') }}</p> }
          @else if (evals().length === 0) { <p class="muted">{{ i18n.t('job.noEvals') }}</p> }
          @else {
            <table>
              <thead><tr><th>{{ i18n.t('common.status') }}</th><th>{{ i18n.t('dash.jobs') }}</th><th>{{ i18n.t('field.grade') }}</th><th></th></tr></thead>
              <tbody>
                @for (e of evals(); track e.id) {
                  <tr>
                    <td><span class="badge {{ e.status.toLowerCase() }}">{{ e.status }}</span></td>
                    <td>{{ e.totalScore ?? '—' }}</td>
                    <td>{{ e.recommendedGradeCode ?? '—' }}</td>
                    <td><a [routerLink]="['/evaluation', e.id]">{{ i18n.t('common.open') }} →</a></td>
                  </tr>
                }
              </tbody>
            </table>
          }
        </div>
      </div>
    } @else if (!error()) { <p>{{ i18n.t('common.loading') }}</p> }
  `,
  styles: [`
    .chips { display:flex; gap:.5rem; flex-wrap:wrap; align-items:center; }
    .chip.good { background:var(--success-soft); color:var(--success); border-color:transparent; }
    .bandbar { margin-top:.5rem; }
    .track { height:8px; border-radius:999px; background:var(--primary-soft); position:relative; margin:18px 0 8px; }
    .track .mid { position:absolute; inset-inline-start:50%; top:-5px; width:2px; height:18px; background:var(--primary); }
    .ends { display:flex; justify-content:space-between; font-size:.9rem; }
  `],
})
export class JobDetail {
  private api = inject(Api);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toast = inject(ToastService);
  readonly i18n = inject(I18n);
  readonly job = signal<Job | null>(null);
  readonly evals = signal<EvaluationListItem[]>([]);
  readonly grades = signal<Grade[]>([]);
  readonly error = signal<string | null>(null);
  readonly showGradeModal = signal(false);
  modalGradeId: string | null = null;

  private get id() { return this.route.snapshot.paramMap.get('id')!; }
  constructor() { this.load(); }
  evaluate(jobId: string) { this.router.navigate(['/evaluation/new'], { queryParams: { jobId } }); }

  openGradeModal(j: Job) {
    this.modalGradeId = j.gradeId ?? null;
    this.showGradeModal.set(true);
  }
  closeGradeModal() { this.showGradeModal.set(false); }

  @HostListener('document:keydown.escape')
  onEscape() { if (this.showGradeModal()) this.closeGradeModal(); }

  private async load() {
    this.error.set(null);
    try {
      const [job, evals, grades] = await Promise.all([this.api.job(this.id), this.api.evaluations(), this.api.grades()]);
      this.job.set(job);
      this.evals.set(evals.filter((e) => e.jobId === this.id));
      this.grades.set(grades);
    } catch (e: any) { this.error.set(e?.error?.detail ?? 'Failed to load job.'); }
  }

  async confirmSetGrade(jobId: string) {
    if (!this.modalGradeId) return;
    try {
      this.job.set(await this.api.assignGrade(jobId, this.modalGradeId));
      this.showGradeModal.set(false);
      this.toast.success(this.i18n.t('toast.saved'));
    } catch (e: any) { this.toast.error(e?.error?.detail ?? 'Failed to set grade.'); }
  }
}
