import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../../core/api';
import { Session } from '../../core/session';
import { I18n } from '../../core/i18n';
import { Level, Grade, JobFamily } from '../../core/models';

type Tab = 'levels' | 'grades' | 'families';
type PanelType = 'level' | 'grade' | 'family';

@Component({
  selector: 'pr-settings-structure',
  imports: [FormsModule],
  styles: [`
    .tabs { display: flex; gap: 0; border-bottom: 1px solid var(--border); margin-bottom: 1.25rem; }
    .tab {
      padding: .6rem 1.1rem; font-size: .85rem; font-weight: 500;
      color: var(--text-muted); border: none; background: none;
      border-bottom: 2px solid transparent; margin-bottom: -1px;
      cursor: pointer; transition: color .1s;
    }
    .tab:hover { color: var(--text); background: transparent; }
    .tab.active { color: var(--primary); border-bottom-color: var(--primary); }
    .split { display: flex; gap: 1rem; align-items: flex-start; }
    .list-col { flex: 1; min-width: 0; }
    .panel {
      width: 300px; flex-shrink: 0;
      border: 1px solid var(--border); border-radius: 8px;
      background: var(--surface-2); padding: 1rem 1.125rem;
    }
    .panel-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 1rem; }
    .panel-head h2 { margin: 0; font-size: 1rem; }
    .panel-actions { display: flex; gap: .5rem; margin-top: 1rem; }
    .list-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: .75rem; }
    .list-head h2 { margin: 0; }
    td .act { display: flex; gap: 4px; justify-content: flex-end; }
  `],
  template: `
    <div class="head">
      <div>
        <div class="faint" style="font-size:.82rem">Settings</div>
        <h1>Structure</h1>
      </div>
    </div>

    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (!session.hasTenant()) { <div class="alert info">{{ i18n.t('common.selectClient') }}</div> }

    <div class="card">
      <div class="tabs">
        <button class="tab" [class.active]="tab() === 'levels'" (click)="switchTab('levels')">
          Levels <span class="faint">({{ levels().length }})</span>
        </button>
        <button class="tab" [class.active]="tab() === 'families'" (click)="switchTab('families')">
          Job families <span class="faint">({{ families().length }})</span>
        </button>
        <button class="tab" [class.active]="tab() === 'grades'" (click)="switchTab('grades')">
          Grades <span class="faint">({{ grades().length }})</span>
        </button>
      </div>

      <div [class.split]="panel()">
        <div class="list-col">

          @if (tab() === 'levels') {
            <div class="list-head">
              <h2>Levels</h2>
              <button class="sm" (click)="openNew('level')"><i class="ti ti-plus"></i> Add level</button>
            </div>
            @if (levels().length === 0) {
              <p class="empty">No levels yet.</p>
            } @else {
              <table>
                <thead>
                  <tr>
                    <th>Rank</th><th>Code</th><th>Name</th><th>Eval scope</th><th></th>
                  </tr>
                </thead>
                <tbody>
                  @for (l of levels(); track l.id) {
                    <tr>
                      <td class="faint">{{ l.rank }}</td>
                      <td>{{ l.code }}</td>
                      <td>{{ i18n.name(l.nameEn, l.nameAr) }}</td>
                      <td>
                        @if (l.inEvalScope) { <span class="badge approved">Yes</span> }
                        @else { <span class="badge draft">No</span> }
                      </td>
                      <td>
                        <div class="act">
                          <button class="sm subtle" (click)="openEdit('level', l.id)"><i class="ti ti-pencil"></i></button>
                          <button class="sm subtle" style="color:var(--danger)" (click)="deleteLevel(l.id)"><i class="ti ti-trash"></i></button>
                        </div>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            }
          }

          @if (tab() === 'families') {
            <div class="list-head">
              <h2>Job families</h2>
              <button class="sm" (click)="openNew('family')"><i class="ti ti-plus"></i> Add family</button>
            </div>
            @if (families().length === 0) {
              <p class="empty">No job families yet.</p>
            } @else {
              <table>
                <thead>
                  <tr><th>Code</th><th>Name</th><th></th></tr>
                </thead>
                <tbody>
                  @for (f of families(); track f.id) {
                    <tr>
                      <td>{{ f.code }}</td>
                      <td>{{ i18n.name(f.nameEn, f.nameAr) }}</td>
                      <td>
                        <div class="act">
                          <button class="sm subtle" (click)="openEdit('family', f.id)"><i class="ti ti-pencil"></i></button>
                          <button class="sm subtle" style="color:var(--danger)" (click)="deleteFamily(f.id)"><i class="ti ti-trash"></i></button>
                        </div>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            }
          }

          @if (tab() === 'grades') {
            <div class="list-head">
              <h2>Grades</h2>
              <button class="sm" (click)="openNew('grade')"><i class="ti ti-plus"></i> Add grade</button>
            </div>
            @if (grades().length === 0) {
              <p class="empty">No grades yet.</p>
            } @else {
              <table>
                <thead>
                  <tr><th>Rank</th><th>Code</th><th>Name</th><th>Level</th><th></th></tr>
                </thead>
                <tbody>
                  @for (g of grades(); track g.id) {
                    <tr>
                      <td class="faint">{{ g.rank }}</td>
                      <td>{{ g.code }}</td>
                      <td>{{ i18n.name(g.nameEn, g.nameAr) }}</td>
                      <td class="faint">{{ g.levelCode ?? '—' }}</td>
                      <td>
                        <div class="act">
                          <button class="sm subtle" (click)="openEdit('grade', g.id)"><i class="ti ti-pencil"></i></button>
                          <button class="sm subtle" style="color:var(--danger)" (click)="deleteGrade(g.id)"><i class="ti ti-trash"></i></button>
                        </div>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            }
          }

        </div>

        @if (panel()) {
          <div class="panel">
            <div class="panel-head">
              <h2>{{ panelTitle() }}</h2>
              <button class="icon subtle" (click)="closePanel()"><i class="ti ti-x"></i></button>
            </div>

            @if (panelError()) {
              <div class="alert error" style="margin-bottom:.75rem">{{ panelError() }}</div>
            }

            @if (panel() === 'level') {
              <div class="field"><label>Code</label><input [(ngModel)]="levelForm.code" placeholder="IC" /></div>
              <div class="field">
                <label>{{ i18n.t('common.nameEn') }}</label>
                <input [(ngModel)]="levelForm.nameEn" />
              </div>
              <div class="field">
                <label>{{ i18n.t('common.nameAr') }} <span class="optional">({{ i18n.t('common.optional') }})</span></label>
                <input [(ngModel)]="levelForm.nameAr" dir="rtl" />
              </div>
              <div class="row">
                <div class="field">
                  <label>{{ i18n.t('field.rank') }}</label>
                  <input type="number" [(ngModel)]="levelForm.rank" min="1" />
                </div>
                <div class="field">
                  <label>{{ i18n.t('field.inScope') }}</label>
                  <select [(ngModel)]="levelForm.inEvalScope">
                    <option [ngValue]="true">Yes</option>
                    <option [ngValue]="false">No</option>
                  </select>
                </div>
              </div>
            }

            @if (panel() === 'family') {
              <div class="field"><label>Code</label><input [(ngModel)]="familyForm.code" placeholder="ENG" /></div>
              <div class="field">
                <label>{{ i18n.t('common.nameEn') }}</label>
                <input [(ngModel)]="familyForm.nameEn" />
              </div>
              <div class="field">
                <label>{{ i18n.t('common.nameAr') }} <span class="optional">({{ i18n.t('common.optional') }})</span></label>
                <input [(ngModel)]="familyForm.nameAr" dir="rtl" />
              </div>
            }

            @if (panel() === 'grade') {
              <div class="field"><label>Code</label><input [(ngModel)]="gradeForm.code" placeholder="G8" /></div>
              <div class="field">
                <label>{{ i18n.t('common.nameEn') }}</label>
                <input [(ngModel)]="gradeForm.nameEn" />
              </div>
              <div class="field">
                <label>{{ i18n.t('common.nameAr') }} <span class="optional">({{ i18n.t('common.optional') }})</span></label>
                <input [(ngModel)]="gradeForm.nameAr" dir="rtl" />
              </div>
              <div class="row">
                <div class="field">
                  <label>{{ i18n.t('field.rank') }}</label>
                  <input type="number" [(ngModel)]="gradeForm.rank" min="1" />
                </div>
                <div class="field">
                  <label>{{ i18n.t('field.level') }} <span class="optional">({{ i18n.t('common.optional') }})</span></label>
                  <select [(ngModel)]="gradeForm.levelId">
                    <option [ngValue]="null">—</option>
                    @for (l of levels(); track l.id) {
                      <option [ngValue]="l.id">{{ l.code }} — {{ i18n.name(l.nameEn, l.nameAr) }}</option>
                    }
                  </select>
                </div>
              </div>
            }

            <div class="panel-actions">
              <button (click)="save()" [disabled]="saving() || !canSave()">
                {{ editingId() ? i18n.t('common.save') : i18n.t('common.create') }}
              </button>
              <button class="subtle" (click)="closePanel()">{{ i18n.t('common.cancel') }}</button>
            </div>
          </div>
        }
      </div>
    </div>
  `,
})
export class SettingsStructure {
  private api = inject(Api);
  readonly session = inject(Session);
  readonly i18n = inject(I18n);

