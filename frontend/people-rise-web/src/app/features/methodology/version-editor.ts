import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Api } from '../../core/api';
import { I18n } from '../../core/i18n';
import { ToastService } from '../../core/toast';
import { ConfirmService } from '../../core/confirm';
import {
  MethodologyVersionDetail, FactorDetail, QuestionDetail,
  AnswerOption, Grade, GradeMapping, QuestionType,
} from '../../core/models';

@Component({
  selector: 'pr-version-editor',
  imports: [FormsModule, RouterLink],
  styles: [`
    .factor-block {
      border: 1px solid var(--border); border-radius: var(--radius-lg);
      overflow: hidden; margin-bottom: .5rem;
    }
    .factor-hd {
      display: flex; align-items: center; gap: .65rem;
      padding: .6rem .9rem; background: var(--surface);
      cursor: pointer; user-select: none; transition: background .1s;
    }
    .factor-hd:hover { background: var(--surface-2); }
    .factor-code {
      width: 32px; height: 32px; border-radius: 7px;
      background: var(--primary-soft); color: var(--primary);
      display: flex; align-items: center; justify-content: center;
      font-size: 10px; font-weight: 700; flex-shrink: 0; text-transform: uppercase;
    }
    .factor-hd-edit {
      padding: .6rem .9rem;
      background: var(--primary-soft); border-bottom: 1px solid var(--border);
    }
    .factor-body { background: var(--bg); }
    .q-row { padding: .6rem .9rem; border-top: 1px solid var(--border); }
    .q-num {
      width: 20px; height: 20px; border-radius: 50%;
      background: var(--surface-2); font-size: 10px; font-weight: 500; color: var(--text-muted);
      display: flex; align-items: center; justify-content: center; flex-shrink: 0; line-height: 1;
    }
    .ochip {
      display: inline-flex; align-items: center; gap: 5px;
      padding: 4px 8px 4px 10px; border: 1px solid var(--border);
      border-radius: 8px; background: var(--surface);
      font-size: .82rem; color: var(--text-muted);
    }
    .pts-badge {
      display: inline-flex; align-items: center; justify-content: center;
      min-width: 26px; height: 18px; border-radius: 4px;
      font-size: .75rem; font-weight: 500;
      background: var(--primary-soft); color: var(--primary); padding: 0 4px;
    }
    .icon-btn {
      background: none; border: none; cursor: pointer; padding: 0;
      color: var(--text-faint); font-size: .78rem; display: flex; line-height: 1;
    }
    .icon-btn:hover { color: var(--danger); }
    .option-edit-form {
      display: inline-flex; align-items: center; gap: .3rem;
      padding: 4px 8px; border: 1px solid var(--primary);
      border-radius: 8px; background: var(--primary-soft);
    }
    .option-edit-form input {
      padding: 3px 6px; font-size: .78rem;
      border: 1px solid var(--border); border-radius: 4px;
      background: var(--surface); color: var(--text); font-family: inherit;
    }
    .add-opts {
      display: flex; gap: .35rem; align-items: center; flex-wrap: wrap;
      padding-top: .4rem; padding-inline-start: 28px;
    }
    .add-opts input { padding: 3px 7px; font-size: .78rem; flex: 1; min-width: 80px; }
    .q-edit-form {
      flex: 1; background: var(--primary-soft); border: 1px solid var(--border);
      border-radius: 8px; padding: .45rem .6rem;
      display: flex; flex-direction: column; gap: .3rem;
    }
    .add-q-row {
      padding: .45rem .9rem; border-top: 1px dashed var(--border);
      display: flex; gap: .4rem; align-items: center; flex-wrap: wrap;
    }
    .add-q-row input { font-size: .82rem; }
    .add-factor-block {
      border: 1px dashed var(--border-strong); border-radius: var(--radius-lg);
      padding: .65rem .9rem; margin-bottom: 1rem;
    }
    .add-factor-block input { font-size: .82rem; }
    .gm-row {
      display: flex; align-items: center; gap: .6rem;
      padding: .5rem 0; border-bottom: 1px solid var(--border);
    }
    .gm-row:last-of-type { border-bottom: none; }
    .gm-bar {
      flex: 1; height: 8px; background: var(--primary-soft);
      border-radius: 4px; position: relative; overflow: hidden;
    }
    .gm-bar-fill {
      position: absolute; top: 0; bottom: 0;
      background: var(--primary); border-radius: 4px;
    }
    .add-map-row {
      padding-top: .55rem; margin-top: .5rem;
      border-top: 1px dashed var(--border);
      display: flex; gap: .4rem; align-items: center; flex-wrap: wrap;
    }
  `],
  template: `
    @if (error()) { <div class="alert error">{{ error() }}</div> }

    @if (v(); as ver) {
      <a [routerLink]="['/methodology', ver.methodologyId]" class="faint"
         style="font-size:.82rem; display:inline-flex; align-items:center; gap:4px; margin-bottom:.75rem">
        <i class="ti ti-arrow-left" style="font-size:12px"></i>
        {{ i18n.name(ver.methodologyNameEn, ver.methodologyNameAr) }}
      </a>

      <div class="head" style="margin-bottom:.75rem">
        <div>
          <h1>
            v{{ ver.versionNo }}
            <span style="font-size:1rem; font-weight:400; color:var(--text-muted)">
              · {{ i18n.name(ver.methodologyNameEn, ver.methodologyNameAr) }}
            </span>
          </h1>
          @if (ver.note) {
            <div style="font-size:.82rem; color:var(--text-faint); margin-top:2px">{{ ver.note }}</div>
          }
          <div style="margin-top:.35rem">
            <span class="badge {{ ver.status.toLowerCase() }}">{{ ver.status }}</span>
          </div>
        </div>
        <div class="spacer"></div>
        @if (isDraft()) {
          <button (click)="publish()">
            <i class="ti ti-send"></i> {{ i18n.t('eval.publish') }}
          </button>
        }
      </div>

      @if (isDraft()) {
        <div class="alert info" style="margin-bottom:1rem">{{ i18n.t('eval.draftHint') }}</div>
      } @else {
        <div class="alert info" style="margin-bottom:1rem">{{ i18n.t('eval.readonly') }}</div>
      }

      <!-- Factors -->
      <div style="font-size:.78rem; font-weight:500; color:var(--text-faint);
                  text-transform:uppercase; letter-spacing:.06em; margin-bottom:.45rem">
        {{ i18n.t('eval.factors') }}
      </div>

      @for (f of ver.factors; track f.id) {
        <div class="factor-block">

          @if (editId() === f.id) {
            <!-- Factor edit form -->
            <div class="factor-hd-edit">
              <div style="font-size:.75rem; font-weight:500; color:var(--primary); margin-bottom:.35rem">
                Editing — {{ i18n.name(f.nameEn, f.nameAr) }}
              </div>
              <div style="display:flex; gap:.4rem; flex-wrap:wrap; align-items:center">
                <input [(ngModel)]="ef.code"      placeholder="{{ i18n.t('common.code') }}"   style="max-width:72px" />
                <input [(ngModel)]="ef.nameEn"    placeholder="{{ i18n.t('common.nameEn') }}" style="flex:1; min-width:120px" />
                <input [(ngModel)]="ef.nameAr"    placeholder="{{ i18n.t('common.nameAr') }}" dir="rtl" style="flex:1; min-width:100px" />
                <input type="number" [(ngModel)]="ef.sortOrder" placeholder="#" style="max-width:60px" />
                <button class="sm" (click)="saveFactor(f)">{{ i18n.t('common.save') }}</button>
                <button class="sm subtle" (click)="editId.set(null)">{{ i18n.t('common.cancel') }}</button>
              </div>
            </div>
          } @else {
            <!-- Factor header -->
            <div class="factor-hd" (click)="toggleFactor(f.id)">
              <div class="factor-code">{{ f.code }}</div>
              <div style="flex:1; min-width:0">
                <div style="font-size:.95rem; font-weight:500; color:var(--text)">
                  {{ i18n.name(f.nameEn, f.nameAr) }}
                </div>
                <div style="font-size:.75rem; color:var(--text-faint); margin-top:1px">
                  {{ f.questions.length }} question{{ f.questions.length !== 1 ? 's' : '' }}
                  · order #{{ f.sortOrder }}
                </div>
              </div>
              @if (isDraft()) {
                <button class="sm subtle" title="{{ i18n.t('common.edit') }}"
                        (click)="startFactor(f); $event.stopPropagation()">
                  <i class="ti ti-pencil"></i>
                </button>
                <button class="sm subtle" title="{{ i18n.t('common.delete') }}"
                        (click)="deleteFactor(ver, f); $event.stopPropagation()">
                  <i class="ti ti-trash"></i>
                </button>
              }
              <i class="ti {{ isExpanded(f.id) ? 'ti-chevron-up' : 'ti-chevron-down' }}"
                 style="font-size:.85rem; color:var(--text-faint); margin-inline-start:.25rem"></i>
            </div>
          }

          @if (isExpanded(f.id)) {
            <div class="factor-body">

              @for (q of f.questions; track q.id) {
                <div class="q-row">

                  @if (editId() === q.id) {
                    <!-- Question edit -->
                    <div style="display:flex; gap:.4rem">
                      <div class="q-num" style="background:var(--primary-soft); color:var(--primary); margin-top:2px">
                        {{ q.sortOrder }}
                      </div>
                      <div class="q-edit-form">
                        <div style="font-size:.72rem; font-weight:500; color:var(--primary)">Editing question</div>
                        <input [(ngModel)]="eq.textEn" placeholder="{{ i18n.t('eval.questionEn') }}" />
                        <input [(ngModel)]="eq.textAr" placeholder="{{ i18n.t('eval.questionAr') }}" dir="rtl" />
                        <div style="display:flex; gap:.35rem; align-items:center">
                          <select [(ngModel)]="eq.questionType" style="width:auto; font-size:.78rem">
                            <option value="SingleChoice">{{ i18n.t('eval.singleChoice') }}</option>
                            <option value="MultipleChoice">{{ i18n.t('eval.multipleChoice') }}</option>
                          </select>
                          <span style="font-size:.75rem; color:var(--text-muted)">Order</span>
                          <input type="number" [(ngModel)]="eq.sortOrder" style="max-width:56px" />
                          <button class="sm" (click)="saveQuestion(f, q)">{{ i18n.t('common.save') }}</button>
                          <button class="sm subtle" (click)="editId.set(null)">{{ i18n.t('common.cancel') }}</button>
                        </div>
                      </div>
                    </div>

                  } @else {
                    <!-- Question display -->
                    <div style="display:flex; align-items:flex-start; gap:.4rem; margin-bottom:.4rem">
                      <div class="q-num">{{ q.sortOrder }}</div>
                      <div style="flex:1; font-size:.9rem; color:var(--text); line-height:1.4">
                        {{ i18n.name(q.questionTextEn, q.questionTextAr) }}
                        @if (q.questionType === 'MultipleChoice') {
                          <span class="pts-badge" style="margin-inline-start:6px; vertical-align:middle">{{ i18n.t('eval.multipleChoice') }}</span>
                        }
                      </div>
                      @if (isDraft()) {
                        <button class="sm subtle" (click)="startQuestion(q)"><i class="ti ti-pencil"></i></button>
                        <button class="sm subtle" (click)="deleteQuestion(f, q)"><i class="ti ti-trash"></i></button>
                      }
                    </div>

                    <!-- Option chips -->
                    <div style="display:flex; gap:5px; flex-wrap:wrap; align-items:center; padding-inline-start:28px">
                      @for (o of q.options; track o.id) {
                        @if (editId() === o.id) {
                          <div class="option-edit-form">
                            <input [(ngModel)]="eo.labelEn" placeholder="{{ i18n.t('eval.labelEn') }}" style="width:100px" />
                            <input [(ngModel)]="eo.labelAr" placeholder="{{ i18n.t('eval.labelAr') }}" dir="rtl" style="width:80px" />
                            <input type="number" [(ngModel)]="eo.points" style="width:48px" />
                            <span style="font-size:.72rem; color:var(--text-muted)">pts</span>
                            <button class="sm" (click)="saveOption(q, o)">{{ i18n.t('common.save') }}</button>
                            <button class="sm subtle" style="padding:.35rem .5rem" (click)="editId.set(null)">✕</button>
                          </div>
                        } @else {
                          <div class="ochip">
                            {{ i18n.name(o.labelEn, o.labelAr) }}
                            <span class="pts-badge">{{ o.points }}</span>
                            @if (isDraft()) {
                              <button class="icon-btn" (click)="startOption(o)" title="{{ i18n.t('common.edit') }}">
                                <i class="ti ti-pencil"></i>
                              </button>
                              <button class="icon-btn" (click)="deleteOption(q, o)" title="{{ i18n.t('common.delete') }}">
                                <i class="ti ti-trash"></i>
                              </button>
                            }
                          </div>
                        }
                      } @empty {
                        <span class="faint" style="font-size:.82rem">{{ i18n.t('common.none') }}</span>
                      }
                    </div>

                    <!-- Add option -->
                    @if (isDraft()) {
                      <div class="add-opts">
                        <input [(ngModel)]="ao(q.id).labelEn" placeholder="{{ i18n.t('eval.labelEn') }}" />
                        <input [(ngModel)]="ao(q.id).labelAr" placeholder="{{ i18n.t('eval.labelAr') }}" dir="rtl" />
                        <input type="number" [(ngModel)]="ao(q.id).points" placeholder="pts" style="max-width:56px" />
                        <button class="sm subtle" (click)="addOption(q.id, q)">
                          + {{ i18n.t('eval.addOption') }}
                        </button>
                      </div>
                    }
                  }

                </div>
              } @empty {
                <div class="q-row"><p class="muted" style="margin:0">{{ i18n.t('common.none') }}</p></div>
              }

              <!-- Add question -->
              @if (isDraft()) {
                <div class="add-q-row">
                  <input [(ngModel)]="aq(f.id).textEn" placeholder="{{ i18n.t('eval.questionEn') }}" style="flex:1; min-width:140px" />
                  <input [(ngModel)]="aq(f.id).textAr" placeholder="{{ i18n.t('eval.questionAr') }}" dir="rtl" style="flex:1; min-width:110px" />
                  <select [(ngModel)]="aq(f.id).questionType" style="width:auto; font-size:.82rem">
                    <option value="SingleChoice">{{ i18n.t('eval.singleChoice') }}</option>
                    <option value="MultipleChoice">{{ i18n.t('eval.multipleChoice') }}</option>
                  </select>
                  <button class="sm subtle" (click)="addQuestion(f.id, f)">
                    + {{ i18n.t('eval.addQuestion') }}
                  </button>
                </div>
              }

            </div>
          }

        </div>
      } @empty {
        <p class="muted">{{ i18n.t('common.none') }}</p>
      }

      <!-- Add factor -->
      @if (isDraft()) {
        <div class="add-factor-block">
          <div style="font-size:.75rem; color:var(--text-faint); margin-bottom:.35rem">
            {{ i18n.t('eval.addFactor') }}
          </div>
          <div style="display:flex; gap:.4rem; flex-wrap:wrap; align-items:center">
            <input [(ngModel)]="nf.code"   placeholder="{{ i18n.t('common.code') }}"   style="max-width:72px" />
            <input [(ngModel)]="nf.nameEn" placeholder="{{ i18n.t('common.nameEn') }}" style="flex:1; min-width:120px" />
            <input [(ngModel)]="nf.nameAr" placeholder="{{ i18n.t('common.nameAr') }}" dir="rtl" style="flex:1; min-width:100px" />
            <button class="sm" (click)="addFactor()">+ {{ i18n.t('eval.addFactor') }}</button>
          </div>
        </div>
      }

      <!-- Grade mappings -->
      <div class="card">
        <div class="card-header">
          <span class="card-title">{{ i18n.t('eval.gradeMappings') }}</span>
          @if (mappingRange()) {
            <span style="font-size:.75rem; color:var(--text-faint)">
              {{ mappingRange()!.min }} – {{ mappingRange()!.max }} pts
            </span>
          }
        </div>

        @if (mappingWarnings().length > 0) {
          <div class="alert info" style="margin-bottom:.75rem">
            @for (w of mappingWarnings(); track w) {
              <div><i class="ti ti-alert-triangle"></i> {{ w }}</div>
            }
          </div>
        }

        @for (gm of ver.gradeMappings; track gm.id) {
          @if (editId() === gm.id) {
            <div style="padding:.5rem 0; border-bottom:1px solid var(--border)">
              <div style="font-size:.72rem; font-weight:500; color:var(--primary); margin-bottom:.3rem">
                Editing — {{ gm.gradeCode }}
              </div>
              <div style="display:flex; gap:.4rem; align-items:center; flex-wrap:wrap">
                <select [(ngModel)]="egm.gradeId" style="width:auto">
                  @for (g of grades(); track g.id) {
                    <option [ngValue]="g.id">{{ g.code }}</option>
                  }
                </select>
                <span style="font-size:.8rem; color:var(--text-muted)">Min</span>
                <input type="number" [(ngModel)]="egm.minScore" style="max-width:72px" />
                <span style="font-size:.8rem; color:var(--text-muted)">Max</span>
                <input type="number" [(ngModel)]="egm.maxScore" style="max-width:72px" />
                <button class="sm" (click)="saveMapping(ver, gm)">{{ i18n.t('common.save') }}</button>
                <button class="sm subtle" (click)="editId.set(null)">{{ i18n.t('common.cancel') }}</button>
              </div>
            </div>
          } @else {
            <div class="gm-row">
              <span style="font-size:.88rem; font-weight:500; color:var(--text); min-width:48px">
                {{ gm.gradeCode }}
              </span>
              <div class="gm-bar">
                <div class="gm-bar-fill" [style.left]="barLeft(gm)" [style.width]="barWidth(gm)"></div>
              </div>
              <span style="font-size:.78rem; color:var(--text-faint); min-width:80px; text-align:end">
                {{ gm.minScore }} – {{ gm.maxScore }}
              </span>
              @if (isDraft()) {
                <button class="sm subtle" (click)="startMapping(gm)"><i class="ti ti-pencil"></i></button>
                <button class="sm subtle" (click)="deleteMapping(ver, gm)"><i class="ti ti-trash"></i></button>
              }
            </div>
          }
        } @empty {
          <p class="muted">{{ i18n.t('common.none') }}</p>
        }

        @if (isDraft()) {
          <div class="add-map-row">
            <select [(ngModel)]="nm.gradeId" style="width:auto">
              <option [ngValue]="''">{{ i18n.t('field.grade') }}</option>
              @for (g of grades(); track g.id) {
                <option [ngValue]="g.id">{{ g.code }}</option>
              }
            </select>
            <span style="font-size:.8rem; color:var(--text-muted)">Min</span>
            <input type="number" [(ngModel)]="nm.minScore" placeholder="0" style="max-width:72px" />
            <span style="font-size:.8rem; color:var(--text-muted)">Max</span>
            <input type="number" [(ngModel)]="nm.maxScore" placeholder="0" style="max-width:72px" />
            <button class="sm subtle" (click)="addMapping()">+ {{ i18n.t('eval.addMapping') }}</button>
          </div>
        }
      </div>

    } @else if (!error()) {
      <p>{{ i18n.t('common.loading') }}</p>
    }
  `,
})
export class VersionEditor {
  private api = inject(Api);
  private route = inject(ActivatedRoute);
  private toast = inject(ToastService);
  private cs = inject(ConfirmService);
  readonly i18n = inject(I18n);
  readonly v = signal<MethodologyVersionDetail | null>(null);
  readonly grades = signal<Grade[]>([]);
  readonly error = signal<string | null>(null);
  readonly editId = signal<string | null>(null);
  readonly expandedIds = signal<Set<string>>(new Set());

