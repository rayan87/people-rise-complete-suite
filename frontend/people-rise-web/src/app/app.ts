import { Component, computed, effect, HostListener, inject, OnInit, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { filter, map } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { Session } from './core/session';
import { ThemeService } from './core/theme';
import { I18n } from './core/i18n';
import { Api } from './core/api';
import { ToastService } from './core/toast';
import { ConfirmService } from './core/confirm';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  readonly session = inject(Session);
  readonly theme  = inject(ThemeService);
  readonly i18n   = inject(I18n);
  readonly toast  = inject(ToastService);
  readonly confirm = inject(ConfirmService);
  private  api    = inject(Api);
  private  router = inject(Router);

  readonly error        = signal<string | null>(null);
  readonly pendingCount = signal(0);

  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd),
      map(e => (e as NavigationEnd).urlAfterRedirects),
    ),
    { initialValue: this.router.url },
  );

  readonly pageTitle = computed(() => {
    const url = this.currentUrl() ?? '';
    if (url.startsWith('/evaluation'))        return this.i18n.t('nav.evaluation');
    if (url.startsWith('/methodology'))       return this.i18n.t('nav.methodology');
    if (url.startsWith('/salary'))            return this.i18n.t('nav.salary');
    if (url.startsWith('/jobs'))              return this.i18n.t('nav.jobLibrary');
    if (url === '/grading')                   return this.i18n.t('nav.grading');
    if (url === '/settings/structure')        return this.i18n.t('nav.taxonomy');
    if (url.startsWith('/settings'))          return this.i18n.t('nav.settings');
    return this.i18n.t('nav.dashboard');
  });

  readonly userInitials = computed(() => {
    const id = this.session.userId();
    return id.replace(/-/g, '').slice(-4, -2).toUpperCase() || 'PR';
  });

  constructor() {
    effect(() => {
      const tenantId = this.session.tenantId();
      if (tenantId) this.loadPendingCount();
      else this.pendingCount.set(0);
    });
  }

  async ngOnInit() {
    try { await this.session.loadTenants(); }
    catch { this.error.set('Could not reach the API on http://localhost:5080. Is it running?'); }
  }

  onTenantChange(id: string) { this.session.setTenant(id || null); }

  async refreshTenants() {
    this.error.set(null);
    try { await this.session.loadTenants(); }
    catch { this.error.set('Could not load tenants — check the API and your X-User-Id.'); }
  }

  @HostListener('document:keydown.escape')
  onEscape() { if (this.confirm.pending()) this.confirm.respond(false); }

  private async loadPendingCount() {
    try {
      const evals = await this.api.evaluations();
      this.pendingCount.set(evals.filter(e => e.status !== 'Approved').length);
    } catch {
      this.pendingCount.set(0);
    }
  }
}
