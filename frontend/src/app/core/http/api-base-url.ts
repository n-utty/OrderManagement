import { InjectionToken } from '@angular/core';

// Relative so the dev-server proxy (proxy.conf.mjs) or a reverse proxy in production can route it.
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => '/api',
});