  nf  = { code: '', nameEn: '', nameAr: '', sortOrder: 1 };
  nm  = { gradeId: '', minScore: 0, maxScore: 0 };
  ef  = { code: '', nameEn: '', nameAr: '', sortOrder: 1 };
  eq  = { textEn: '', textAr: '', questionType: 'SingleChoice' as QuestionType, sortOrder: 1 };
  eo  = { labelEn: '', labelAr: '', points: 0 };
  egm = { gradeId: '', minScore: 0, maxScore: 0 };
  private aqf: Record<string, { textEn: string; textAr: string; questionType: QuestionType }> = {};
  private aof: Record<string, { labelEn: string; labelAr: string; points: number }> = {};

  readonly mappingWarnings = computed((): string[] => {
    const mappings = this.v()?.gradeMappings ?? [];
    if (mappings.length < 2) return [];
    const warnings: string[] = [];
    for (const m of mappings) {
      if (m.minScore > m.maxScore)
        warnings.push(`${m.gradeCode}: min (${m.minScore}) > max (${m.maxScore})`);
    }
    const valid = [...mappings]
      .filter(m => m.minScore <= m.maxScore)
      .sort((a, b) => a.minScore - b.minScore);
    for (let i = 0; i < valid.length - 1; i++) {
      const curr = valid[i], next = valid[i + 1];
      if (curr.maxScore >= next.minScore)
        warnings.push(`Overlap: ${curr.gradeCode} [${curr.minScore}–${curr.maxScore}] and ${next.gradeCode} [${next.minScore}–${next.maxScore}]`);
      else if (curr.maxScore + 1 < next.minScore)
        warnings.push(`Gap [${curr.maxScore + 1}–${next.minScore - 1}] not covered (between ${curr.gradeCode} and ${next.gradeCode})`);
    }
    return warnings;
  });