  readonly levels  = signal<Level[]>([]);
  readonly grades  = signal<Grade[]>([]);
  readonly families = signal<JobFamily[]>([]);
  readonly error    = signal<string | null>(null);
  readonly panelError = signal<string | null>(null);
  readonly saving  = signal(false);
  readonly tab     = signal<Tab>('levels');
  readonly panel   = signal<PanelType | null>(null);
  readonly editingId = signal<string | null>(null);

  levelForm  = { code: '', nameEn: '', nameAr: '', rank: 1, inEvalScope: true };
  gradeForm  = { code: '', nameEn: '', nameAr: '', rank: 1, levelId: null as string | null };
  familyForm = { code: '', nameEn: '', nameAr: '' };

  readonly panelTitle = computed(() => {
    const p = this.panel();
    if (!p) return '';
    const name = p === 'level' ? 'level' : p === 'grade' ? 'grade' : 'family';
    return this.editingId() ? `Edit ${name}` : `New ${name}`;
  });

  constructor() { effect(() => { this.session.tenantId(); this.load(); }); }

  private async load() {
    if (!this.session.hasTenant()) { this.levels.set([]); this.grades.set([]); this.families.set([]); return; }
    this.error.set(null);
    try {
      const [l, g, f] = await Promise.all([this.api.levels(), this.api.grades(), this.api.families()]);
      this.levels.set(l); this.grades.set(g); this.families.set(f);
    } catch (e: any) { this.error.set(e?.error?.detail ?? 'Failed to load.'); }
  }

