import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { API_BASE, DEV_USER_ID } from './config';
import { TenantAccess } from './models';

const USER_KEY = 'pr.userId';
const TENANT_KEY = 'pr.tenantId';

/**
 * Holds the dev identity + active tenant. In this starter there is no real auth: the user id is a
 * header (X-User-Id) and the tenant is chosen from the caller's access grants (X-Tenant-Id).
 */
@Injectable({ providedIn: 'root' })
export class Session {
  readonly userId = signal<string>(localStorage.getItem(USER_KEY) ?? DEV_USER_ID);
  readonly tenantId = signal<string | null>(localStorage.getItem(TENANT_KEY));
  readonly tenants = signal<TenantAccess[]>([]);

  readonly activeTenant = computed(() =>
    this.tenants().find((t) => t.tenantId === this.tenantId()) ?? null);

  readonly hasTenant = computed(() => !!this.tenantId());

  constructor(private http: HttpClient) {}

  setUserId(id: string) {
    this.userId.set(id.trim());
    localStorage.setItem(USER_KEY, id.trim());
  }

  setTenant(id: string | null) {
    this.tenantId.set(id);
    if (id) localStorage.setItem(TENANT_KEY, id);
    else localStorage.removeItem(TENANT_KEY);
  }

  async loadTenants(): Promise<void> {
    const rows = await firstValueFrom(
      this.http.get<TenantAccess[]>(`${API_BASE}/me/tenants`));
    this.tenants.set(rows);
    // Auto-select if nothing chosen yet and exactly one is available.
    if (!this.tenantId() && rows.length === 1) this.setTenant(rows[0].tenantId);
  }
}