  readonly mappingRange = computed(() => {
    const mappings = this.v()?.gradeMappings ?? [];
    if (!mappings.length) return null;
    return {
      min: Math.min(...mappings.map(m => m.minScore)),
      max: Math.max(...mappings.map(m => m.maxScore)),
    };
  });

  barLeft(gm: GradeMapping): string {
    const r = this.mappingRange();
    if (!r || r.max === r.min) return '0%';
    return ((gm.minScore - r.min) / (r.max - r.min) * 100).toFixed(1) + '%';
  }

  barWidth(gm: GradeMapping): string {
    const r = this.mappingRange();
    if (!r || r.max === r.min) return '100%';
    return ((gm.maxScore - gm.minScore) / (r.max - r.min) * 100).toFixed(1) + '%';
  }

  private get id() { return this.route.snapshot.paramMap.get('id')!; }
  constructor() { this.load(); }
  isDraft() { return this.v()?.status === 'Draft'; }
  aq(fid: string) { return (this.aqf[fid] ??= { textEn: '', textAr: '', questionType: 'SingleChoice' }); }
  ao(qid: string) { return (this.aof[qid] ??= { labelEn: '', labelAr: '', points: 0 }); }

  toggleFactor(id: string) {
    const s = new Set(this.expandedIds());
    if (s.has(id)) s.delete(id); else s.add(id);
    this.expandedIds.set(s);
  }

