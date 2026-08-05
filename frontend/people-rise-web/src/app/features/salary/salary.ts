import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { Api } from '../../core/api';
import { Session } from '../../core/session';
import { I18n } from '../../core/i18n';
import { ToastService } from '../../core/toast';
import { ConfirmService } from '../../core/confirm';
import { SalaryBandRow } from '../../core/models';

@Component({
  selector: 'pr-salary',
  imports: [FormsModule, DecimalPipe],
  template: `
    <h1>{{ i18n.t('salary.title') }}</h1>
    <p>{{ i18n.t('salary.subtitle') }}</p>

    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (!session.hasTenant()) { <div class="alert info">{{ i18n.t('common.selectClient') }}</div> }

    <div class="card">
      <h2>{{ i18n.t('salary.generate') }}</h2>
      <p>{{ i18n.t('salary.generateHint') }}</p>
      <div class="row">
        <div class="field"><label>{{ i18n.t('salary.base') }}</label><input type="number" [(ngModel)]="gen.baseMidpoint" /></div>
        <div class="field"><label>{{ i18n.t('salary.progression') }}</label><input type="number" [(ngModel)]="gen.progressionPct" /></div>
        <div class="field"><label>{{ i18n.t('salary.currency') }}</label><input [(ngModel)]="gen.currency" /></div>
        <div class="field"><label>{{ i18n.t('salary.effective') }}</label><input type="date" [(ngModel)]="gen.effectiveDate" /></div>
      </div>
      <button (click)="generate()" [disabled]="!session.hasTenant() || busy()">{{ i18n.t('salary.run') }}</button>
    </div>

    <div class="card">
      <table>
        <thead><tr>
          <th>{{ i18n.t('field.grade') }}</th><th>{{ i18n.t('salary.min') }}</th><th>{{ i18n.t('salary.midpoint') }}</th>
          <th>{{ i18n.t('salary.max') }}</th><th>{{ i18n.t('salary.spread') }}</th><th>{{ i18n.t('salary.overlap') }}</th><th></th>
        </tr></thead>
        <tbody>
          @for (r of rows(); track r.gradeId) {
            <tr>
              <td><strong>{{ r.gradeCode }}</strong> <span class="faint">{{ i18n.name(r.gradeNameEn, r.gradeNameAr) }}</span></td>
              @if (editGrade() === r.gradeId) {
                <td colspan="6">
                  <div class="inline">
                    <input type="number" [ngModel]="form.midpoint" (ngModelChange)="form.midpoint = $event; onMidpointChange(r)"
                           style="max-width:120px" aria-label="midpoint" [title]="i18n.t('salary.midpoint')" />
                    <input type="number" [ngModel]="form.overlapPct" (ngModelChange)="form.overlapPct = $event; onOverlapChange(r)"
                           [disabled]="!canEditOverlap(r)" style="max-width:90px" aria-label="overlap" [title]="i18n.t('salary.overlap')" />
                    <input [(ngModel)]="form.currency" style="max-width:80px" aria-label="currency" />
                    <input type="date" [(ngModel)]="form.effectiveDate" style="max-width:150px" />
                    <button class="sm" (click)="save(r)">{{ i18n.t('common.save') }}</button>
                    <button class="sm subtle" (click)="editGrade.set(null)">{{ i18n.t('common.cancel') }}</button>
                  </div>
                </td>
              } @else if (r.band; as b) {
                <td>{{ b.minAmount | number:'1.0-0' }}</td>
                <td><strong>{{ b.midpoint | number:'1.0-0' }}</strong> {{ b.currency }}</td>
                <td>{{ b.maxAmount | number:'1.0-0' }}</td>
                <td>{{ b.spreadPct | number:'1.0-0' }}%</td>
                <td>@if (b.overlapPct === null) { <span class="faint">—</span> } @else { {{ b.overlapPct | number:'1.0-1' }}% }</td>
                <td class="right"><button class="sm subtle" (click)="edit(r)">{{ i18n.t('common.edit') }}</button></td>
              } @else {
                <td colspan="5" class="faint">{{ i18n.t('salary.noBand') }}</td>
                <td class="right"><button class="sm subtle" (click)="edit(r)">{{ i18n.t('salary.addBand') }}</button></td>
              }
            </tr>
          }
          @empty { <tr><td colspan="7" class="muted">{{ i18n.t('common.none') }}</td></tr> }
        </tbody>
      </table>
    </div>
  `,
  styles: [`.inline { display:flex; gap:.5rem; align-items:center; flex-wrap:wrap; } .right { text-align:end; }`],
})
export class Salary {
  private api = inject(Api);
  private toast = inject(ToastService);
  private cs = inject(ConfirmService);
  readonly session = inject(Session);
  readonly i18n = inject(I18n);
  readonly rows = signal<SalaryBandRow[]>([]);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);
  readonly editGrade = signal<string | null>(null);

  gen = { baseMidpoint: 8000, progressionPct: 25, currency: 'EGP', effectiveDate: '2026-01-01' };
  form: { midpoint: number; overlapPct: number | null; currency: string; effectiveDate: string; edited: 'midpoint' | 'overlap' } =
    { midpoint: 0, overlapPct: null, currency: 'EGP', effectiveDate: '2026-01-01', edited: 'midpoint' };

  constructor() { effect(() => { this.session.tenantId(); this.load(); }); }
  private fail(e: any) { this.toast.error(e?.error?.detail ?? this.i18n.t('err.generic')); }

  private async load() {
    if (!this.session.hasTenant()) { this.rows.set([]); return; }
    this.error.set(null);
    try { this.rows.set(await this.api.salaryBands()); }
    catch (e: any) { this.error.set(e?.error?.detail ?? this.i18n.t('err.loadSalaryBands')); }
  }

  /** The previous grade's (by rank) existing band midpoint, or null if this is the first grade
   * or that grade has no band yet — matches the backend's overlap-eligibility rule. */
  private previousMidpoint(r: SalaryBandRow): number | null {
    return this.rows().find(x => x.rank === r.rank - 1)?.band?.midpoint ?? null;
  }

  canEditOverlap(r: SalaryBandRow): boolean { return this.previousMidpoint(r) != null; }

  edit(r: SalaryBandRow) {
    const prev = this.previousMidpoint(r);
    this.form = {
      midpoint: r.band?.midpoint ?? this.gen.baseMidpoint,
      overlapPct: r.band?.overlapPct ?? (prev != null ? this.gen.progressionPct : null),
      currency: r.band?.currency ?? this.gen.currency,
      effectiveDate: (r.band?.effectiveDate ?? this.gen.effectiveDate).slice(0, 10),
      edited: 'midpoint',
    };
    this.editGrade.set(r.gradeId);
  }

  onMidpointChange(r: SalaryBandRow) {
    this.form.edited = 'midpoint';
    const prev = this.previousMidpoint(r);
    this.form.overlapPct = prev != null ? Math.round((this.form.midpoint / prev - 1) * 100 * 10000) / 10000 : null;
  }

  onOverlapChange(r: SalaryBandRow) {
    const prev = this.previousMidpoint(r);
    if (prev == null || this.form.overlapPct == null) return;
    this.form.edited = 'overlap';
    this.form.midpoint = prev * (1 + this.form.overlapPct / 100);
  }

  async save(r: SalaryBandRow) {
    try {
      const base = { currency: this.form.currency, effectiveDate: this.form.effectiveDate };
      const payload = this.form.edited === 'overlap' && this.form.overlapPct != null
        ? { ...base, overlapPct: this.form.overlapPct }
        : { ...base, midpoint: this.form.midpoint };
      if (r.band) await this.api.updateSalaryBand(r.band.id, payload);
      else await this.api.createSalaryBand({ gradeId: r.gradeId, ...payload });
      this.editGrade.set(null);
      this.toast.success(this.i18n.t('toast.saved'));
      await this.load();
    } catch (e) { this.fail(e); }
  }

  async generate() {
    const hasExisting = this.rows().some(r => r.band);
    if (hasExisting) {
      const ok = await this.cs.confirm({
        title: this.i18n.t('confirm.generate.title'),
        body: this.i18n.t('confirm.generate.body'),
        danger: true,
      });
      if (!ok) return;
    }
    this.busy.set(true); this.error.set(null);
    try {
      this.rows.set(await this.api.generateBands({ ...this.gen }));
      this.toast.success(this.i18n.t('toast.generated'));
    } catch (e) { this.fail(e); }
    finally { this.busy.set(false); }
  }
}
