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
    .factor-body { background: var(--bg); }
    .q-row { padding: .6rem .9rem; border-top: 1px solid var(--border); }
    .q-num {
      width: 20px; height: 20px; border-radius: 50%;
      background: var(--surface-2); font-size: 10px; font-weight: 500; color: var(--text-muted);
      display: flex; align-items: center; justify-content: center; flex-shrink: 0; line-height: 1;
    }
    .olist { border: 1px solid var(--border); border-radius: 8px; overflow: hidden; background: var(--surface); }
    .ochip {
      display: flex; align-items: flex-start; gap: 10px;
      padding: 8px 10px; border-bottom: 1px solid var(--border);
    }
    .ochip:last-child { border-bottom: none; }
    .ochip-main { flex: 1; min-width: 0; }
    .ochip-label { font-size: .85rem; color: var(--text); }
    .ochip-actions { display: flex; align-items: center; gap: 6px; flex-shrink: 0; }
    .pts-badge {
      display: inline-flex; align-items: center; justify-content: center;
      min-width: 26px; height: 18px; border-radius: 4px;
      font-size: .75rem; font-weight: 500;
      background: var(--primary-soft); color: var(--primary); padding: 0 4px;
    }
    .rating-tag {
      font-size: .7rem; color: var(--text-faint); letter-spacing: .02em;
    }
    .req-tag {
      font-size: .68rem; padding: 2px 6px; border-radius: 4px;
      background: var(--surface-2); color: var(--text-faint);
    }
    .subject-help {
      font-size: .78rem; color: var(--text-faint); line-height: 1.4;
      margin-top: 2px; font-weight: 400;
    }
    .icon-btn {
      background: none; border: none; cursor: pointer; padding: 0;
      color: var(--text-faint); font-size: .78rem; display: flex; line-height: 1;
    }
    .icon-btn:hover { color: var(--danger); }
    .add-q-row {
      padding: .45rem .9rem; border-top: 1px dashed var(--border);
    }
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
    .budget-edit {
      margin-top: .5rem; padding: .5rem .7rem;
      background: var(--primary-soft); border-radius: var(--radius);
      display: flex; gap: .4rem; align-items: center; flex-wrap: wrap;
    }
    .budget-edit input { max-width: 80px; }

    .modal-wide { max-width: 560px; text-align: start; }
    .field { margin-bottom: .8rem; }
    .field label {
      display: block; font-size: .72rem; font-weight: 500;
      color: var(--text-faint); margin-bottom: 4px;
    }
    .field input, .field select, .field textarea { width: 100%; }
    .field-row-2 { display: grid; grid-template-columns: 1fr 1fr; gap: .7rem; }
    .modal-bottom {
      display: flex; gap: .7rem; flex-wrap: wrap; align-items: flex-end;
      border-top: 1px solid var(--border); padding-top: .8rem; margin-top: .2rem;
    }
    .modal-bottom .field { flex: 1; min-width: 90px; margin-bottom: 0; }
    .modal-bottom .field-check {
      display: flex; align-items: center; gap: 6px; height: 36px;
      font-size: .82rem; color: var(--text-muted); flex: 1; min-width: 90px;
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

          @if (editingBudget()) {
            <div class="budget-edit">
              <span style="font-size:.78rem; color:var(--primary); font-weight:500">{{ i18n.t('eval.pointBudget') }}</span>
              <input type="number" [(ngModel)]="epb.minPoints" />
              <span>–</span>
              <input type="number" [(ngModel)]="epb.maxPoints" />
              <button class="sm" (click)="savePointBudget()">{{ i18n.t('common.save') }}</button>
              <button class="sm subtle" (click)="editingBudget.set(false)">{{ i18n.t('common.cancel') }}</button>
            </div>
          } @else {
            <div style="margin-top:.5rem; font-size:.82rem; color:var(--text-faint); display:flex; align-items:center; gap:.35rem">
              <i class="ti ti-target"></i>
              {{ i18n.t('eval.pointBudget') }}: {{ ver.minPoints }} – {{ ver.maxPoints }} {{ i18n.t('eval.pts') }}
              @if (isDraft()) {
                <button class="sm subtle" (click)="startPointBudget(ver)"><i class="ti ti-pencil"></i></button>
              }
            </div>
          }
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
      <div style="display:flex; align-items:center; margin-bottom:.45rem">
        <div style="font-size:.78rem; font-weight:500; color:var(--text-faint);
                    text-transform:uppercase; letter-spacing:.06em">
          {{ i18n.t('eval.factors') }}
        </div>
        <div class="spacer"></div>
        @if (isDraft()) {
          <button class="sm subtle" (click)="openAddFactor(ver)">
            <i class="ti ti-plus"></i> {{ i18n.t('eval.addFactor') }}
          </button>
        }
      </div>

      @if (isDraft() && ver.factors.length > 0 && !factorWeightValid()) {
        <div class="alert info" style="margin-bottom:.6rem">
          <i class="ti ti-alert-triangle"></i> {{ i18n.t('eval.factorWeightWarn') }} ({{ factorWeightSum() }}%)
        </div>
      }

      @for (f of ver.factors; track f.id) {
        <div class="factor-block">
          <div class="factor-hd" (click)="toggleFactor(f.id)">
            <div class="factor-code">{{ f.code }}</div>
            <div style="flex:1; min-width:0">
              <div style="font-size:.95rem; font-weight:500; color:var(--text)">
                {{ i18n.name(f.nameEn, f.nameAr) }}
                <span class="pts-badge" style="margin-inline-start:6px; vertical-align:middle">
                  {{ f.weight }}% · {{ f.calculatedPoints }} {{ i18n.t('eval.pts') }}
                </span>
              </div>
              @if (f.helpTextEn || f.helpTextAr) {
                <div class="subject-help">{{ i18n.name(f.helpTextEn, f.helpTextAr) }}</div>
              }
              <div style="font-size:.75rem; color:var(--text-faint); margin-top:1px">
                {{ f.questions.length }} question{{ f.questions.length !== 1 ? 's' : '' }}
                · order #{{ f.sortOrder }}
              </div>
            </div>
            @if (isDraft()) {
              <button class="sm subtle" title="{{ i18n.t('common.edit') }}"
                      (click)="openEditFactor(f); $event.stopPropagation()">
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

          @if (isExpanded(f.id)) {
            <div class="factor-body">

              @if (isDraft() && f.questions.length > 0 && !questionWeightValid(f)) {
                <div class="alert info" style="margin:.5rem .9rem">
                  <i class="ti ti-alert-triangle"></i> {{ i18n.t('eval.questionWeightWarn') }} ({{ questionWeightSum(f) }}%)
                </div>
              }

              @for (q of f.questions; track q.id) {
                <div class="q-row">
                  <div style="display:flex; align-items:flex-start; gap:.4rem; margin-bottom:.4rem">
                    <div class="q-num">{{ q.sortOrder }}</div>
                    <div style="flex:1; font-size:.9rem; color:var(--text); line-height:1.4">
                      {{ i18n.name(q.questionTextEn, q.questionTextAr) }}
                      <span class="pts-badge" style="margin-inline-start:6px; vertical-align:middle">
                        {{ q.weight }}% · {{ q.calculatedPoints }} {{ i18n.t('eval.pts') }}
                      </span>
                      <span class="req-tag" style="margin-inline-start:4px">
                        {{ q.isRequired ? i18n.t('eval.required') : i18n.t('eval.optional') }}
                      </span>
                      @if (q.questionType === 'MultipleChoice') {
                        <span class="pts-badge" style="margin-inline-start:6px; vertical-align:middle">{{ i18n.t('eval.multipleChoice') }}</span>
                      }
                      @if (q.helpTextEn || q.helpTextAr) {
                        <div class="subject-help">{{ i18n.name(q.helpTextEn, q.helpTextAr) }}</div>
                      }
                    </div>
                    @if (isDraft()) {
                      <button class="sm subtle" (click)="openEditQuestion(f, q)"><i class="ti ti-pencil"></i></button>
                      <button class="sm subtle" (click)="deleteQuestion(f, q)"><i class="ti ti-trash"></i></button>
                    }
                  </div>

                  <!-- Options -->
                  <div style="padding-inline-start:28px">
                    @if (q.options.length > 0) {
                      <div class="olist">
                        @for (o of q.options; track o.id) {
                          <div class="ochip">
                            <div class="ochip-main">
                              <span class="ochip-label">{{ i18n.name(o.labelEn, o.labelAr) }}</span>
                              <span class="rating-tag" style="margin-inline-start:5px">★{{ o.rating }}</span>
                              @if (o.helpTextEn || o.helpTextAr) {
                                <div class="subject-help">{{ i18n.name(o.helpTextEn, o.helpTextAr) }}</div>
                              }
                            </div>
                            <span class="pts-badge" title="{{ i18n.t('eval.calculatedPoints') }}">{{ o.calculatedPoints }} {{ i18n.t('eval.pts') }}</span>
                            @if (isDraft()) {
                              <div class="ochip-actions">
                                <button class="icon-btn" (click)="openEditOption(q, o)" title="{{ i18n.t('common.edit') }}">
                                  <i class="ti ti-pencil"></i>
                                </button>
                                <button class="icon-btn" (click)="deleteOption(q, o)" title="{{ i18n.t('common.delete') }}">
                                  <i class="ti ti-trash"></i>
                                </button>
                              </div>
                            }
                          </div>
                        }
                      </div>
                    } @else {
                      <span class="faint" style="font-size:.82rem">{{ i18n.t('common.none') }}</span>
                    }
                    @if (isDraft()) {
                      <button class="sm subtle" style="margin-top:.4rem" (click)="openAddOption(q)">
                        <i class="ti ti-plus"></i> {{ i18n.t('eval.addOption') }}
                      </button>
                    }
                  </div>
                </div>
              } @empty {
                <div class="q-row"><p class="muted" style="margin:0">{{ i18n.t('common.none') }}</p></div>
              }

              @if (isDraft()) {
                <div class="add-q-row">
                  <button class="sm subtle" (click)="openAddQuestion(f)">
                    <i class="ti ti-plus"></i> {{ i18n.t('eval.addQuestion') }}
                  </button>
                </div>
              }

            </div>
          }

        </div>
      } @empty {
        <p class="muted">{{ i18n.t('common.none') }}</p>
      }

      <!-- Grade mappings -->
      <div class="card">
        <div class="card-header">
          <span class="card-title">{{ i18n.t('eval.gradeMappings') }}</span>
          <div style="display:flex; align-items:center; gap:.6rem">
            @if (mappingRange(); as r) {
              <span style="font-size:.75rem; color:var(--text-faint)">
                {{ r.min }} – {{ r.max }} {{ i18n.t('eval.pts') }}
              </span>
            }
            @if (isDraft() && ver.gradeMappings.length > 0) {
              <button class="sm subtle" (click)="autoAssignRanges(ver)">
                <i class="ti ti-wand"></i> {{ i18n.t('eval.autoAssignRanges') }}
              </button>
            }
          </div>
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
          } @else if (rangingId() === gm.id) {
            <div style="padding:.5rem 0; border-bottom:1px solid var(--border)">
              <div style="font-size:.72rem; font-weight:500; color:var(--primary); margin-bottom:.3rem">
                {{ i18n.t('eval.setRange') }} — {{ gm.gradeCode }}
              </div>
              <div style="display:flex; gap:.4rem; align-items:center; flex-wrap:wrap">
                <span style="font-size:.8rem; color:var(--text-muted)">Min</span>
                <input type="number" [(ngModel)]="erange.minScore" style="max-width:72px" />
                <span style="font-size:.8rem; color:var(--text-muted)">Max</span>
                <input type="number" [(ngModel)]="erange.maxScore" style="max-width:72px" />
                <button class="sm" (click)="saveRange(ver, gm)">{{ i18n.t('common.save') }}</button>
                <button class="sm subtle" (click)="rangingId.set(null)">{{ i18n.t('common.cancel') }}</button>
              </div>
            </div>
          } @else {
            <div class="gm-row">
              <span style="font-size:.88rem; font-weight:500; color:var(--text); min-width:48px">
                {{ gm.gradeCode }}
              </span>
              @if (gm.minScore !== null && gm.maxScore !== null) {
                <div class="gm-bar">
                  <div class="gm-bar-fill" [style.left]="barLeft(gm)" [style.width]="barWidth(gm)"></div>
                </div>
                <span style="font-size:.78rem; color:var(--text-faint); min-width:80px; text-align:end">
                  {{ gm.minScore }} – {{ gm.maxScore }}
                </span>
              } @else {
                <span class="req-tag">{{ i18n.t('eval.notRanged') }}</span>
                <div class="spacer"></div>
              }
              @if (isDraft()) {
                @if (gm.minScore === null || gm.maxScore === null) {
                  <button class="sm subtle" (click)="startRange(gm)">{{ i18n.t('eval.setRange') }}</button>
                }
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
            <button class="sm subtle" (click)="addMapping()">+ {{ i18n.t('eval.assignGrade') }}</button>
          </div>
        }
      </div>

      <!-- Factor modal -->
      @if (fModalOpen()) {
        <div class="modal-backdrop" (click)="fModalOpen.set(false)">
          <div class="modal modal-wide" role="dialog" aria-modal="true" aria-labelledby="factor-modal-title"
               (click)="$event.stopPropagation()">
            <h2 id="factor-modal-title">{{ fm.id ? i18n.t('eval.editFactor') : i18n.t('eval.addFactor') }}</h2>

            <div class="field-row-2">
              <div class="field">
                <label>{{ i18n.t('common.nameEn') }}</label>
                <input [(ngModel)]="fm.nameEn" />
              </div>
              <div class="field">
                <label>{{ i18n.t('common.nameAr') }}</label>
                <input [(ngModel)]="fm.nameAr" dir="rtl" />
              </div>
            </div>

            <div class="field-row-2">
              <div class="field">
                <label>{{ i18n.t('eval.helpTextEn') }}</label>
                <textarea rows="3" [(ngModel)]="fm.helpTextEn" placeholder="{{ i18n.t('eval.helpTextHint') }}"></textarea>
              </div>
              <div class="field">
                <label>{{ i18n.t('eval.helpTextAr') }}</label>
                <textarea rows="3" dir="rtl" [(ngModel)]="fm.helpTextAr" placeholder="{{ i18n.t('eval.helpTextHint') }}"></textarea>
              </div>
            </div>

            <div class="modal-bottom">
              <div class="field">
                <label>{{ i18n.t('common.code') }}</label>
                <input [(ngModel)]="fm.code" />
              </div>
              <div class="field">
                <label>{{ i18n.t('eval.weight') }} (%)</label>
                <input type="number" [(ngModel)]="fm.weight" min="0" max="100" step="0.1" />
              </div>
              <div class="field">
                <label>{{ i18n.t('eval.order') }}</label>
                <input type="number" [(ngModel)]="fm.sortOrder" />
              </div>
            </div>

            <div class="modal-actions">
              <button class="subtle" (click)="fModalOpen.set(false)">{{ i18n.t('common.cancel') }}</button>
              <button (click)="saveFactorModal()">{{ i18n.t('common.save') }}</button>
            </div>
          </div>
        </div>
      }

      <!-- Question modal -->
      @if (qModalOpen()) {
        <div class="modal-backdrop" (click)="qModalOpen.set(false)">
          <div class="modal modal-wide" role="dialog" aria-modal="true" aria-labelledby="question-modal-title"
               (click)="$event.stopPropagation()">
            <h2 id="question-modal-title">{{ qm.id ? i18n.t('eval.editQuestion') : i18n.t('eval.addQuestion') }}</h2>

            <div class="field-row-2">
              <div class="field">
                <label>{{ i18n.t('eval.questionEn') }}</label>
                <textarea rows="2" [(ngModel)]="qm.textEn"></textarea>
              </div>
              <div class="field">
                <label>{{ i18n.t('eval.questionAr') }}</label>
                <textarea rows="2" dir="rtl" [(ngModel)]="qm.textAr"></textarea>
              </div>
            </div>

            <div class="field-row-2">
              <div class="field">
                <label>{{ i18n.t('eval.helpTextEn') }}</label>
                <textarea rows="2" [(ngModel)]="qm.helpTextEn" placeholder="{{ i18n.t('eval.helpTextHint') }}"></textarea>
              </div>
              <div class="field">
                <label>{{ i18n.t('eval.helpTextAr') }}</label>
                <textarea rows="2" dir="rtl" [(ngModel)]="qm.helpTextAr" placeholder="{{ i18n.t('eval.helpTextHint') }}"></textarea>
              </div>
            </div>

            <div class="modal-bottom">
              <div class="field">
                <label>{{ i18n.t('field.type') }}</label>
                <select [(ngModel)]="qm.questionType">
                  <option value="SingleChoice">{{ i18n.t('eval.singleChoice') }}</option>
                  <option value="MultipleChoice">{{ i18n.t('eval.multipleChoice') }}</option>
                </select>
              </div>
              <div class="field">
                <label>{{ i18n.t('eval.weight') }} (%)</label>
                <input type="number" [(ngModel)]="qm.weight" min="0" max="100" step="0.1" />
              </div>
              <div class="field">
                <label>{{ i18n.t('eval.order') }}</label>
                <input type="number" [(ngModel)]="qm.sortOrder" />
              </div>
              <label class="field-check">
                <input type="checkbox" [(ngModel)]="qm.isRequired" style="width:auto" /> {{ i18n.t('eval.required') }}
              </label>
            </div>

            <div class="modal-actions">
              <button class="subtle" (click)="qModalOpen.set(false)">{{ i18n.t('common.cancel') }}</button>
              <button (click)="saveQuestionModal()">{{ i18n.t('common.save') }}</button>
            </div>
          </div>
        </div>
      }

      <!-- Answer option modal -->
      @if (oModalOpen()) {
        <div class="modal-backdrop" (click)="oModalOpen.set(false)">
          <div class="modal modal-wide" role="dialog" aria-modal="true" aria-labelledby="option-modal-title"
               (click)="$event.stopPropagation()">
            <h2 id="option-modal-title">{{ om.id ? i18n.t('eval.editOption') : i18n.t('eval.addOption') }}</h2>

            <div class="field-row-2">
              <div class="field">
                <label>{{ i18n.t('eval.labelEn') }}</label>
                <input [(ngModel)]="om.labelEn" />
              </div>
              <div class="field">
                <label>{{ i18n.t('eval.labelAr') }}</label>
                <input [(ngModel)]="om.labelAr" dir="rtl" />
              </div>
            </div>

            <div class="field-row-2">
              <div class="field">
                <label>{{ i18n.t('eval.helpTextEn') }}</label>
                <textarea rows="2" [(ngModel)]="om.helpTextEn" placeholder="{{ i18n.t('eval.helpTextHint') }}"></textarea>
              </div>
              <div class="field">
                <label>{{ i18n.t('eval.helpTextAr') }}</label>
                <textarea rows="2" dir="rtl" [(ngModel)]="om.helpTextAr" placeholder="{{ i18n.t('eval.helpTextHint') }}"></textarea>
              </div>
            </div>

            <div class="modal-bottom">
              <div class="field">
                <label>{{ i18n.t('eval.rating') }}</label>
                <select [(ngModel)]="om.rating">
                  <option [ngValue]="1">1</option>
                  <option [ngValue]="2">2</option>
                  <option [ngValue]="3">3</option>
                  <option [ngValue]="4">4</option>
                  <option [ngValue]="5">5</option>
                </select>
              </div>
              <div class="field">
                <label>{{ i18n.t('eval.order') }}</label>
                <input type="number" [(ngModel)]="om.sortOrder" />
              </div>
            </div>

            <div class="modal-actions">
              <button class="subtle" (click)="oModalOpen.set(false)">{{ i18n.t('common.cancel') }}</button>
              <button (click)="saveOptionModal()">{{ i18n.t('common.save') }}</button>
            </div>
          </div>
        </div>
      }

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
  readonly rangingId = signal<string | null>(null);
  readonly expandedIds = signal<Set<string>>(new Set());
  readonly editingBudget = signal(false);

  readonly fModalOpen = signal(false);
  readonly qModalOpen = signal(false);
  readonly oModalOpen = signal(false);

  fm  = { id: null as string | null, code: '', nameEn: '', nameAr: '', helpTextEn: '', helpTextAr: '', sortOrder: 1, weight: 0 };
  qm  = { id: null as string | null, factorId: '', textEn: '', textAr: '', helpTextEn: '', helpTextAr: '', questionType: 'SingleChoice' as QuestionType, weight: 0, isRequired: true, sortOrder: 1 };
  om  = { id: null as string | null, questionId: '', labelEn: '', labelAr: '', helpTextEn: '', helpTextAr: '', rating: 1, sortOrder: 1 };

  nm  = { gradeId: '' };
  egm = { gradeId: '', minScore: 0, maxScore: 0 };
  erange = { minScore: 0, maxScore: 0 };
  epb = { minPoints: 200, maxPoints: 1000 };

  // Weight-sum checks are plain addition over values already on screen - not the scoring formula
  // (that lives server-side in ScoringService; calculated points below come straight from the API).
  readonly factorWeightSum = computed(() => {
    const factors = this.v()?.factors ?? [];
    return round1(factors.reduce((sum, f) => sum + f.weight, 0));
  });

  readonly factorWeightValid = computed(() => Math.abs(this.factorWeightSum() - 100) <= 0.01);

  questionWeightSum(f: FactorDetail): number {
    return round1(f.questions.reduce((sum, q) => sum + q.weight, 0));
  }

  questionWeightValid(f: FactorDetail): boolean {
    return Math.abs(this.questionWeightSum(f) - 100) <= 0.01;
  }

  readonly mappingWarnings = computed((): string[] => {
    const mappings = rangedOnly(this.v()?.gradeMappings ?? []);
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
    const mappings = rangedOnly(this.v()?.gradeMappings ?? []);
    if (!mappings.length) return null;
    return {
      min: Math.min(...mappings.map(m => m.minScore)),
      max: Math.max(...mappings.map(m => m.maxScore)),
    };
  });

  barLeft(gm: GradeMapping): string {
    const r = this.mappingRange();
    if (!r || r.max === r.min || gm.minScore === null) return '0%';
    return ((gm.minScore - r.min) / (r.max - r.min) * 100).toFixed(1) + '%';
  }

  barWidth(gm: GradeMapping): string {
    const r = this.mappingRange();
    if (!r || r.max === r.min || gm.minScore === null || gm.maxScore === null) return '0%';
    return ((gm.maxScore - gm.minScore) / (r.max - r.min) * 100).toFixed(1) + '%';
  }

  private get id() { return this.route.snapshot.paramMap.get('id')!; }
  constructor() { this.load(); }
  isDraft() { return this.v()?.status === 'Draft'; }

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

  startPointBudget(ver: MethodologyVersionDetail) {
    this.epb = { minPoints: ver.minPoints, maxPoints: ver.maxPoints };
    this.editingBudget.set(true);
  }

  async savePointBudget() {
    try {
      await this.api.setPointBudget(this.id, { ...this.epb });
      this.editingBudget.set(false);
      this.toast.success(this.i18n.t('toast.saved'));
      await this.load();
    } catch (e) { this.fail(e); }
  }

  openAddFactor(ver: MethodologyVersionDetail) {
    const sortOrder = ver.factors.length > 0 ? Math.max(...ver.factors.map(f => f.sortOrder)) + 1 : 1;
    this.fm = { id: null, code: '', nameEn: '', nameAr: '', helpTextEn: '', helpTextAr: '', sortOrder, weight: 0 };
    this.fModalOpen.set(true);
  }

  openEditFactor(f: FactorDetail) {
    this.fm = {
      id: f.id, code: f.code, nameEn: f.nameEn, nameAr: f.nameAr ?? '',
      helpTextEn: f.helpTextEn ?? '', helpTextAr: f.helpTextAr ?? '',
      sortOrder: f.sortOrder, weight: f.weight,
    };
    this.fModalOpen.set(true);
  }

  async saveFactorModal() {
    const body = {
      code: this.fm.code, nameEn: this.fm.nameEn, nameAr: this.fm.nameAr || null,
      helpTextEn: this.fm.helpTextEn || null, helpTextAr: this.fm.helpTextAr || null,
      sortOrder: this.fm.sortOrder, weight: this.fm.weight,
    };
    try {
      if (this.fm.id) {
        await this.api.updateFactor(this.id, this.fm.id, body);
        this.toast.success(this.i18n.t('toast.saved'));
      } else {
        await this.api.addFactor(this.id, body);
        this.toast.success(this.i18n.t('toast.created'));
      }
      this.fModalOpen.set(false);
      const prevIds = new Set(this.v()?.factors.map(f => f.id) ?? []);
      const wasAdd = this.fm.id === null;
      await this.load();
      if (wasAdd) {
        const newIds = this.v()?.factors.filter(f => !prevIds.has(f.id)).map(f => f.id) ?? [];
        if (newIds.length) {
          const s = new Set(this.expandedIds());
          newIds.forEach(id => s.add(id));
          this.expandedIds.set(s);
        }
      }
    } catch (e) { this.fail(e); }
  }

  openAddQuestion(f: FactorDetail) {
    const sortOrder = f.questions.length > 0 ? Math.max(...f.questions.map(q => q.sortOrder)) + 1 : 1;
    this.qm = { id: null, factorId: f.id, textEn: '', textAr: '', helpTextEn: '', helpTextAr: '', questionType: 'SingleChoice', weight: 0, isRequired: true, sortOrder };
    this.qModalOpen.set(true);
  }

  openEditQuestion(f: FactorDetail, q: QuestionDetail) {
    this.qm = {
      id: q.id, factorId: f.id, textEn: q.questionTextEn, textAr: q.questionTextAr ?? '',
      helpTextEn: q.helpTextEn ?? '', helpTextAr: q.helpTextAr ?? '',
      questionType: q.questionType, weight: q.weight, isRequired: q.isRequired, sortOrder: q.sortOrder,
    };
    this.qModalOpen.set(true);
  }

  async saveQuestionModal() {
    const body = {
      questionTextEn: this.qm.textEn, questionTextAr: this.qm.textAr || null,
      helpTextEn: this.qm.helpTextEn || null, helpTextAr: this.qm.helpTextAr || null,
      questionType: this.qm.questionType, weight: this.qm.weight, isRequired: this.qm.isRequired, sortOrder: this.qm.sortOrder,
    };
    try {
      if (this.qm.id) {
        await this.api.updateQuestion(this.qm.factorId, this.qm.id, body);
        this.toast.success(this.i18n.t('toast.saved'));
      } else {
        await this.api.addQuestion(this.qm.factorId, body);
        this.toast.success(this.i18n.t('toast.created'));
      }
      this.qModalOpen.set(false);
      await this.load();
    } catch (e) { this.fail(e); }
  }

  openAddOption(q: QuestionDetail) {
    const sortOrder = q.options.length > 0 ? Math.max(...q.options.map(o => o.sortOrder)) + 1 : 1;
    this.om = { id: null, questionId: q.id, labelEn: '', labelAr: '', helpTextEn: '', helpTextAr: '', rating: 1, sortOrder };
    this.oModalOpen.set(true);
  }

  openEditOption(q: QuestionDetail, o: AnswerOption) {
    this.om = {
      id: o.id, questionId: q.id, labelEn: o.labelEn, labelAr: o.labelAr ?? '',
      helpTextEn: o.helpTextEn ?? '', helpTextAr: o.helpTextAr ?? '',
      rating: o.rating, sortOrder: o.sortOrder,
    };
    this.oModalOpen.set(true);
  }

  async saveOptionModal() {
    const body = {
      labelEn: this.om.labelEn, labelAr: this.om.labelAr || null,
      helpTextEn: this.om.helpTextEn || null, helpTextAr: this.om.helpTextAr || null,
      rating: this.om.rating, sortOrder: this.om.sortOrder,
    };
    try {
      if (this.om.id) {
        await this.api.updateOption(this.om.questionId, this.om.id, body);
        this.toast.success(this.i18n.t('toast.saved'));
      } else {
        await this.api.addOption(this.om.questionId, body);
        this.toast.success(this.i18n.t('toast.created'));
      }
      this.oModalOpen.set(false);
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
    this.egm = { gradeId: gm.gradeId, minScore: gm.minScore ?? 0, maxScore: gm.maxScore ?? 0 };
    this.rangingId.set(null);
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

  async addMapping() {
    try {
      await this.api.addGradeMapping(this.id, { gradeId: this.nm.gradeId, minScore: null, maxScore: null });
      this.nm = { gradeId: '' };
      this.toast.success(this.i18n.t('toast.saved'));
      await this.load();
    } catch (e) { this.fail(e); }
  }

  // Manual half of the two-step flow: set one already-assigned grade's range by hand (assignment
  // itself, via addMapping, no longer takes a range - see the "assign then range" split below).
  startRange(gm: GradeMapping) {
    this.erange = { minScore: gm.minScore ?? 0, maxScore: gm.maxScore ?? 0 };
    this.editId.set(null);
    this.rangingId.set(gm.id);
  }

  async saveRange(ver: MethodologyVersionDetail, gm: GradeMapping) {
    try {
      await this.api.setGradeRange(ver.id, gm.id, this.erange);
      this.rangingId.set(null);
      this.toast.success(this.i18n.t('toast.saved'));
      await this.load();
    } catch (e) { this.fail(e); }
  }

  // Automatic half: tile the point budget evenly - no gaps, no overlap - across every currently
  // assigned grade, ordered by rank. Overwrites any ranges already set.
  async autoAssignRanges(ver: MethodologyVersionDetail) {
    const ok = await this.cs.confirm({
      title: this.i18n.t('confirm.autoRange.title'),
      body: this.i18n.t('confirm.autoRange.body'),
      danger: false,
    });
    if (!ok) return;
    try {
      await this.api.autoAssignGradeRanges(ver.id);
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

function round1(n: number): number { return Math.round(n * 10) / 10; }

function rangedOnly(mappings: GradeMapping[]): (GradeMapping & { minScore: number; maxScore: number })[] {
  return mappings.filter((m): m is GradeMapping & { minScore: number; maxScore: number } =>
    m.minScore !== null && m.maxScore !== null);
}
