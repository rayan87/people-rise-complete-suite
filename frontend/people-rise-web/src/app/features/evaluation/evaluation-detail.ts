import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Api } from '../../core/api';
import { I18n } from '../../core/i18n';
import { ToastService } from '../../core/toast';
import { EvaluationResult, MethodologyVersionDetail, AnswerSelection } from '../../core/models';

@Component({
  selector: 'pr-evaluation-detail',
  imports: [RouterLink],
  template: `
    <a routerLink="/evaluation" class="muted">← {{ i18n.t('nav.evaluation') }}</a>
    @if (error()) { <div class="alert error">{{ error() }}</div> }

    @if (evaluation(); as ev) {
      <div class="head" style="margin-top:.5rem">
        <div>
          <h1>{{ i18n.name(ev.jobTitleEn, ev.jobTitleAr) }} <span class="faint">{{ ev.jobCode }}</span></h1>
          <p><span class="badge {{ ev.status.toLowerCase() }}">{{ ev.status }}</span></p>
        </div>
        <div class="spacer"></div>
        @if (ev.status === 'Submitted') { <button (click)="approve()" [disabled]="busy()">{{ i18n.t('eval.approve') }}</button> }
      </div>

      @if (ev.status === 'Draft') {
        @if (version(); as ver) {
          <div class="card">
            <h2>{{ i18n.t('eval.questionnaire') }} — {{ i18n.name(ver.methodologyNameEn, ver.methodologyNameAr) }}</h2>
            @for (f of ver.factors; track f.id) {
              <div class="factor">
                <h3>{{ i18n.name(f.nameEn, f.nameAr) }}</h3>
                @if (f.helpTextEn || f.helpTextAr) {
                  <div class="help-text">{{ i18n.name(f.helpTextEn, f.helpTextAr) }}</div>
                }
                @for (q of f.questions; track q.id) {
                  <div class="question">
                    <div class="q-text">{{ i18n.name(q.questionTextEn, q.questionTextAr) }}</div>
                    @if (q.helpTextEn || q.helpTextAr) {
                      <div class="help-text">{{ i18n.name(q.helpTextEn, q.helpTextAr) }}</div>
                    }
                    <div class="opts">
                      @if (q.questionType === 'MultipleChoice') {
                        @for (o of q.options; track o.id) {
                          <label class="opt">
                            <input type="checkbox" [checked]="isSelected(q.id, o.id)" (change)="toggle(q.id, o.id)" />
                            <span>
                              {{ i18n.name(o.labelEn, o.labelAr) }}
                              @if (o.helpTextEn || o.helpTextAr) {
                                <div class="help-text">{{ i18n.name(o.helpTextEn, o.helpTextAr) }}</div>
                              }
                            </span>
                          </label>
                        }
                      } @else {
                        @for (o of q.options; track o.id) {
                          <label class="opt">
                            <input type="radio" [name]="q.id" [checked]="isSelected(q.id, o.id)" (change)="select(q.id, o.id)" />
                            <span>
                              {{ i18n.name(o.labelEn, o.labelAr) }}
                              @if (o.helpTextEn || o.helpTextAr) {
                                <div class="help-text">{{ i18n.name(o.helpTextEn, o.helpTextAr) }}</div>
                              }
                            </span>
                          </label>
                        }
                      }
                    </div>
                  </div>
                }
              </div>
            }
            <div class="submit-bar">
              <span class="muted">{{ answeredCount() }} / {{ totalQuestions() }} {{ i18n.t('eval.answered') }}</span>
              <button (click)="submit()" [disabled]="!allAnswered() || busy()">{{ i18n.t('eval.submit') }}</button>
            </div>
          </div>
        } @else if (!error()) { <p>{{ i18n.t('common.loading') }}</p> }
      }

      @if (ev.status !== 'Draft') {
        <div class="row">
          <div class="card sc"><div class="big">{{ ev.totalScore }}</div><div class="muted">{{ i18n.t('eval.score') }}</div></div>
          <div class="card sc"><div class="big grade">{{ ev.recommendedGradeCode ?? '—' }}</div><div class="muted">{{ i18n.name(ev.recommendedGradeNameEn, ev.recommendedGradeNameAr) }}</div></div>
        </div>
        <div class="card">
          <h2>{{ i18n.t('eval.breakdown') }}</h2>
          <table>
            <thead><tr><th>{{ i18n.t('eval.factors') }}</th><th>{{ i18n.t('field.points') }}</th></tr></thead>
            <tbody>@for (fs of ev.factorScores; track fs.factorId) { <tr><td>{{ i18n.name(fs.factorNameEn, fs.factorNameAr) }}</td><td>{{ fs.score }}</td></tr> }</tbody>
          </table>
        </div>
        <div class="card">
          <h2>{{ i18n.t('eval.audit') }}</h2>
          <table>
            <thead><tr><th>{{ i18n.t('eval.questionnaire') }}</th><th>{{ i18n.t('eval.labelEn') }}</th><th>{{ i18n.t('eval.rating') }}</th></tr></thead>
            <tbody>@for (a of ev.answers; track a.questionId) { <tr><td>{{ i18n.name(a.questionTextEn, a.questionTextAr) }}</td><td>{{ i18n.name(a.answerLabelEn, a.answerLabelAr) }}</td><td>{{ a.ratingSnapshot }}</td></tr> }</tbody>
          </table>
        </div>
      }
    } @else if (!error()) { <p>{{ i18n.t('common.loading') }}</p> }
  `,
  styles: [`
    .factor { margin-bottom:1.25rem; }
    .question { background:var(--surface-2); border-radius:8px; padding:.7rem .9rem; margin:.5rem 0; }
    .q-text { font-weight:600; }
    .help-text { font-size:.82rem; color:var(--text-faint); font-weight:400; line-height:1.4; margin-top:2px; }
    .opts { display:flex; flex-direction:column; gap:.35rem; margin-top:.4rem; }
    .opt { display:flex; align-items:flex-start; gap:.5rem; font-weight:400; color:var(--text); cursor:pointer; }
    .opt input { width:auto; margin-top:3px; }
    .submit-bar { display:flex; align-items:center; gap:1rem; margin-top:1rem; }
    .submit-bar button { margin-inline-start:auto; }
    .sc { text-align:center; }
    .big { font-size:2.4rem; font-weight:800; color:var(--primary); }
    .big.grade { color:var(--success); }
  `],
})
export class EvaluationDetail {
  private api = inject(Api);
  private route = inject(ActivatedRoute);
  private toast = inject(ToastService);
  readonly i18n = inject(I18n);
  readonly evaluation = signal<EvaluationResult | null>(null);
  readonly version = signal<MethodologyVersionDetail | null>(null);
  readonly selections = signal<Record<string, string[]>>({});
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);

  readonly totalQuestions = computed(() => (this.version()?.factors ?? []).reduce((n, f) => n + f.questions.length, 0));
  readonly answeredCount = computed(() => Object.values(this.selections()).filter((ids) => ids.length > 0).length);
  readonly allAnswered = computed(() => this.totalQuestions() > 0 && this.answeredCount() === this.totalQuestions());

  private get id() { return this.route.snapshot.paramMap.get('id')!; }
  constructor() { this.load(); }

  private async load() {
    this.error.set(null);
    try {
      const ev = await this.api.evaluation(this.id);
      this.evaluation.set(ev);
      if (ev.status === 'Draft') this.version.set(await this.api.version(ev.methodologyVersionId));
    } catch (e: any) { this.error.set(e?.error?.detail ?? 'Failed to load evaluation.'); }
  }

  isSelected(q: string, o: string) { return (this.selections()[q] ?? []).includes(o); }
  select(q: string, o: string) { this.selections.update((s) => ({ ...s, [q]: [o] })); }
  toggle(q: string, o: string) {
    this.selections.update((s) => {
      const current = s[q] ?? [];
      const next = current.includes(o) ? current.filter((id) => id !== o) : [...current, o];
      return { ...s, [q]: next };
    });
  }

  async submit() {
    if (!this.allAnswered()) return;
    this.busy.set(true); this.error.set(null);
    try {
      const answers: AnswerSelection[] = Object.entries(this.selections())
        .filter(([, answerOptionIds]) => answerOptionIds.length > 0)
        .map(([questionId, answerOptionIds]) => ({ questionId, answerOptionIds }));
      this.evaluation.set(await this.api.submitAnswers(this.id, answers));
      this.toast.success(this.i18n.t('toast.submitted'));
    } catch (e: any) { this.toast.error(e?.error?.detail ?? 'Failed to submit.'); }
    finally { this.busy.set(false); }
  }

  async approve() {
    this.busy.set(true); this.error.set(null);
    try {
      this.evaluation.set(await this.api.approve(this.id));
      this.toast.success(this.i18n.t('toast.approved'));
    } catch (e: any) { this.toast.error(e?.error?.detail ?? 'Failed to approve.'); }
    finally { this.busy.set(false); }
  }
}
