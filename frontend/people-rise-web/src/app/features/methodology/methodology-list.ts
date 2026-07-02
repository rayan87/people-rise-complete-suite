import { Component, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Api } from '../../core/api';
import { Session } from '../../core/session';
import { I18n } from '../../core/i18n';
import { ToastService } from '../../core/toast';
import { ConfirmService } from '../../core/confirm';
import { Methodology } from '../../core/models';

@Component({
  selector: 'pr-methodology-list',
  imports: [RouterLink, FormsModule],
  styles: [`
    .mcard {
      background: var(--surface); border: 1px solid var(--border);
      border-radius: var(--radius-lg); padding: 1rem 1.25rem;
      margin-bottom: .75rem; display: flex; align-items: center; gap: 1rem;
      text-decoration: none; transition: border-color .12s;
    }
    .mcard:hover { border-color: var(--primary); text-decoration: none; }
    .mcard-body { flex: 1; min-width: 0; }
    .mcard-name { font-weight: 500; color: var(--text); font-size: 1rem; }
    .mcard-meta { font-size: .88rem; color: var(--text-faint); margin-top: 2px; }
    .mcard-arrow { color: var(--text-faint); font-size: 18px; }
    .mcard-del { margin-inline-start: auto; }
    .version-chips { display: flex; gap: 6px; flex-wrap: wrap; margin-top: 6px; }
    .ver-chip {
      font-size: .8rem; padding: 1px 8px; border-radius: 999px;
      border: 1px solid var(--border); background: var(--surface-2); color: var(--text-muted);
    }
    .ver-chip.active { background: var(--success-soft); color: var(--success); border-color: transparent; }
    .ver-chip.draft { background: var(--surface-2); color: var(--text-muted); }
    .ver-chip.retired { background: var(--danger-soft); color: var(--danger); border-color: transparent; }
  `],
  template: `
    <div class="head">
      <div><h1>Methodologies</h1></div>
      <div class="spacer"></div>
      <button (click)="showForm.set(!showForm())">
        {{ showForm() ? i18n.t('common.cancel') : '+ New methodology' }}
      </button>
    </div>

    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (!session.hasTenant()) { <div class="alert info">{{ i18n.t('common.selectClient') }}</div> }

    @if (showForm()) {
      <div class="card">
        <h2>New methodology</h2>
        <div class="row">
          <div class="field">
            <label>{{ i18n.t('common.code') }}</label>
            <input [(ngModel)]="nm.code" placeholder="ELD-PF" />
          </div>
          <div class="field">
            <label>{{ i18n.t('common.nameEn') }}</label>
            <input [(ngModel)]="nm.nameEn" />
          </div>
          <div class="field">
            <label>{{ i18n.t('common.nameAr') }} <span class="optional">({{ i18n.t('common.optional') }})</span></label>
            <input [(ngModel)]="nm.nameAr" dir="rtl" />
          </div>
          <div style="align-self:flex-end">
            <button (click)="create()" [disabled]="saving() || !session.hasTenant() || !nm.code || !nm.nameEn">
              {{ i18n.t('common.create') }}
            </button>
          </div>
        </div>
      </div>
    }

    <div class="card">
      @if (loading()) {
        <p class="muted">{{ i18n.t('common.loading') }}</p>
      } @else if (methodologies().length === 0) {
        <p class="empty">No methodologies yet. Create one to get started.</p>
      } @else {
        @for (m of methodologies(); track m.id) {
          <a class="mcard" [routerLink]="['/methodology', m.id]">
            <div class="mcard-body">
              <div class="mcard-name">{{ i18n.name(m.nameEn, m.nameAr) }}</div>
              <div class="mcard-meta">{{ m.code }} · {{ m.versions.length }} version{{ m.versions.length !== 1 ? 's' : '' }}</div>
              @if (m.versions.length > 0) {
                <div class="version-chips">
                  @for (v of m.versions; track v.id) {
                    <span class="ver-chip {{ v.status.toLowerCase() }}">v{{ v.versionNo }} · {{ v.status }}</span>
                  }
                </div>
              }
            </div>
            <i class="ti ti-chevron-right mcard-arrow"></i>
            @if (canDelete(m)) {
              <button class="sm danger icon mcard-del" (click)="onDelete(m, $event)" [disabled]="saving()" title="Delete methodology">
                <i class="ti ti-trash"></i>
              </button>
            }
          </a>
        }
      }
    </div>
  `,
})
export class MethodologyList {
  private api = inject(Api);
  private toast = inject(ToastService);
  private cs = inject(ConfirmService);
  readonly session = inject(Session);
  readonly i18n = inject(I18n);
  readonly methodologies = signal<Methodology[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly showForm = signal(false);
  nm = { code: '', nameEn: '', nameAr: '' };

  constructor() { effect(() => { this.session.tenantId(); this.load(); }); }

  private async load() {
    if (!this.session.hasTenant()) { this.methodologies.set([]); return; }
    this.loading.set(true); this.error.set(null);
    try { this.methodologies.set(await this.api.methodologies()); }
    catch (e: any) { this.error.set(e?.error?.detail ?? 'Failed to load methodologies.'); }
    finally { this.loading.set(false); }
  }

  canDelete(m: Methodology): boolean {
    return !m.versions.some(v => v.status === 'Active' || v.status === 'Retired');
  }

  async onDelete(m: Methodology, e: Event) {
    e.preventDefault(); e.stopPropagation();
    const ok = await this.cs.confirm({
      title: `Delete "${this.i18n.name(m.nameEn, m.nameAr)}"?`,
      body: 'This will permanently remove the methodology and all its draft versions.',
      confirmLabel: 'Delete',
      danger: true,
    });
    if (!ok) return;
    this.saving.set(true);
    try {
      await this.api.deleteMethodology(m.id);
      this.toast.success('Methodology deleted.');
      await this.load();
    } catch (e: any) { this.toast.error(e?.error?.detail ?? 'Failed to delete methodology.'); }
    finally { this.saving.set(false); }
  }

  async create() {
    this.saving.set(true); this.error.set(null);
    try {
      await this.api.createMethodology({ code: this.nm.code, nameEn: this.nm.nameEn, nameAr: this.nm.nameAr || null });
      this.nm = { code: '', nameEn: '', nameAr: '' };
      this.showForm.set(false);
      await this.load();
    } catch (e: any) { this.error.set(e?.error?.detail ?? 'Failed to create methodology.'); }
    finally { this.saving.set(false); }
  }
}
