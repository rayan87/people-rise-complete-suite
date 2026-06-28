import { Injectable, signal } from '@angular/core';

export interface ToastMessage {
  id: number;
  kind: 'ok' | 'error' | 'info';
  text: string;
}

let _seq = 0;

@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly messages = signal<ToastMessage[]>([]);

  success(text: string) { this._push('ok', text); }
  error(text: string) { this._push('error', text); }
  info(text: string) { this._push('info', text); }

  dismiss(id: number) {
    this.messages.update(ms => ms.filter(m => m.id !== id));
  }

  private _push(kind: ToastMessage['kind'], text: string) {
    const id = ++_seq;
    this.messages.update(ms => [...ms, { id, kind, text }]);
    setTimeout(() => this.dismiss(id), 4000);
  }
}