  isExpanded(id: string): boolean { return this.expandedIds().has(id); }

  private async load() {
    const isFirstLoad = this.v() === null;
    this.error.set(null);
    try {
      const [ver, grades] = await Promise.all([this.api.version(this.id), this.api.grades()]);
      this.v.set(ver);
      this.grades.set(grades);
      if (isFirstLoad) {
        this.expandedIds.set(new Set(ver.factors.map(f => f.id)));
      }
    } catch (e: any) { this.error.set(e?.error?.detail ?? 'Failed to load version.'); }
  }

  private fail(e: any) { this.toast.error(e?.error?.detail ?? 'Request failed.'); }

  async addFactor() {
    const ver = this.v();
    const sortOrder = ver && ver.factors.length > 0
      ? Math.max(...ver.factors.map(f => f.sortOrder)) + 1 : 1;
    const prevIds = new Set(ver?.factors.map(f => f.id) ?? []);
    try {
      await this.api.addFactor(this.id, { code: this.nf.code, nameEn: this.nf.nameEn, nameAr: this.nf.nameAr || null, sortOrder, weight: null });
      this.nf = { code: '', nameEn: '', nameAr: '', sortOrder: 1 };
      this.toast.success(this.i18n.t('toast.created'));
      await this.load();
      const newIds = this.v()?.factors.filter(f => !prevIds.has(f.id)).map(f => f.id) ?? [];
      if (newIds.length) {
        const s = new Set(this.expandedIds());
        newIds.forEach(id => s.add(id));
        this.expandedIds.set(s);
      }
    } catch (e) { this.fail(e); }
  }

