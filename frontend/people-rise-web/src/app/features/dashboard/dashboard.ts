import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { Api } from '../../core/api';
import { Session } from '../../core/session';
import { I18n } from '../../core/i18n';
import { Job, EvaluationListItem, Grade, SalaryBandRow } from '../../core/models';

@Component({
  selector: 'pr-dashboard',
  imports: [RouterLink, DatePipe],
  styles: [`
    /* ── Stat cards ── */
    .stats { display: grid; grid-template-columns: repeat(4, 1fr); gap: 0.75rem; margin-bottom: 1.1rem; }
    .stat {
      background: var(--surface); border: 1px solid var(--border);
      border-radius: var(--radius-lg); padding: 1rem 1.1rem;
    }
    .stat-icon-row { display: flex; align-items: center; justify-content: space-between; margin-bottom: 0.6rem; }
    .stat-icon {
      width: 2rem; height: 2rem; border-radius: var(--radius);
      background: var(--primary-soft); display: grid; place-items: center;
    }
    .stat-icon i { font-size: 1rem; color: var(--primary); }
    .stat-delta {
      font-size: 0.75rem; padding: 0.1rem 0.45rem; border-radius: 999px;
      background: var(--success-soft); color: var(--success); font-weight: 500;
    }
    .stat-val { font-size: 1.5rem; font-weight: 500; color: var(--text); line-height: 1; }
    .stat-label { font-size: 0.75rem; color: var(--text-faint); margin-top: 0.25rem; }

    /* ── Pipeline strip ── */
    .pipeline-card {
      background: var(--surface); border: 1px solid var(--border);
      border-radius: var(--radius-lg); margin-bottom: 1.1rem; overflow: hidden;
    }
    .pipeline-header {
      display: flex; align-items: center; justify-content: space-between;
      padding: 0.8rem 1.1rem; border-bottom: 1px solid var(--border);
    }
    .pipeline-header h2 { font-size: 0.875rem; font-weight: 500; color: var(--text); margin: 0; }
    .pipeline-header span { font-size: 0.75rem; color: var(--text-faint); }
    .pipeline-steps { display: flex; padding: 1.25rem 1.5rem; gap: 0; }
    .pipe-step {
      flex: 1; display: flex; flex-direction: column; align-items: center;
      gap: 0.4rem; position: relative;
    }
    .pipe-step:not(:last-child)::after {
      content: ''; position: absolute; top: 0.875rem;
      inset-inline-start: 50%; width: 100%; height: 1px;
      background: var(--border); z-index: 0;
    }
    .pipe-step:not(:last-child).done::after { background: var(--success); }
    .pipe-bubble {
      width: 1.75rem; height: 1.75rem; border-radius: 50%;
      border: 1px solid var(--border); background: var(--surface-2);
      display: grid; place-items: center;
      font-size: 0.75rem; font-weight: 500; color: var(--text-muted);
      position: relative; z-index: 1;
    }
    .pipe-bubble.done { background: var(--success-soft); color: var(--success); border-color: var(--success); }
    .pipe-bubble.active { background: var(--primary-soft); color: var(--primary); border-color: var(--primary); }
    .pipe-lbl { font-size: 0.75rem; color: var(--text-faint); text-align: center; white-space: nowrap; }

    /* ── Two-col ── */
    .two-col { display: grid; grid-template-columns: 1fr 252px; gap: 1rem; }

    /* Activity card */
    .activity-card {
      background: var(--surface); border: 1px solid var(--border);
      border-radius: var(--radius-lg); overflow: hidden;
    }
    .card-hd {
      display: flex; align-items: center; justify-content: space-between;
      padding: 0.8rem 1.1rem; border-bottom: 1px solid var(--border);
    }
    .card-hd h2 { font-size: 0.875rem; font-weight: 500; color: var(--text); margin: 0; }
    .view-all { font-size: 0.8125rem; color: var(--primary); text-decoration: none; display: flex; align-items: center; gap: 3px; }
    .view-all:hover { text-decoration: underline; }
    .job-name { font-weight: 500; color: var(--text); text-decoration: none; }
    .job-name:hover { text-decoration: underline; color: var(--primary); }
    .module-tag {
      font-size: 0.75rem; padding: 2px 8px; border-radius: 999px;
      background: var(--surface-2); color: var(--text-muted); border: 1px solid var(--border);
    }

    /* Module shortcuts */
    .module-cards { display: flex; flex-direction: column; gap: 0.6rem; }
    .module-card {
      background: var(--surface); border: 1px solid var(--border);
      border-radius: var(--radius-lg); padding: 0.875rem 1rem;
      display: flex; align-items: center; gap: 0.75rem;
      text-decoration: none; transition: border-color .12s;
    }
    .module-card:hover { border-color: var(--primary); text-decoration: none; }
    .mod-icon {
      width: 2.25rem; height: 2.25rem; border-radius: var(--radius);
      display: grid; place-items: center; flex-shrink: 0;
    }
    .mod-icon i { font-size: 1.1rem; }
    .mod-icon.ev { background: var(--primary-soft); }
    .mod-icon.ev i { color: var(--primary); }
    .mod-icon.gr { background: var(--success-soft); }
    .mod-icon.gr i { color: var(--success); }
    .mod-icon.sb { background: var(--warn-soft); }
    .mod-icon.sb i { color: var(--warn); }
    .mod-name { font-size: 0.875rem; font-weight: 500; color: var(--text); line-height: 1.3; }
    .mod-desc { font-size: 0.75rem; color: var(--text-faint); margin-top: 2px; }
    .mod-arrow { margin-inline-start: auto; color: var(--text-faint); font-size: 1rem; }

    /* Responsive: stack below ~900px */
    @media (max-width: 900px) {
      .stats { grid-template-columns: repeat(2, 1fr); }
      .two-col { grid-template-columns: 1fr; }
    }
  `],
  template: `
    @if (error()) { <div class="alert error">{{ error() }}</div> }

    @if (session.hasTenant()) {

      <!-- Stat cards -->
      <div class="stats">
        <div class="stat">
          <div class="stat-icon-row">
            <div class="stat-icon"><i class="ti ti-briefcase" aria-hidden="true"></i></div>
          </div>
          <div class="stat-val">{{ jobs().length }}</div>
          <div class="stat-label">{{ i18n.t('dash.jobs') }} in library</div>
        </div>
        <div class="stat">
          <div class="stat-icon-row">
            <div class="stat-icon"><i class="ti ti-clipboard-check" aria-hidden="true"></i></div>
            @if (approvedCount() > 0) {
              <span class="stat-delta">{{ approvedCount() }} approved</span>
            }
          </div>
          <div class="stat-val">{{ evaluations().length }}</div>
          <div class="stat-label">Evaluations run</div>
        </div>
        <div class="stat">
          <div class="stat-icon-row">
            <div class="stat-icon"><i class="ti ti-table" aria-hidden="true"></i></div>
          </div>
          <div class="stat-val">{{ grades().length }}</div>
          <div class="stat-label">Grade levels defined</div>
        </div>
        <div class="stat">
          <div class="stat-icon-row">
            <div class="stat-icon"><i class="ti ti-chart-bar" aria-hidden="true"></i></div>
          </div>
          <div class="stat-val">{{ bandCount() }}</div>
          <div class="stat-label">{{ i18n.t('dash.bands') }}</div>
        </div>
      </div>

      <!-- Pipeline strip -->
      <div class="pipeline-card">
        <div class="pipeline-header">
          <h2>Evaluation pipeline</h2>
          <span>{{ jobs().length }} jobs in library</span>
        </div>
        <div class="pipeline-steps">
          <div class="pipe-step" [class.done]="jobs().length > 0">
            <div class="pipe-bubble" [class.done]="jobs().length > 0">
              {{ jobs().length || '—' }}
            </div>
            <div class="pipe-lbl">Jobs added</div>
          </div>
          <div class="pipe-step" [class.done]="evaluations().length > 0">
            <div class="pipe-bubble" [class.done]="evaluations().length > 0">
              {{ evaluations().length || '—' }}
            </div>
            <div class="pipe-lbl">Evaluated</div>
          </div>
          <div class="pipe-step" [class.done]="inReviewCount() > 0">
            <div class="pipe-bubble"
                 [class.done]="inReviewCount() > 0"
                 [class.active]="inReviewCount() > 0">
              {{ inReviewCount() || '—' }}
            </div>
            <div class="pipe-lbl">In review</div>
          </div>
          <div class="pipe-step" [class.done]="approvedCount() > 0">
            <div class="pipe-bubble" [class.done]="approvedCount() > 0">
              {{ approvedCount() || '—' }}
            </div>
            <div class="pipe-lbl">Approved</div>
          </div>
          <div class="pipe-step" [class.done]="grades().length > 0">
            <div class="pipe-bubble" [class.done]="grades().length > 0">
              {{ grades().length || '—' }}
            </div>
            <div class="pipe-lbl">Grade structure</div>
          </div>
          <div class="pipe-step" [class.done]="bandCount() > 0">
            <div class="pipe-bubble" [class.done]="bandCount() > 0">
              {{ bandCount() || '—' }}
            </div>
            <div class="pipe-lbl">Salary bands</div>
          </div>
        </div>
      </div>

      <!-- Two-col -->
      <div class="two-col">

        <!-- Recent activity -->
        <div class="activity-card">
          <div class="card-hd">
            <h2>Recent evaluations</h2>
            <a class="view-all" routerLink="/evaluation">
              View all <i class="ti ti-arrow-right" aria-hidden="true" style="font-size:13px"></i>
            </a>
          </div>
          @if (loading()) {
            <p class="empty">Loading…</p>
          } @else if (evaluations().length === 0) {
            <p class="empty">No evaluations yet. <a routerLink="/evaluation/new">Start one →</a></p>
          } @else {
            <table>
              <thead>
                <tr>
                  <th>Job</th>
                  <th>Score</th>
                  <th>Grade</th>
                  <th>Status</th>
                  <th>Date</th>
                </tr>
              </thead>
              <tbody>
                @for (e of evaluations().slice(0, 7); track e.id) {
                  <tr>
                    <td>
                      <a class="job-name" [routerLink]="['/evaluation', e.id]">{{ i18n.name(e.jobTitleEn, e.jobTitleAr) }}</a>
                      <div class="faint">{{ e.jobCode }}</div>
                    </td>
                    <td>{{ e.totalScore ?? '—' }}</td>
                    <td>
                      @if (e.recommendedGradeCode) {
                        <span class="badge approved">{{ e.recommendedGradeCode }}</span>
                      } @else { <span class="faint">—</span> }
                    </td>
                    <td>
                      <span class="badge {{ e.status.toLowerCase() }}">{{ e.status }}</span>
                    </td>
                    <td class="faint">{{ e.createdAt | date:'d MMM' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          }
        </div>

        <!-- Module shortcuts -->
        <div class="module-cards">
          <a class="module-card" routerLink="/evaluation">
            <div class="mod-icon ev"><i class="ti ti-clipboard-list" aria-hidden="true"></i></div>
            <div>
              <div class="mod-name">Job Evaluation</div>
              <div class="mod-desc">
                @if (pendingCount() > 0) { {{ pendingCount() }} pending · }
                {{ approvedCount() }} approved
              </div>
            </div>
            <i class="ti ti-chevron-right mod-arrow" aria-hidden="true"></i>
          </a>
          <a class="module-card" routerLink="/grading">
            <div class="mod-icon gr"><i class="ti ti-table" aria-hidden="true"></i></div>
            <div>
              <div class="mod-name">Grading Structure</div>
              <div class="mod-desc">
                @if (grades().length > 0) { {{ grades().length }} levels defined }
                @else { Not started }
              </div>
            </div>
            <i class="ti ti-chevron-right mod-arrow" aria-hidden="true"></i>
          </a>
          <a class="module-card" routerLink="/salary">
            <div class="mod-icon sb"><i class="ti ti-chart-bar" aria-hidden="true"></i></div>
            <div>
              <div class="mod-name">Salary Builder</div>
              <div class="mod-desc">
                @if (bandCount() > 0) { {{ bandCount() }} bands configured }
                @else { Not started }
              </div>
            </div>
            <i class="ti ti-chevron-right mod-arrow" aria-hidden="true"></i>
          </a>
        </div>

      </div>

    }
  `,
})
export class Dashboard {
  private api    = inject(Api);
  readonly session = inject(Session);
  readonly i18n    = inject(I18n);

