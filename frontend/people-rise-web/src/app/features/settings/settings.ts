import { Component, inject, signal } from '@angular/core';
import { Api } from '../../core/api';
import { Session } from '../../core/session';
import { I18n } from '../../core/i18n';
import { ThemeService } from '../../core/theme';
import { ToastService } from '../../core/toast';

@Component({
  selector: 'pr-settings',
  template: `
    <h1>{{ i18n.t('settings.title') }}</h1>

    <div class="card">
      <h2>{{ i18n.t('settings.appearance') }}</h2>
      <div class="row" style="max-width:520px">
        <div class="field">
          <label>{{ i18n.t('settings.theme') }}</label>
          <div class="seg">
            <button [class.on]="theme.theme() === 'light'" (click)="theme.set('light')">{{ i18n.t('settings.light') }}</button>
            <button [class.on]="theme.theme() === 'dark'" (click)="theme.set('dark')">{{ i18n.t('settings.dark') }}</button>
          </div>
        </div>
        <div class="field">
          <label>{{ i18n.t('settings.language') }}</label>
          <div class="seg">
            <button [class.on]="i18n.lang() === 'en'" (click)="i18n.set('en')">English</button>
            <button [class.on]="i18n.lang() === 'ar'" (click)="i18n.set('ar')">العربية</button>
          </div>
        </div>
      </div>
    </div>

    <div class="card">
      <h2>{{ i18n.t('settings.data') }}</h2>
      <p>{{ i18n.t('settings.seedHint') }}</p>
      @if (error()) { <div class="alert error">{{ error() }}</div> }
      <button (click)="seed()" [disabled]="busy()">{{ i18n.t('settings.seed') }}</button>
    </div>
  `,
  styles: [`.seg { display:inline-flex; border:1px solid var(--border); border-radius:8px; overflow:hidden; }
            .seg button { background:var(--surface); color:var(--text-muted); border:none; border-radius:0; padding:.5rem .9rem; font-weight:600; }
            .seg button.on { background:var(--primary); color:var(--primary-contrast); }`],
})
export class Settings {
  private api = inject(Api);
  private session = inject(Session);
  private toast = inject(ToastService);
  readonly i18n = inject(I18n);
  readonly theme = inject(ThemeService);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  async seed() {
    this.busy.set(true); this.error.set(null);
    try {
      const r = await this.api.seedElDelta();
      await this.session.loadTenants();
      this.session.setTenant(r.id);
      this.toast.success('El-Delta demo client ready — selected as active client.');
    } catch (e: any) {
      this.error.set(e?.error?.detail ?? 'Failed to seed demo data.');
    } finally { this.busy.set(false); }
  }
}