  async addQuestion(fid: string, factor: FactorDetail) {
    const f = this.aq(fid);
    const sortOrder = factor.questions.length > 0
      ? Math.max(...factor.questions.map(q => q.sortOrder)) + 1 : 1;
    try {
      await this.api.addQuestion(fid, { questionTextEn: f.textEn, questionTextAr: f.textAr || null, helpTextEn: null, helpTextAr: null, questionType: f.questionType, sortOrder });
      this.aqf[fid] = { textEn: '', textAr: '', questionType: 'SingleChoice' };
      this.toast.success(this.i18n.t('toast.created'));
      await this.load();
    } catch (e) { this.fail(e); }
  }

  async addOption(qid: string, question: QuestionDetail) {
    const o = this.ao(qid);
    const sortOrder = question.options.length > 0
      ? Math.max(...question.options.map(op => op.sortOrder)) + 1 : 1;
    try {
      await this.api.addOption(qid, { labelEn: o.labelEn, labelAr: o.labelAr || null, points: o.points, sortOrder });
      this.aof[qid] = { labelEn: '', labelAr: '', points: 0 };
      this.toast.success(this.i18n.t('toast.created'));
      await this.load();
    } catch (e) { this.fail(e); }
  }

  async addMapping() {
    try {
      await this.api.addGradeMapping(this.id, { ...this.nm });
      this.nm = { gradeId: '', minScore: 0, maxScore: 0 };
      this.toast.success(this.i18n.t('toast.saved'));
      await this.load();
    } catch (e) { this.fail(e); }
  }