  switchTab(t: Tab) { this.tab.set(t); this.closePanel(); }

  openNew(type: PanelType) {
    this.panelError.set(null);
    this.editingId.set(null);
    if (type === 'level')  this.levelForm  = { code: '', nameEn: '', nameAr: '', rank: 1, inEvalScope: true };
    if (type === 'grade')  this.gradeForm  = { code: '', nameEn: '', nameAr: '', rank: 1, levelId: null };
    if (type === 'family') this.familyForm = { code: '', nameEn: '', nameAr: '' };
    this.panel.set(type);
  }

  openEdit(type: PanelType, id: string) {
    this.panelError.set(null);
    this.editingId.set(id);
    if (type === 'level') {
      const l = this.levels().find(x => x.id === id)!;
      this.levelForm = { code: l.code, nameEn: l.nameEn, nameAr: l.nameAr ?? '', rank: l.rank, inEvalScope: l.inEvalScope };
    }
    if (type === 'grade') {
      const g = this.grades().find(x => x.id === id)!;
      this.gradeForm = { code: g.code, nameEn: g.nameEn, nameAr: g.nameAr ?? '', rank: g.rank, levelId: g.levelId };
    }
    if (type === 'family') {
      const f = this.families().find(x => x.id === id)!;
      this.familyForm = { code: f.code, nameEn: f.nameEn, nameAr: f.nameAr ?? '' };
    }
    this.panel.set(type);
  }

  closePanel() { this.panel.set(null); this.editingId.set(null); this.panelError.set(null); }

  canSave(): boolean {
    const p = this.panel();
    if (p === 'level')  return !!this.levelForm.code  && !!this.levelForm.nameEn;
    if (p === 'grade')  return !!this.gradeForm.code  && !!this.gradeForm.nameEn;
    if (p === 'family') return !!this.familyForm.code && !!this.familyForm.nameEn;
    return false;
  }

  async save() {
    this.saving.set(true); this.panelError.set(null);
    const id = this.editingId();
    try {
      const p = this.panel();
      if (p === 'level') {
        const b = { code: this.levelForm.code, nameEn: this.levelForm.nameEn, nameAr: this.levelForm.nameAr || null, rank: this.levelForm.rank, inEvalScope: this.levelForm.inEvalScope };
        if (id) await this.api.updateLevel(id, b); else await this.api.createLevel(b);
      } else if (p === 'grade') {
        const b = { code: this.gradeForm.code, nameEn: this.gradeForm.nameEn, nameAr: this.gradeForm.nameAr || null, rank: this.gradeForm.rank, levelId: this.gradeForm.levelId };
        if (id) await this.api.updateGrade(id, b); else await this.api.createGrade(b);
      } else if (p === 'family') {
        const b = { code: this.familyForm.code, nameEn: this.familyForm.nameEn, nameAr: this.familyForm.nameAr || null };
        if (id) await this.api.updateFamily(id, b); else await this.api.createFamily(b);
      }
      this.closePanel();
      await this.load();
    } catch (e: any) { this.panelError.set(e?.error?.detail ?? 'Failed to save.'); }
    finally { this.saving.set(false); }
  }

  async deleteLevel(id: string) {
    if (!confirm('Delete this level?')) return;
    this.error.set(null);
    try { await this.api.deleteLevel(id); await this.load(); }
    catch (e: any) { this.error.set(e?.error?.detail ?? 'Failed to delete level.'); }
  }

  async deleteGrade(id: string) {
    if (!confirm('Delete this grade?')) return;
    this.error.set(null);
    try { await this.api.deleteGrade(id); await this.load(); }
    catch (e: any) { this.error.set(e?.error?.detail ?? 'Failed to delete grade.'); }
  }

  async deleteFamily(id: string) {
    if (!confirm('Delete this family?')) return;
    this.error.set(null);
    try { await this.api.deleteFamily(id); await this.load(); }
    catch (e: any) { this.error.set(e?.error?.detail ?? 'Failed to delete family.'); }
  }
}
