import { Component, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Api } from '../../core/api';
import { Session } from '../../core/session';
import { I18n } from '../../core/i18n';
import { Job } from '../../core/models';

interface ActiveVersion { versionId: string; label: string; }

@Component({
  selector: 'pr-new-evaluation',
  imports: [FormsModule, RouterLink],
  template: `
    <a routerLink="/evaluation" class="muted">← {{ i18n.t('nav.evaluation') }}</a>
    <h1>{{ i18n.t('eval.newEvaluation') }}</h1>

    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (!session.hasTenant()) { <div class="alert info">{{ i18n.t('common.selectClient') }}</div> }

    <div class="card">
      <div class="field"><label>{{ i18n.t('eval.pickJob') }}</label>
        <select [(ngModel)]="jobId">
          <option [ngValue]="''">—</option>
          @for (j of jobs(); track j.id) { <option [ngValue]="j.id">{{ i18n.name(j.titleEn, j.titleAr) }} ({{ j.code }})</option> }
        </select>
      </div>
      <div class="field"><label>{{ i18n.t('eval.pickVersion') }}</label>
        <select [(ngModel)]="versionId">
          <option [ngValue]="''">—</option>
          @for (v of versions(); track v.versionId) { <option [ngValue]="v.versionId">{{ v.label }}</option> }
        </select>
        @if (versions().length === 0 && session.hasTenant()) { <p class="muted">{{ i18n.t('eval.noActive') }}</p> }
      </div>
      <button (click)="start()" [disabled]="!jobId || !versionId || busy()">{{ i18n.t('eval.start') }}</button>
    </div>
  `,
})
export class NewEvaluation {
  private api = inject(Api);
  readonly router = inject(Router);
  private route = inject(ActivatedRoute);
  readonly session = inject(Session);
  readonly i18n = inject(I18n);
  readonly jobs = signal<Job[]>([]);
  readonly versions = signal<ActiveVersion[]>([]);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);
  jobId = '';
  versionId = '';

  constructor() {
    effect(() => { this.session.tenantId(); this.load(); });
    const pre = this.route.snapshot.queryParamMap.get('jobId');
    if (pre) this.jobId = pre;
  }

  private async load() {
    if (!this.session.hasTenant()) return;
    this.error.set(null);
    try {
      const [jobs, methodologies] = await Promise.all([this.api.jobs(), this.api.methodologies()]);
      this.jobs.set(jobs);
      const active: ActiveVersion[] = [];
      for (const m of methodologies)
        for (const v of m.versions)
          if (v.status === 'Active') active.push({ versionId: v.id, label: `${this.i18n.name(m.nameEn, m.nameAr)} v${v.versionNo}` });
      this.versions.set(active);
    } catch (e: any) { this.error.set(e?.error?.detail ?? 'Failed to load.'); }
  }

  async start() {
    this.busy.set(true); this.error.set(null);
    try {
      const created = await this.api.createEvaluation({ jobId: this.jobId, methodologyVersionId: this.versionId, evaluatorEmployeeId: null });
      await this.router.navigate(['/evaluation', created.id]);
    } catch (e: any) { this.error.set(e?.error?.detail ?? 'Failed to create evaluation.'); }
    finally { this.busy.set(false); }
  }
}