  startFactor(f: FactorDetail) {
    this.ef = { code: f.code, nameEn: f.nameEn, nameAr: f.nameAr ?? '', sortOrder: f.sortOrder };
    this.editId.set(f.id);
    const s = new Set(this.expandedIds());
    s.add(f.id);
    this.expandedIds.set(s);
  }

  async saveFactor(f: FactorDetail) {
    try {
      await this.api.updateFactor(this.id, f.id, { code: this.ef.code, nameEn: this.ef.nameEn, nameAr: this.ef.nameAr || null, sortOrder: this.ef.sortOrder, weight: f.weight });
      this.editId.set(null);
      this.toast.success(this.i18n.t('toast.saved'));
      await this.load();
    } catch (e) { this.fail(e); }
  }

  startQuestion(q: QuestionDetail) {
    this.eq = { textEn: q.questionTextEn, textAr: q.questionTextAr ?? '', questionType: q.questionType, sortOrder: q.sortOrder };
    this.editId.set(q.id);
  }

  async saveQuestion(f: FactorDetail, q: QuestionDetail) {
    try {
      await this.api.updateQuestion(f.id, q.id, { questionTextEn: this.eq.textEn, questionTextAr: this.eq.textAr || null, helpTextEn: q.helpTextEn, helpTextAr: q.helpTextAr, questionType: this.eq.questionType, sortOrder: this.eq.sortOrder });
      this.editId.set(null);
      this.toast.success(this.i18n.t('toast.saved'));
      await this.load();
    } catch (e) { this.fail(e); }
  }

