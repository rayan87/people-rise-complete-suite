import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { API_BASE } from './config';
import { Session } from './session';

/** Attaches the dev auth + tenant headers to every API call. */
export const sessionInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith(API_BASE)) return next(req);

  const session = inject(Session);
  const headers: Record<string, string> = {};
  if (session.userId()) headers['X-User-Id'] = session.userId();
  const tenant = session.tenantId();
  if (tenant) headers['X-Tenant-Id'] = tenant;

  return next(req.clone({ setHeaders: headers }));
};
