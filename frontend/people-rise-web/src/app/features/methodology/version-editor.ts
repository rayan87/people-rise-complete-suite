import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Api } from '../../core/api';
import { I18n } from '../../core/i18n';
import { ToastService } from '../../core/toast';
import { ConfirmService } from '../../core/confirm';
import { MethodologyVersionDetail, FactorDetail, QuestionDetail, AnswerOption, Grade, GradeMapping } from '../../core/models';

@Component({
  selector: 'pr-version-editor',
  imports: [FormsModule, RouterLink],
  template: `
    <a routerLink="/methodology" class="muted">← {{ i18n.t('nav.methodology') }}</a>
    @if (error()) { <div class="alert error">{{ error() }}</div> }

    @if (v(); as ver) {
      <div class="head" style="margin-top:.5rem">
        <div>
          <h1>{{ i18n.name(ver.methodologyNameEn, ver.methodologyNameAr) }} · v{{ ver.versionNo }}</h1>
          <p><span class="badge {{ ver.status.toLowerCase() }}">{{ ver.status }}</span></p>
        </div>
        <div class="spacer"></div>
        @if (isDraft()) { <button (click)="publish()">{{ i18n.t('eval.publish') }}</button> }
      </div>
      @if (isDraft()) { <div class="alert info">{{ i18n.t('eval.draftHint') }}</div> }
      @else { <div class="alert info">{{ i18n.t('eval.readonly') }}</div> }

      <div class="card">
        <h2>{{ i18n.t('eval.factors') }}</h2>
        @for (f of ver.factors; track f.id) {
          <div class="factor">
            @if (editId() === f.id) {
              <div class="ef">
                <input [(ngModel)]="ef.code" placeholder="{{ i18n.t('common.code') }}" style="max-width:120px" />
                <input [(ngModel)]="ef.nameEn" placeholder="{{ i18n.t('common.nameEn') }}" />
                <input [(ngModel)]="ef.nameAr" placeholder="{{ i18n.t('common.nameAr') }}" dir="rtl" />
                <input type="number" [(ngModel)]="ef.sortOrder" style="max-width:70px" />
                <button class="sm" (click)="saveFactor(f)">{{ i18n.t('common.save') }}</button>
                <button class="sm subtle" (click)="editId.set(null)">{{ i18n.t('common.cancel') }}</button>
              </div>
            } @else {
              <div class="fhead">
                <h3>{{ i18n.name(f.nameEn, f.nameAr) }} <span class="faint">{{ f.code }} · #{{ f.sortOrder }}</span></h3>
                @if (isDraft()) { <button class="sm subtle" (click)="startFactor(f)">{{ i18n.t('common.edit') }}</button> }
                @if (isDraft()) { <button class="sm subtle" (click)="deleteFactor(ver, f)">{{ i18n.t('common.delete') }}</button> }
              </div>
            }

            @for (q of f.questions; track q.id) {
              <div class="question">
                @if (editId() === q.id) {
                  <div class="ef col">
                    <input [(ngModel)]="eq.textEn" placeholder="{{ i18n.t('eval.questionEn') }}" />
                    <input [(ngModel)]="eq.textAr" placeholder="{{ i18n.t('eval.questionAr') }}" dir="rtl" />
                    <input type="number" [(ngModel)]="eq.sortOrder" style="max-width:70px" />
                    <button class="sm" (click)="saveQuestion(f, q)">{{ i18n.t('common.save') }}</button>
                    <button class="sm subtle" (click)="editId.set(null)">{{ i18n.t('common.cancel') }}</button>
                  </div>
                } @else {
                  <div class="fhead">
                    <div class="q-text">{{ q.sortOrder }}. {{ i18n.name(q.questionTextEn, q.questionTextAr) }}</div>
                    @if (isDraft()) { <button class="sm subtle" (click)="startQuestion(q)">{{ i18n.t('common.edit') }}</button> }
                    @if (isDraft()) { <button class="sm subtle" (click)="deleteQuestion(f, q)">{{ i18n.t('common.delete') }}</button> }
                  </div>
                }
                <ul class="options">
                  @for (o of q.options; track o.id) {
                    <li>
                      @if (editId() === o.id) {
                        <div class="ef">
                          <input [(ngModel)]="eo.labelEn" placeholder="{{ i18n.t('eval.labelEn') }}" />
                          <input [(ngModel)]="eo.labelAr" placeholder="{{ i18n.t('eval.labelAr') }}" dir="rtl" />
                          <input type="number" [(ngModel)]="eo.points" style="max-width:80px" />
                          <button class="sm" (click)="saveOption(q, o)">{{ i18n.t('common.save') }}</button>
                          <button class="sm subtle" (click)="editId.set(null)">{{ i18n.t('common.cancel') }}</button>
                        </div>
                      } @else {
                        <span>{{ i18n.name(o.labelEn, o.labelAr) }} <span class="pts">{{ o.points }}</span></span>
                        @if (isDraft()) { <button class="sm link" (click)="startOption(o)">{{ i18n.t('common.edit') }}</button> }
                        @if (isDraft()) { <button class="sm link" (click)="deleteOption(q, o)">{{ i18n.t('common.delete') }}</button> }
                      }
                    </li>
                  } @empty { <li class="muted">{{ i18n.t('common.none') }}</li> }
                </ul>
                @if (isDraft()) {
                  <div class="ef">
                    <input [(ngModel)]="ao(q.id).labelEn" placeholder="{{ i18n.t('eval.labelEn') }}" />
                    <input [(ngModel)]="ao(q.id).labelAr" placeholder="{{ i18n.t('eval.labelAr') }}" dir="rtl" />
                    <input type="number" [(ngModel)]="ao(q.id).points" placeholder="{{ i18n.t('field.points') }}" style="max-width:90px" />
                    <button class="sm subtle" (click)="addOption(q.id, q)">+ {{ i18n.t('eval.addOption') }}</button>
                  </div>
                }
              </div>
            }
            @if (isDraft()) {
              <div class="ef addq">
                <input [(ngModel)]="aq(f.id).textEn" placeholder="{{ i18n.t('eval.questionEn') }}" />
                <input [(ngModel)]="aq(f.id).textAr" placeholder="{{ i18n.t('eval.questionAr') }}" dir="rtl" />
                <button class="sm subtle" (click)="addQuestion(f.id, f)">+ {{ i18n.t('eval.addQuestion') }}</button>
              </div>
            }
          </div>
        } @empty { <p class="muted">{{ i18n.t('common.none') }}</p> }

        @if (isDraft()) {
          <div class="ef addf">
            <input [(ngModel)]="nf.code" placeholder="{{ i18n.t('common.code') }}" style="max-width:120px" />
            <input [(ngModel)]="nf.nameEn" placeholder="{{ i18n.t('common.nameEn') }}" />
            <input [(ngModel)]="nf.nameAr" placeholder="{{ i18n.t('common.nameAr') }}" dir="rtl" />
            <input type="number" [(ngModel)]="nf.sortOrder" placeholder="#" style="max-width:70px" />
            <button class="sm" (click)="addFactor()">+ {{ i18n.t('eval.addFactor') }}</button>
          </div>
        }
      </div>

      <div class="card">
        <h2>{{ i18n.t('eval.gradeMappings') }}</h2>

        @if (mappingWarnings().length > 0) {
          <div class="alert info" style="margin-bottom:.75rem">
            @for (w of mappingWarnings(); track w) {
              <div>⚠ {{ w }}</div>
            }
          </div>
        }

        <table>
          <thead>
            <tr>
              <th>{{ i18n.t('field.grade') }}</th>
              <th>{{ i18n.t('salary.min') }}</th>
              <th>{{ i18n.t('salary.max') }}</th>
              @if (isDraft()) { <th></th> }
            </tr>
          </thead>
          <tbody>
            @for (gm of ver.gradeMappings; track gm.id) {
              @if (editId() === gm.id) {
                <tr>
                  <td>
                    <select [(ngModel)]="egm.gradeId" style="width:auto">
                      @for (g of grades(); track g.id) { <option [ngValue]="g.id">{{ g.code }}</option> }
                    </select>
                  </td>
                  <td><input type="number" [(ngModel)]="egm.minScore" style="max-width:80px" /></td>
                  <td><input type="number" [(ngModel)]="egm.maxScore" style="max-width:80px" /></td>
                  <td style="white-space:nowrap">
                    <button class="sm" (click)="saveMapping(ver, gm)">{{ i18n.t('common.save') }}</button>
                    <button class="sm subtle" (click)="editId.set(null)">{{ i18n.t('common.cancel') }}</button>
                  </td>
                </tr>
              } @else {
                <tr>
                  <td>{{ gm.gradeCode }}</td>
                  <td>{{ gm.minScore }}</td>
                  <td>{{ gm.maxScore }}</td>
                  @if (isDraft()) {
                    <td style="text-align:end; white-space:nowrap">
                      <button class="sm subtle" (click)="startMapping(gm)">{{ i18n.t('common.edit') }}</button>
                      <button class="sm subtle" (click)="deleteMapping(ver, gm)">{{ i18n.t('common.delete') }}</button>
                    </td>
                  }
                </tr>
              }
            }
            @empty { <tr><td colspan="4" class="muted">{{ i18n.t('common.none') }}</td></tr> }
          </tbody>
        </table>
        @if (isDraft()) {
          <div class="ef" style="margin-top:.6rem">
            <select [(ngModel)]="nm.gradeId"><option [ngValue]="''">{{ i18n.t('field.grade') }}</option>@for (g of grades(); track g.id) { <option [ngValue]="g.id">{{ g.code }}</option> }</select>
            <input type="number" [(ngModel)]="nm.minScore" placeholder="{{ i18n.t('salary.min') }}" style="max-width:90px" />
            <input type="number" [(ngModel)]="nm.maxScore" placeholder="{{ i18n.t('salary.max') }}" style="max-width:90px" />
            <button class="sm subtle" (click)="addMapping()">+ {{ i18n.t('eval.addMapping') }}</button>
          </div>
        }
      </div>
    } @else if (!error()) { <p>{{ i18n.t('common.loading') }}</p> }
  `,
  styles: [`
    .factor { border-inline-start:3px solid var(--primary-soft); padding-inline-start:1rem; margin-bottom:1.5rem; }
    .fhead { display:flex; align-items:center; gap:.75rem; }
    .fhead h3, .fhead .q-text { margin:.2rem 0; }
    .fhead button { margin-inline-start:auto; }
    .question { background:var(--surface-2); border-radius:8px; padding:.7rem .9rem; margin:.6rem 0; }
    .q-text { font-weight:600; font-size:.92rem; }
    .options { margin:.4rem 0; padding-inline-start:1.1rem; }
    .options li { font-size:.88rem; margin:.25rem 0; display:flex; align-items:center; gap:.5rem; }
    .pts { color:var(--primary); font-weight:700; font-size:.8rem; }
    button.sm.link { background:none; border:none; color:var(--primary); padding:0 .3rem; font-size:.78rem; }
    .ef { display:flex; gap:.5rem; align-items:center; flex-wrap:wrap; margin-top:.5rem; }
    .ef input { flex:1; min-width:120px; }
    .addq { margin-top:.6rem; }
    .addf { margin-top:1rem; border-top:1px dashed var(--border); padding-top:1rem; }
  `],
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

  nf = { code: '', nameEn: '', nameAr: '', sortOrder: 1 };
  nm = { gradeId: '', minScore: 0, maxScore: 0 };
  ef = { code: '', nameEn: '', nameAr: '', sortOrder: 1 };
  eq = { textEn: '', textAr: '', sortOrder: 1 };
  eo = { labelEn: '', labelAr: '', points: 0 };
  egm = { gradeId: '', minScore: 0, maxScore: 0 };
  private aqf: Record<string, { textEn: string; textAr: string }> = {};
  private aof: Record<string, { labelEn: string; labelAr: string; points: number }> = {};

  /** Client-side validation: gaps, overlaps, inverted ranges in grade mappings. */
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

  private get id() { return this.route.snapshot.paramMap.get('id')!; }
  constructor() { this.load(); }
  isDraft() { return this.v()?.status === 'Draft'; }
  aq(fid: string) { return (this.aqf[fid] ??= { textEn: '', textAr: '' }); }
  ao(qid: string) { return (this.aof[qid] ??= { labelEn: '', labelAr: '', points: 0 }); }

  private async load() {
    this.error.set(null);
    try {
      const [ver, grades] = await Promise.all([this.api.version(this.id), this.api.grades()]);
      this.v.set(ver); this.grades.set(grades);
    } catch (e: any) { this.error.set(e?.error?.detail ?? 'Failed to load version.'); }
  }
  private fail(e: any) { this.toast.error(e?.error?.detail ?? 'Request failed.'); }

  async addFactor() {
    const ver = this.v();
    const sortOrder = ver && ver.factors.length > 0
      ? Math.max(...ver.factors.map(f => f.sortOrder)) + 1 : 1;
    try {
      await this.api.addFactor(this.id, { code: this.nf.code, nameEn: this.nf.nameEn, nameAr: this.nf.nameAr || null, sortOrder, weight: null });
      this.nf = { code: '', nameEn: '', nameAr: '', sortOrder: 1 };
      this.toast.success(this.i18n.t('toast.created'));
      await this.load();
    } catch (e) { this.fail(e); }
  }

  async addQuestion(fid: string, factor: FactorDetail) {
    const f = this.aq(fid);
    const sortOrder = factor.questions.length > 0
      ? Math.max(...factor.questions.map(q => q.sortOrder)) + 1 : 1;
    try {
      await this.api.addQuestion(fid, { questionTextEn: f.textEn, questionTextAr: f.textAr || null, helpTextEn: null, helpTextAr: null, sortOrder });
      this.aqf[fid] = { textEn: '', textAr: '' };
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

  startFactor(f: FactorDetail) { this.ef = { code: f.code, nameEn: f.nameEn, nameAr: f.nameAr ?? '', sortOrder: f.sortOrder }; this.editId.set(f.id); }
  async saveFactor(f: FactorDetail) {
    try {
      await this.api.updateFactor(this.id, f.id, { code: this.ef.code, nameEn: this.ef.nameEn, nameAr: this.ef.nameAr || null, sortOrder: this.ef.sortOrder, weight: f.weight });
      this.editId.set(null);
      this.toast.success(this.i18n.t('toast.saved'));
      await this.load();
    } catch (e) { this.fail(e); }
  }

  startQuestion(q: QuestionDetail) { this.eq = { textEn: q.questionTextEn, textAr: q.questionTextAr ?? '', sortOrder: q.sortOrder }; this.editId.set(q.id); }
  async saveQuestion(f: FactorDetail, q: QuestionDetail) {
    try {
      await this.api.updateQuestion(f.id, q.id, { questionTextEn: this.eq.textEn, questionTextAr: this.eq.textAr || null, helpTextEn: q.helpTextEn, helpTextAr: q.helpTextAr, sortOrder: this.eq.sortOrder });
      this.editId.set(null);
      this.toast.success(this.i18n.t('toast.saved'));
      await this.load();
    } catch (e) { this.fail(e); }
  }

  startOption(o: AnswerOption) { this.eo = { labelEn: o.labelEn, labelAr: o.labelAr ?? '', points: o.points }; this.editId.set(o.id); }
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