  startOption(o: AnswerOption) {
    this.eo = { labelEn: o.labelEn, labelAr: o.labelAr ?? '', points: o.points };
    this.editId.set(o.id);
  }

  async saveOption(q: QuestionDetail, o: AnswerOption) {
    try {
      await this.api.updateOption(q.id, o.id, { labelEn: this.eo.labelEn, labelAr: this.eo.labelAr || null, points: this.eo.points, sortOrder: o.sortOrder });
      this.editId.set(null);
      this.toast.success(this.i18n.t('toast.saved'));
      await this.load();
    } catch (e) { this.fail(e); }
  }

  async deleteFactor(ver: MethodologyVersionDetail, f: FactorDetail) {
    const ok = await this.cs.confirm({
      title: `Delete factor "${this.i18n.name(f.nameEn, f.nameAr)}"?`,
      body: 'This will also delete all its questions and answer options.',
      confirmLabel: this.i18n.t('confirm.delete'),
      danger: true,
    });
    if (!ok) return;
    try {
      await this.api.deleteFactor(ver.id, f.id);
      this.toast.success(this.i18n.t('toast.deleted'));
      await this.load();
    } catch (e) { this.fail(e); }
  }

  async deleteQuestion(f: FactorDetail, q: QuestionDetail) {
    const ok = await this.cs.confirm({
      title: 'Delete question?',
      body: this.i18n.name(q.questionTextEn, q.questionTextAr) ?? undefined,
      confirmLabel: this.i18n.t('confirm.delete'),
      danger: true,
    });
    if (!ok) return;
    try {
      await this.api.deleteQuestion(f.id, q.id);
      this.toast.success(this.i18n.t('toast.deleted'));
      await this.load();
    } catch (e) { this.fail(e); }
  }

