import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Api } from '../../core/api';
import { Session } from '../../core/session';
import { I18n } from '../../core/i18n';
import { Job, Level, JobFamily } from '../../core/models';

@Component({
  selector: 'pr-grading',
  imports: [RouterLink],
  styles: [`
    .matrix-wrap { overflow-x: auto; }
    .matrix { width: 100%; border-collapse: collapse; }
    .matrix th { padding: .5rem .75rem; text-align: center; font-size: .9rem; font-weight: 500; color: var(--text-muted); border-bottom: 1px solid var(--border); background: var(--surface-2); }
    .matrix th.level-corner { text-align: start; width: 110px; }
    .matrix th.unassigned { font-style: italic; }
    .matrix td.level-label { padding: .75rem .875rem; border-inline-end: 1px solid var(--border); font-size: .9rem; font-weight: 600; color: var(--text); vertical-align: top; background: var(--surface-2); white-space: nowrap; min-width: 90px; }
    .matrix td.level-label .level-code { font-size: .8rem; font-weight: 400; color: var(--text-muted); margin-top: 1px; }
    .matrix td.cell { padding: .45rem; vertical-align: top; border-top: 1px solid var(--border); border-inline-start: 1px solid var(--border); min-width: 140px; }
    .job-card { display: block; background: var(--surface); border: 1px solid var(--border); border-radius: 6px; padding: .4rem .6rem; margin-bottom: 4px; text-decoration: none; transition: border-color .15s; }
    .job-card:hover { border-color: var(--primary); }
    .job-card-title { font-size: .88rem; font-weight: 500; color: var(--text); line-height: 1.3; }
    .grade-chip { display: inline-block; margin-top: 3px; font-size: .68rem; font-weight: 600; background: var(--surface-2); color: var(--text-muted); border: 1px solid var(--border); border-radius: 3px; padding: 0 4px; letter-spacing: .01em; }
    .cell-empty { display: block; text-align: center; padding: 1.25rem 0; color: var(--border); font-size: .9rem; }
  `],
  template: `
    <div class="head">
      <div><h1>{{ i18n.t('nav.grading') }}</h1></div>
    </div>

    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (!session.hasTenant()) { <div class="alert info">{{ i18n.t('common.selectClient') }}</div> }

    @if (outOfScopeLevels().length) {
      <div class="alert info" style="margin-bottom:1rem">
        @for (l of outOfScopeLevels(); track l.id) {
          <span><strong>{{ l.code }}</strong> ({{ i18n.name(l.nameEn, l.nameAr) }}) is out of evaluation scope and not shown in the grid. </span>
        }
      </div>
    }

    <div class="card">
      @if (loading()) {
        <p class="muted">{{ i18n.t('common.loading') }}</p>
      } @else if (matrixLevels().length === 0) {
        <p class="empty">No levels in evaluation scope. Add levels in <strong>Configuration → Levels, Families &amp; Grades</strong>.</p>
      } @else {
        <div class="matrix-wrap">
          <table class="matrix">
            <thead>
              <tr>
                <th class="level-corner"></th>
                @for (f of matrixFamilies(); track f.id) {
                  <th>{{ i18n.name(f.nameEn, f.nameAr) }}</th>
                }
                @if (hasUnassigned()) {
                  <th class="unassigned">Unassigned</th>
                }
              </tr>
            </thead>
            <tbody>
              @for (l of matrixLevels(); track l.id) {
                <tr>
                  <td class="level-label">
                    <div>{{ i18n.name(l.nameEn, l.nameAr) }}</div>
                    <div class="level-code">{{ l.code }}</div>
                  </td>
                  @for (f of matrixFamilies(); track f.id) {
                    <td class="cell">
                      @for (j of jobsAt(l.id, f.id); track j.id) {
                        <a [routerLink]="['/jobs', j.id]" class="job-card">
                          <div class="job-card-title">{{ i18n.name(j.titleEn, j.titleAr) }}</div>
                          @if (j.gradeCode) { <span class="grade-chip">{{ j.gradeCode }}</span> }
                        </a>
                      }
                      @if (!jobsAt(l.id, f.id).length) { <span class="cell-empty">—</span> }
                    </td>
                  }
                  @if (hasUnassigned()) {
                    <td class="cell">
                      @for (j of jobsAt(l.id, null); track j.id) {
                        <a [routerLink]="['/jobs', j.id]" class="job-card">
                          <div class="job-card-title">{{ i18n.name(j.titleEn, j.titleAr) }}</div>
                          @if (j.gradeCode) { <span class="grade-chip">{{ j.gradeCode }}</span> }
                        </a>
                      }
                      @if (!jobsAt(l.id, null).length) { <span class="cell-empty">—</span> }
                    </td>
                  }
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
})
export class Grading {
  private api = inject(Api);
  readonly session = inject(Session);
  readonly i18n = inject(I18n);

  readonly jobs = signal<Job[]>([]);
  readonly levels = signal<Level[]>([]);
  readonly families = signal<JobFamily[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly matrixLevels = computed(() =>
    this.levels().filter(l => l.inEvalScope).sort((a, b) => b.rank - a.rank)
  );

  readonly outOfScopeLevels = computed(() =>
    this.levels().filter(l => !l.inEvalScope)
  );

  readonly matrixFamilies = computed(() =>
    [...this.families()].sort((a, b) => a.code.localeCompare(b.code))
  );

  readonly hasUnassigned = computed(() =>
    this.jobs().some(j => j.jobFamilyId === null)
  );

  jobsAt(levelId: string, familyId: string | null): Job[] {
    return this.jobs().filter(j => j.levelId === levelId && j.jobFamilyId === familyId);
  }

  constructor() { effect(() => { this.session.tenantId(); this.load(); }); }

  private async load() {
    if (!this.session.hasTenant()) {
      this.jobs.set([]); this.levels.set([]); this.families.set([]);
      return;
    }
    this.loading.set(true); this.error.set(null);
    try {
      const [j, l, f] = await Promise.all([this.api.jobs(), this.api.levels(), this.api.families()]);
      this.jobs.set(j); this.levels.set(l); this.families.set(f);
    } catch (e: any) { this.error.set(e?.error?.detail ?? 'Failed to load grading structure.'); }
    finally { this.loading.set(false); }
  }
}