  readonly jobs        = signal<Job[]>([]);
  readonly evaluations = signal<EvaluationListItem[]>([]);
  readonly grades      = signal<Grade[]>([]);
  readonly bands       = signal<SalaryBandRow[]>([]);
  readonly loading     = signal(false);
  readonly error       = signal<string | null>(null);

  readonly approvedCount  = computed(() => this.evaluations().filter(e => e.status === 'Approved').length);
  readonly inReviewCount  = computed(() => this.evaluations().filter(e => e.status === 'Submitted').length);
  readonly pendingCount   = computed(() => this.evaluations().filter(e => e.status !== 'Approved').length);
  readonly bandCount      = computed(() => this.bands().filter(b => b.band).length);

  constructor() {
    effect(() => { this.session.tenantId(); this.load(); });
  }

  private async load() {
    if (!this.session.hasTenant()) {
      this.jobs.set([]); this.evaluations.set([]); this.grades.set([]); this.bands.set([]);
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    try {
      const [jobs, evals, grades, bands] = await Promise.all([
        this.api.jobs(), this.api.evaluations(), this.api.grades(), this.api.salaryBands(),
      ]);
      this.jobs.set(jobs);
      this.evaluations.set(evals);
      this.grades.set(grades);
      this.bands.set(bands);
    } catch (e: any) {
      this.error.set(e?.error?.detail ?? 'Failed to load dashboard data.');
    } finally {
      this.loading.set(false);
    }
  }
}
