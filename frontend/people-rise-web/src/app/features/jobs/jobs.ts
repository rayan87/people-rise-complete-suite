import { Component, computed, effect, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { Api } from '../../core/api';
import { Session } from '../../core/session';
import { I18n } from '../../core/i18n';
import { ToastService } from '../../core/toast';
import { ConfirmService } from '../../core/confirm';
import { Job, Level, JobFamily, Grade } from '../../core/models';

@Component({
  selector: 'pr-jobs',
  imports: [FormsModule, DecimalPipe, RouterLink],
  styles: [`
    .filters {
      display: flex; gap: 10px; flex-wrap: wrap;
      background: var(--surface-2); border-radius: var(--radius);
      padding: .6rem .75rem; margin-bottom: 1rem; align-items: center;
    }
    .filters label { margin: 0; font-size: .75rem; white-space: nowrap; }
    .filters select { width: auto; padding: .3rem .5rem; font-size: .8rem; }
    .filter-group { display: flex; align-items: center; gap: 5px; }
    .clear-link { font-size: .75rem; color: var(--primary); cursor: pointer; white-space: nowrap; margin-inline-start: auto; }
    td code { font-size: .78rem; background: var(--surface-2); padding: 1px 6px; border-radius: 4px; }
  `],
  template: `
    <div class="head">
      <div><h1>{{ i18n.t('jobs.title') }}</h1></div>
      <div class="spacer"></div>
      <button (click)="showForm.set(!showForm())">{{ showForm() ? i18n.t('common.cancel') : '+ ' + i18n.t('jobs.new') }}</button>
    </div>

    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (!session.hasTenant()) { <div class="alert info">{{ i18n.t('common.selectClient') }}</div> }

    @if (showForm()) {
      <div class="card">
        <h2>{{ i18n.t('jobs.new') }}</h2>
        <div class="row">
          <div class="field"><label>{{ i18n.t('common.code') }}</label><input [(ngModel)]="form.code" placeholder="ENG-SE1" /></div>
          <div class="field"><label>{{ i18n.t('jobs.titleEn') }}</label><input [(ngModel)]="form.titleEn" /></div>
          <div class="field">
            <label>{{ i18n.t('jobs.titleAr') }} <span class="optional">({{ i18n.t('common.optional') }})</span></label>
            <input [(ngModel)]="form.titleAr" dir="rtl" />
          </div>
        </div>
        <div class="row">
          <div class="field">
            <label>{{ i18n.t('field.level') }}</label>
            <select [(ngModel)]="form.levelId">
              <option [ngValue]="''">—</option>
              @for (l of levels(); track l.id) {
                <option [ngValue]="l.id">{{ i18n.name(l.nameEn, l.nameAr) }} ({{ l.code }})</option>
              }
            </select>
          </div>
          <div class="field">
            <label>{{ i18n.t('field.family') }} <span class="optional">({{ i18n.t('common.optional') }})</span></label>
            <select [(ngModel)]="form.jobFamilyId">
              <option [ngValue]="null">—</option>
              @for (f of families(); track f.id) { <option [ngValue]="f.id">{{ i18n.name(f.nameEn, f.nameAr) }}</option> }
            </select>
          </div>
        </div>
        <button (click)="add()" [disabled]="!canAdd()">{{ i18n.t('common.create') }}</button>
      </div>
    }

    <div class="card">
      <!-- Filter bar -->
      <div class="filters">
        <div class="filter-group">
          <label>Family</label>
          <select [ngModel]="filterFamily()" (ngModelChange)="filterFamily.set($event)">
            <option value="">All</option>
            @for (f of families(); track f.id) { <option [value]="f.id">{{ i18n.name(f.nameEn, f.nameAr) }}</option> }
          </select>
        </div>
        <div class="filter-group">
          <label>Level</label>
          <select [ngModel]="filterLevel()" (ngModelChange)="filterLevel.set($event)">
            <option value="">All</option>
            @for (l of levels(); track l.id) { <option [value]="l.id">{{ i18n.name(l.nameEn, l.nameAr) }}</option> }
          </select>
        </div>
        <div class="filter-group">
          <label>Status</label>
          <select [ngModel]="filterStatus()" (ngModelChange)="filterStatus.set($event)">
            <option value="">All</option>
            <option value="Draft">Draft</option>
            <option value="Evaluated">Evaluated</option>
            <option value="Active">Active</option>
            <option value="Archived">Archived</option>
          </select>
        </div>
        <div class="filter-group">
          <label>Grade</label>
          <select [ngModel]="filterGrade()" (ngModelChange)="filterGrade.set($event)">
            <option value="">All</option>
            @for (g of grades(); track g.id) { <option [value]="g.id">{{ g.code }}</option> }
          </select>
        </div>
        @if (filterFamily() || filterLevel() || filterStatus() || filterGrade()) {
          <span class="clear-link" (click)="clearFilters()">Clear filters</span>
        }
      </div>

      @if (loading()) {
        <p>{{ i18n.t('common.loading') }}</p>
      } @else if (filtered().length === 0) {
        <p class="empty">{{ jobs().length === 0 ? i18n.t('jobs.none') : 'No jobs match the current filters.' }}</p>
      } @else {
        <table>
          <thead>
            <tr>
              <th>Code</th>
              <th>{{ i18n.t('field.title') }}</th>
              <th>{{ i18n.t('field.level') }}</th>
              <th>Family</th>
              <th>{{ i18n.t('field.grade') }}</th>
              <th>Band</th>
              <th>{{ i18n.t('common.status') }}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (j of filtered(); track j.id) {
              <tr>
                <td><code>{{ j.code }}</code></td>
                <td><strong>{{ i18n.name(j.titleEn, j.titleAr) }}</strong></td>
                <td class="faint">{{ j.levelCode ?? '—' }}</td>
                <td class="faint">{{ j.jobFamilyCode ?? '—' }}</td>
                <td>{{ j.gradeCode ?? '—' }}</td>
                <td class="faint" style="font-size:.8rem">
                  @if (j.band) {
                    {{ j.band.currency }} {{ j.band.minAmount | number:'1.0-0' }}–{{ j.band.maxAmount | number:'1.0-0' }}
                  } @else { — }
                </td>
                <td><span class="badge {{ j.status.toLowerCase() }}">{{ j.status }}</span></td>
                <td style="text-align:end">
                  <div style="display:flex;gap:5px;justify-content:flex-end">
                    <a [routerLink]="['/jobs', j.id]">
                      <button class="sm subtle">Details</button>
                    </a>
                    <button class="sm subtle icon" (click)="startEdit(j)"
                            [attr.aria-label]="i18n.t('common.edit') + ' ' + i18n.name(j.titleEn, j.titleAr)"
                            [title]="i18n.t('common.edit')">
                      <i class="ti ti-pencil" aria-hidden="true"></i>
                    </button>
                    <button class="sm subtle icon" style="color:var(--danger)" (click)="deleteJob(j.id)"
                            [attr.aria-label]="i18n.t('common.delete') + ' ' + i18n.name(j.titleEn, j.titleAr)"
                            [title]="i18n.t('common.delete')">
                      <i class="ti ti-trash" aria-hidden="true"></i>
                    </button>
                  </div>
                </td>
              </tr>
              @if (editingId() === j.id) {
                <tr>
                  <td colspan="8" style="background:var(--surface-2); padding:.75rem">
                    <div class="row" style="margin:0">
                      <div class="field" style="min-width:100px"><label>Code</label><input [(ngModel)]="editForm.code" /></div>
                      <div class="field"><label>Title (EN)</label><input [(ngModel)]="editForm.titleEn" /></div>
                      <div class="field"><label>Title (AR) <span class="optional">(opt.)</span></label><input [(ngModel)]="editForm.titleAr" dir="rtl" /></div>
                      <div class="field">
                        <label>Level</label>
                        <select [(ngModel)]="editForm.levelId">
                          @for (l of levels(); track l.id) { <option [value]="l.id">{{ l.code }}</option> }
                        </select>
                      </div>
                      <div class="field">
                        <label>Family</label>
                        <select [(ngModel)]="editForm.jobFamilyId">
                          <option [ngValue]="null">—</option>
                          @for (f of families(); track f.id) { <option [value]="f.id">{{ f.code }}</option> }
                        </select>
                      </div>
                      <div style="align-self:flex-end; display:flex; gap:.4rem">
                        <button class="sm" (click)="saveEdit(j.id)" [disabled]="saving()">Save</button>
                        <button class="sm subtle" (click)="editingId.set(null)">Cancel</button>
                      </div>
                    </div>
                  </td>
                </tr>
              }
            }
          </tbody>
        </table>
        <div class="faint" style="font-size:.75rem; padding:.5rem .75rem 0">
          {{ filtered().length }} of {{ jobs().length }} jobs
        </div>
      }
    </div>
  `,
})
export class Jobs {
  private api = inject(Api);
  private router = inject(Router);
  private toast = inject(ToastService);
  private cs = inject(ConfirmService);
  readonly session = inject(Session);
  readonly i18n = inject(I18n);

  readonly jobs = signal<Job[]>([]);
  readonly levels = signal<Level[]>([]);
  readonly families = signal<JobFamily[]>([]);
  readonly grades = signal<Grade[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly showForm = signal(false);
  readonly editingId = signal<string | null>(null);

  readonly filterFamily = signal('');
  readonly filterLevel = signal('');
  readonly filterStatus = signal('');
  readonly filterGrade = signal('');

  form = { code: '', titleEn: '', titleAr: '', levelId: '', jobFamilyId: null as string | null };
  editForm = { code: '', titleEn: '', titleAr: '', levelId: '', jobFamilyId: null as string | null };

  readonly filtered = computed(() => {
    let result = this.jobs();
    if (this.filterFamily()) result = result.filter(j => j.jobFamilyId === this.filterFamily());
    if (this.filterLevel())  result = result.filter(j => j.levelId === this.filterLevel());
    if (this.filterStatus()) result = result.filter(j => j.status === this.filterStatus());
    if (this.filterGrade())  result = result.filter(j => j.gradeId === this.filterGrade());
    return result;
  });

  constructor() { effect(() => { this.session.tenantId(); this.load(); }); }

  canAdd() { return this.session.hasTenant() && !!this.form.code && !!this.form.titleEn && !!this.form.levelId; }
  clearFilters() { this.filterFamily.set(''); this.filterLevel.set(''); this.filterStatus.set(''); this.filterGrade.set(''); }

  startEdit(j: Job) {
    this.editingId.set(j.id);
    this.editForm = { code: j.code, titleEn: j.titleEn, titleAr: j.titleAr ?? '', levelId: j.levelId, jobFamilyId: j.jobFamilyId };
  }

  private async load() {
    if (!this.session.hasTenant()) { this.jobs.set([]); return; }
    this.loading.set(true); this.error.set(null);
    try {
      const [j, l, f, g] = await Promise.all([this.api.jobs(), this.api.levels(), this.api.families(), this.api.grades()]);
      this.jobs.set(j); this.levels.set(l); this.families.set(f); this.grades.set(g);
    } catch (e: any) { this.error.set(e?.error?.detail ?? 'Failed to load jobs.'); }
    finally { this.loading.set(false); }
  }

  async add() {
    try {
      await this.api.createJob({
        code: this.form.code, titleEn: this.form.titleEn, titleAr: this.form.titleAr || null,
        levelId: this.form.levelId, descriptionEn: null, descriptionAr: null, jobFamilyId: this.form.jobFamilyId,
      });
      this.form = { code: '', titleEn: '', titleAr: '', levelId: '', jobFamilyId: null };
      this.showForm.set(false);
      this.toast.success(this.i18n.t('toast.created'));
      await this.load();
    } catch (e: any) { this.toast.error(e?.error?.detail ?? 'Failed to create job.'); }
  }

  async saveEdit(id: string) {
    this.saving.set(true); this.error.set(null);
    try {
      await this.api.updateJob(id, {
        code: this.editForm.code, titleEn: this.editForm.titleEn, titleAr: this.editForm.titleAr || null,
        levelId: this.editForm.levelId, descriptionEn: null, descriptionAr: null, jobFamilyId: this.editForm.jobFamilyId,
      });
      this.editingId.set(null);
      this.toast.success(this.i18n.t('toast.saved'));
      await this.load();
    } catch (e: any) { this.toast.error(e?.error?.detail ?? 'Failed to update job.'); }
    finally { this.saving.set(false); }
  }

  async deleteJob(id: string) {
    const ok = await this.cs.confirm({
      title: this.i18n.t('confirm.job.title'),
      body: this.i18n.t('confirm.job.body'),
      confirmLabel: this.i18n.t('confirm.delete'),
      danger: true,
    });
    if (!ok) return;
    try {
      await this.api.deleteJob(id);
      this.toast.success(this.i18n.t('toast.deleted'));
      await this.load();
    } catch (e: any) { this.toast.error(e?.error?.detail ?? 'Failed to delete job.'); }
  }
}
