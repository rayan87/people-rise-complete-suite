import { Injectable, effect, signal } from '@angular/core';

export type Theme = 'light' | 'dark';
const KEY = 'pr.theme';

/** Light/dark theme, default light, persisted. Sets data-theme on <html>. */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly theme = signal<Theme>((localStorage.getItem(KEY) as Theme) || 'light');

  constructor() {
    effect(() => {
      const t = this.theme();
      document.documentElement.setAttribute('data-theme', t);
      localStorage.setItem(KEY, t);
    });
  }

  toggle() { this.theme.update((t) => (t === 'light' ? 'dark' : 'light')); }
  set(t: Theme) { this.theme.set(t); }
}
