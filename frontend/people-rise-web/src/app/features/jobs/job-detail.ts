import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { Api } from '../../core/api';
import { I18n } from '../../core/i18n';
import { Job, EvaluationListItem } from '../../core/models';

@Component({
  selector: 'pr-job-detail',
  imports: [RouterLink, DecimalPipe],
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
        <button (click)="evaluate(j.id)">+ {{ i18n.t('job.evaluate') }}</button>
      </div>

      <div class="chips" style="margin-bottom:1.25rem">
        <span class="chip">{{ i18n.t('field.level') }}: {{ i18n.name(j.levelNameEn, j.levelNameAr) }}</span>
        @if (j.jobFamilyCode) { <span class="chip">{{ i18n.t('field.family') }}: {{ i18n.name(j.jobFamilyNameEn, j.jobFamilyNameAr) }}</span> }
        <span class="chip {{ j.gradeCode ? 'good' : '' }}">{{ i18n.t('field.grade') }}: {{ j.gradeCode ?? '—' }}</span>
        <span class="badge {{ j.status.toLowerCase() }}">{{ j.status }}</span>
      </div>

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
          @if (evals().length === 0) { <p class="muted">{{ i18n.t('job.noEvals') }}</p> }
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
  readonly i18n = inject(I18n);
  readonly job = signal<Job | null>(null);
  readonly evals = signal<EvaluationListItem[]>([]);
  readonly error = signal<string | null>(null);

  private get id() { return this.route.snapshot.paramMap.get('id')!; }
  constructor() { this.load(); }
  evaluate(jobId: string) { this.router.navigate(['/evaluation/new'], { queryParams: { jobId } }); }

  private async load() {
    this.error.set(null);
    try {
      const [job, evals] = await Promise.all([this.api.job(this.id), this.api.evaluations()]);
      this.job.set(job);
      this.evals.set(evals.filter((e) => e.jobId === this.id));
    } catch (e: any) { this.error.set(e?.error?.detail ?? 'Failed to load job.'); }
  }
}
