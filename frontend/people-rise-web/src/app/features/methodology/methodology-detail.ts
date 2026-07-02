import { Component, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Api } from '../../core/api';
import { Session } from '../../core/session';
import { I18n } from '../../core/i18n';
import { ToastService } from '../../core/toast';
import { ConfirmService } from '../../core/confirm';
import { Methodology } from '../../core/models';

@Component({
  selector: 'pr-methodology-detail',
  imports: [RouterLink, FormsModule, DatePipe],
  styles: [`
    .info-icon {
      width: 44px; height: 44px; border-radius: 10px;
      background: var(--primary-soft);
      display: flex; align-items: center; justify-content: center; flex-shrink: 0;
    }
    .info-icon i { font-size: 22px; color: var(--primary); }
    .info-name { font-size: 1.1rem; font-weight: 500; color: var(--text); }
    .info-name-ar { font-size: .9rem; color: var(--text-muted); direction: rtl; margin-top: 2px; }
    .info-meta { font-size: .82rem; color: var(--text-faint); margin-top: 5px; }
    .ver-card {
      background: var(--surface); border: 1px solid var(--border);
      border-radius: var(--radius-lg); padding: .85rem 1rem;
      margin-bottom: .6rem; display: flex; align-items: center; gap: 1rem;
    }
    .ver-card.active-ver {
      border-inline-start-width: 3px; border-inline-start-color: var(--success);
      border-color: var(--border-strong);
    }
    .ver-card.retired-ver { opacity: .55; }
    .ver-icon {
      width: 44px; height: 44px; border-radius: 10px;
      display: flex; align-items: center; justify-content: center;
      font-size: .88rem; font-weight: 500; flex-shrink: 0;
    }
    .ver-icon.draft-icon  { background: var(--surface-2); color: var(--text-muted); }
    .ver-icon.active-icon { background: var(--success-soft); color: var(--success); }
    .ver-icon.retired-icon{ background: var(--danger-soft); color: var(--danger); }
    .sec-head {
      display: flex; align-items: center; justify-content: space-between;
      margin-bottom: .6rem;
    }
    .sec-title { font-size: .9rem; font-weight: 500; color: var(--text); }
  `],
  template: `
    <a routerLink="/methodology" class="faint"
       style="font-size:.82rem; display:inline-flex; align-items:center; gap:4px; margin-bottom:.75rem">
      <i class="ti ti-arrow-left" style="font-size:12px"></i> Methodologies
    </a>

    @if (error()) { <div class="alert error">{{ error() }}</div> }

    <!-- Methodology info card -->
    <div class="card" style="margin-bottom:1.25rem">
      <div style="display:flex; align-items:flex-start; gap:1rem">
        <div class="info-icon"><i class="ti ti-file-analytics" aria-hidden="true"></i></div>
        <div style="flex:1; min-width:0">
          @if (!editMode()) {
            <div class="info-name">{{ methodology()?.nameEn ?? '…' }}</div>
            @if (methodology()?.nameAr) {
              <div class="info-name-ar">{{ methodology()?.nameAr }}</div>
            }
            <div class="info-meta">
              {{ methodology()?.code }}
              @if (methodology()?.versions?.length) {
                · {{ methodology()!.versions.length }}
                version{{ methodology()!.versions.length !== 1 ? 's' : '' }}
              }
            </div>
          } @else {
            <div class="row" style="margin-bottom:0; flex-wrap:wrap">
              <div class="field" style="margin-bottom:0">
                <label>{{ i18n.t('common.nameEn') }}</label>
                <input [(ngModel)]="editForm.nameEn" />
              </div>
              <div class="field" style="margin-bottom:0">
                <label>{{ i18n.t('common.nameAr') }}
                  <span class="optional">({{ i18n.t('common.optional') }})</span>
                </label>
                <input [(ngModel)]="editForm.nameAr" dir="rtl" />
              </div>
              <div style="align-self:flex-end; display:flex; gap:.5rem">
                <button (click)="saveEdit()" [disabled]="saving()">Save</button>
                <button class="subtle" (click)="editMode.set(false)">Cancel</button>
              </div>
            </div>
          }
        </div>
        @if (!editMode()) {
          <button class="subtle sm" (click)="editMode.set(true)">
            <i class="ti ti-pencil"></i> Edit name
          </button>
        }
      </div>
    </div>

    <!-- Version history -->
    <div class="sec-head">
      <span class="sec-title">Version history</span>
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
        <div class="ver-card"
             [class.active-ver]="v.status === 'Active'"
             [class.retired-ver]="v.status === 'Retired'">
          <div class="ver-icon"
               [class.draft-icon]="v.status === 'Draft'"
               [class.active-icon]="v.status === 'Active'"
               [class.retired-icon]="v.status === 'Retired'">
            v{{ v.versionNo }}
          </div>
          <div style="flex:1; min-width:0">
            <div style="display:flex; align-items:center; gap:.5rem; margin-bottom:3px">
              <span class="badge {{ v.status.toLowerCase() }}">{{ v.status }}</span>
              @if (v.publishedAt) {
                <span style="font-size:.78rem; color:var(--text-faint)">
                  {{ v.publishedAt | date:'d MMM yyyy' }}
                </span>
              }
            </div>
            @if (v.note) {
              <div style="font-size:.82rem; color:var(--text-muted)">{{ v.note }}</div>
            } @else {
              <div style="font-size:.82rem; color:var(--text-faint)">No notes</div>
            }
          </div>
          <div style="display:flex; gap:.5rem; align-items:center">
            @if (v.status === 'Draft') {
              <a [routerLink]="['/methodology/versions', v.id]">
                <button class="sm"><i class="ti ti-edit"></i> Open editor</button>
              </a>
              <button class="sm danger icon" (click)="deleteVersion(v.id, v.versionNo)"
                      [disabled]="saving()" title="Delete version">
                <i class="ti ti-trash"></i>
              </button>
            } @else {
              <a [routerLink]="['/methodology/versions', v.id]">
                <button class="sm subtle"><i class="ti ti-eye"></i> View</button>
              </a>
            }
          </div>
        </div>
      }
    }
  `,
})
export class MethodologyDetail {
  private api = inject(Api);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toast = inject(ToastService);
  private cs = inject(ConfirmService);
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
      if (m) this.editForm = { nameEn: m.nameEn, nameAr: m.nameAr ?? '' };
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

  async deleteVersion(id: string, versionNo: number) {
    const ok = await this.cs.confirm({
      title: `Delete draft version v${versionNo}?`,
      body: 'This will permanently remove the version and all its factors, questions, and grade mappings.',
      confirmLabel: 'Delete',
      danger: true,
    });
    if (!ok) return;
    this.saving.set(true);
    try {
      await this.api.deleteVersion(id);
      this.toast.success('Version deleted.');
      const m = this.methodology();
      if (m) await this.load(m.id);
    } catch (e: any) { this.toast.error(e?.error?.detail ?? 'Failed to delete version.'); }
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
