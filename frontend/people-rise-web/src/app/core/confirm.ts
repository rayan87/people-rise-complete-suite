import { Injectable, signal } from '@angular/core';

export interface ConfirmOptions {
  title: string;
  body?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  danger?: boolean;
}

interface Pending {
  options: ConfirmOptions;
  resolve: (v: boolean) => void;
}

@Injectable({ providedIn: 'root' })
export class ConfirmService {
  readonly pending = signal<Pending | null>(null);

  confirm(options: ConfirmOptions): Promise<boolean> {
    return new Promise(resolve => this.pending.set({ options, resolve }));
  }

  respond(result: boolean) {
    const p = this.pending();
    this.pending.set(null);
    p?.resolve(result);
  }
}