  async deleteOption(q: QuestionDetail, o: AnswerOption) {
    const ok = await this.cs.confirm({
      title: `Delete option "${this.i18n.name(o.labelEn, o.labelAr)}"?`,
      confirmLabel: this.i18n.t('confirm.delete'),
      danger: true,
    });
    if (!ok) return;
    try {
      await this.api.deleteOption(q.id, o.id);
      this.toast.success(this.i18n.t('toast.deleted'));
      await this.load();
    } catch (e) { this.fail(e); }
  }

  startMapping(gm: GradeMapping) {
    this.egm = { gradeId: gm.gradeId, minScore: gm.minScore, maxScore: gm.maxScore };
    this.editId.set(gm.id);
  }

  async saveMapping(ver: MethodologyVersionDetail, gm: GradeMapping) {
    try {
      await this.api.updateGradeMapping(ver.id, gm.id, this.egm);
      this.editId.set(null);
      this.toast.success(this.i18n.t('toast.saved'));
      await this.load();
    } catch (e) { this.fail(e); }
  }

  async deleteMapping(ver: MethodologyVersionDetail, gm: GradeMapping) {
    const ok = await this.cs.confirm({
      title: `Delete grade mapping for ${gm.gradeCode ?? gm.gradeId}?`,
      confirmLabel: this.i18n.t('confirm.delete'),
      danger: true,
    });
    if (!ok) return;
    try {
      await this.api.deleteGradeMapping(ver.id, gm.id);
      this.toast.success(this.i18n.t('toast.deleted'));
      await this.load();
    } catch (e) { this.fail(e); }
  }

  async publish() {
    const ok = await this.cs.confirm({
      title: this.i18n.t('confirm.publish.title'),
      body: this.i18n.t('confirm.publish.body'),
      danger: false,
    });
    if (!ok) return;
    try {
      await this.api.publishVersion(this.id);
      this.toast.success(this.i18n.t('toast.published'));
      await this.load();
    } catch (e) { this.fail(e); }
  }
}
