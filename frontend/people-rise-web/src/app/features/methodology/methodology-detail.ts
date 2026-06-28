import { Component, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Api } from '../../core/api';
import { Session } from '../../core/session';
import { I18n } from '../../core/i18n';
import { ToastService } from '../../core/toast';
import { Methodology } from '../../core/models';

@Component({
  selector: 'pr-methodology-detail',
  imports: [RouterLink, FormsModule, DatePipe],
  styles: [`
    .ver-row {
      display: flex; align-items: center; gap: 1rem;
      padding: .7rem .75rem; border-bottom: 1px solid var(--border);
    }
    .ver-row:last-child { border-bottom: none; }
    .ver-num { font-weight: 500; color: var(--text); min-width: 40px; }
    .ver-note { font-size: .82rem; color: var(--text-muted); flex: 1; }
    .ver-date { font-size: .78rem; color: var(--text-faint); }
    .ver-actions { display: flex; gap: 6px; }
  `],
  template: `
    <div class="head">
      <div>
        <a routerLink="/methodology" class="faint" style="font-size:.82rem">
          <i class="ti ti-arrow-left" style="font-size:12px"></i> Methodologies
        </a>
        <h1 style="margin-top:.25rem">{{ methodology()?.nameEn ?? '…' }}</h1>
        <div class="faint" style="margin-top:2px">{{ methodology()?.code }}</div>
      </div>
      <div class="spacer"></div>
      <button class="subtle" (click)="editMode.set(!editMode())">
        <i class="ti ti-pencil"></i> Edit name
      </button>
    </div>

    @if (error()) { <div class="alert error">{{ error() }}</div> }

    @if (editMode()) {
      <div class="card">
        <h2>Edit methodology</h2>
        <div class="row">
          <div class="field">
            <label>{{ i18n.t('common.nameEn') }}</label>
            <input [(ngModel)]="editForm.nameEn" />
          </div>
          <div class="field">
            <label>{{ i18n.t('common.nameAr') }} <span class="optional">({{ i18n.t('common.optional') }})</span></label>
            <input [(ngModel)]="editForm.nameAr" dir="rtl" />
          </div>
          <div style="align-self:flex-end; display:flex; gap:.5rem">
            <button (click)="saveEdit()" [disabled]="saving()">Save</button>
            <button class="subtle" (click)="editMode.set(false)">Cancel</button>
          </div>
        </div>
      </div>
    }

    <div class="card">
      <div class="card-header">
        <span class="card-title">Versions</span>
        <button class="sm" (click)="newVersion()" [disabled]="saving()">
          <i class="ti ti-plus"></i> New draft version
        </button>
      </div>

      @if (loading()) {
        <p class="muted">{{ i18n.t('common.loading') }}</p>
      } @else if (!methodology()?.versions?.length) {
        <p class="empty">No versions yet. Create the first draft version.</p>
      } @else {
        @for (v of methodology()!.versions; track v.id) {
          <div class="ver-row">
            <div class="ver-num">v{{ v.versionNo }}</div>
            <span class="badge {{ v.status.toLowerCase() }}">{{ v.status }}</span>
            @if (v.note) { <div class="ver-note">{{ v.note }}</div> }
            @else { <div class="ver-note faint">—</div> }
            @if (v.publishedAt) {
              <div class="ver-date">{{ v.publishedAt | date:'d MMM yyyy' }}</div>
            }
            <div class="ver-actions">
              <a [routerLink]="['/methodology/versions', v.id]">
                <button class="sm subtle">Open editor →</button>
              </a>
            </div>
          </div>
        }
      }
    </div>
  `,
})
export class MethodologyDetail {
  private api = inject(Api);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toast = inject(ToastService);
  readonly session = inject(Session);
  readonly i18n = inject(I18n);
  readonly methodology = signal<Methodology | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly editMode = signal(false);
  editForm = { nameEn: '', nameAr: '' };

  constructor() {
    effect(() => {
      const id = this.route.snapshot.paramMap.get('id');
      if (id) this.load(id);
    });
  }

  private async load(id: string) {
    this.loading.set(true); this.error.set(null);
    try {
      const list = await this.api.methodologies();
      const m = list.find(x => x.id === id) ?? null;
      this.methodology.set(m);
      if (m) { this.editForm = { nameEn: m.nameEn, nameAr: m.nameAr ?? '' }; }
    } catch (e: any) { this.error.set(e?.error?.detail ?? 'Failed to load.'); }
    finally { this.loading.set(false); }
  }

  async saveEdit() {
    const m = this.methodology();
    if (!m) return;
    this.saving.set(true); this.error.set(null);
    try {
      await this.api.updateMethodology(m.id, { nameEn: this.editForm.nameEn, nameAr: this.editForm.nameAr || null });
      this.editMode.set(false);
      this.toast.success(this.i18n.t('toast.saved'));
      await this.load(m.id);
    } catch (e: any) { this.toast.error(e?.error?.detail ?? 'Failed to save.'); }
    finally { this.saving.set(false); }
  }

  async newVersion() {
    const m = this.methodology();
    if (!m) return;
    this.saving.set(true); this.error.set(null);
    try {
      const v = await this.api.createVersion(m.id, { note: null });
      this.router.navigate(['/methodology/versions', v.id]);
    } catch (e: any) {
      this.error.set(e?.error?.detail ?? 'Failed to create version.');
      this.saving.set(false);
    }
  }
}
