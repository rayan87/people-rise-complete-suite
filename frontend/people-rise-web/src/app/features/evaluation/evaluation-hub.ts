import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Api } from '../../core/api';
import { Session } from '../../core/session';
import { I18n } from '../../core/i18n';
import { EvaluationListItem } from '../../core/models';

@Component({
  selector: 'pr-evaluation-hub',
  imports: [RouterLink, DatePipe, FormsModule],
  styles: [`
    .sort-bar { display:flex; align-items:center; gap:.5rem; margin-bottom:.75rem; }
    .sort-bar label { margin:0; font-size:.8rem; }
    .sort-bar select { width:auto; font-size:.8rem; padding:.3rem .5rem; }
    .rank-col { width:44px; text-align:center; }
    .rank-num { display:inline-flex; align-items:center; justify-content:center;
                width:26px; height:26px; border-radius:50%; background:var(--primary-soft);
                color:var(--primary); font-size:.78rem; font-weight:600; }
  `],
  template: `
    <div class="head">
      <div><h1>{{ i18n.t('nav.evaluation') }}</h1></div>
      <div class="spacer"></div>
      <button (click)="router.navigate(['/evaluation/new'])">
        <i class="ti ti-plus"></i> {{ i18n.t('eval.newEvaluation') }}
      </button>
    </div>

    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (!session.hasTenant()) { <div class="alert info">{{ i18n.t('common.selectClient') }}</div> }

    <div class="card">
      @if (loading()) {
        <p class="muted">{{ i18n.t('common.loading') }}</p>
      } @else if (evaluations().length === 0) {
        <p class="empty">{{ i18n.t('common.none') }} — <a routerLink="/evaluation/new">start an evaluation →</a></p>
      } @else {
        <!-- Sort control -->
        <div class="sort-bar">
          <label>Sort:</label>
          <select [ngModel]="sortBy()" (ngModelChange)="sortBy.set($event)">
            <option value="date">{{ i18n.t('eval.sortDate') }}</option>
            <option value="score">{{ i18n.t('eval.sortScore') }}</option>
          </select>
        </div>
        <table>
          <thead>
            <tr>
              @if (sortBy() === 'score') { <th class="rank-col">{{ i18n.t('eval.rank') }}</th> }
              <th>{{ i18n.t('field.title') }}</th>
              <th>Code</th>
              <th>Score</th>
              <th>{{ i18n.t('field.grade') }}</th>
              <th>{{ i18n.t('common.status') }}</th>
              <th>Date</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (e of sorted(); track e.id) {
              <tr>
                @if (sortBy() === 'score') {
                  <td class="rank-col">
                    @if (scoreRanks().get(e.id); as r) {
                      <span class="rank-num">{{ r }}</span>
                    } @else { <span class="faint">—</span> }
                  </td>
                }
                <td><strong>{{ i18n.name(e.jobTitleEn, e.jobTitleAr) }}</strong></td>
                <td class="faint">{{ e.jobCode }}</td>
                <td>{{ e.totalScore ?? '—' }}</td>
                <td>
                  @if (e.recommendedGradeCode) {
                    <span class="badge approved">{{ e.recommendedGradeCode }}</span>
                  } @else { <span class="faint">—</span> }
                </td>
                <td><span class="badge {{ e.status.toLowerCase() }}">{{ e.status }}</span></td>
                <td class="faint">{{ e.createdAt | date:'d MMM yyyy' }}</td>
                <td style="text-align:end">
                  <a [routerLink]="['/evaluation', e.id]">{{ i18n.t('common.open') }} →</a>
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    </div>
  `,
})
export class EvaluationHub {
  private api = inject(Api);
  readonly router = inject(Router);
  readonly session = inject(Session);
  readonly i18n = inject(I18n);
  readonly evaluations = signal<EvaluationListItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly sortBy = signal<'date' | 'score'>('date');

  /** Sorted list: by score desc (nulls last) when sortBy=score, else original load order. */
  readonly sorted = computed(() => {
    const evals = this.evaluations();
    if (this.sortBy() !== 'score') return evals;
    return [...evals].sort((a, b) => {
      if (a.totalScore === null && b.totalScore === null) return 0;
      if (a.totalScore === null) return 1;
      if (b.totalScore === null) return -1;
      return b.totalScore - a.totalScore;
    });
  });

  /** Rank map: only Submitted/Approved rows with a score get a rank position. */
  readonly scoreRanks = computed(() => {
    const scored = this.evaluations()
      .filter(e => e.totalScore !== null && (e.status === 'Submitted' || e.status === 'Approved'))
      .sort((a, b) => b.totalScore! - a.totalScore!);
    const map = new Map<string, number>();
    scored.forEach((e, i) => map.set(e.id, i + 1));
    return map;
  });

  constructor() { effect(() => { this.session.tenantId(); this.load(); }); }

  private async load() {
    if (!this.session.hasTenant()) { this.evaluations.set([]); return; }
    this.loading.set(true); this.error.set(null);
    try { this.evaluations.set(await this.api.evaluations()); }
    catch (e: any) { this.error.set(e?.error?.detail ?? 'Failed to load evaluations.'); }
    finally { this.loading.set(false); }
  }
}
