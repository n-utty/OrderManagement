import { DOCUMENT, Injectable, inject, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

// Also read by the inline script in index.html; keep the two in sync.
export const THEME_STORAGE_KEY = 'om-theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly current = signal<Theme>(this.initialTheme());

  readonly theme = this.current.asReadonly();

  constructor() {
    this.apply(this.current());
  }

  setTheme(theme: Theme): void {
    this.current.set(theme);
    this.apply(theme);
    this.save(theme);
  }

  toggle(): void {
    this.setTheme(this.current() === 'dark' ? 'light' : 'dark');
  }

  private initialTheme(): Theme {
    const stored = this.read();
    if (stored === 'light' || stored === 'dark') {
      return stored;
    }
    const prefersDark = this.document.defaultView?.matchMedia?.(
      '(prefers-color-scheme: dark)',
    ).matches;
    return prefersDark ? 'dark' : 'light';
  }

  private apply(theme: Theme): void {
    this.document.documentElement.setAttribute('data-theme', theme);
  }

  // Storage can throw (private browsing, blocked site data); the theme still works for the session.
  private read(): string | null {
    try {
      return this.document.defaultView?.localStorage.getItem(THEME_STORAGE_KEY) ?? null;
    } catch {
      return null;
    }
  }

  private save(theme: Theme): void {
    try {
      this.document.defaultView?.localStorage.setItem(THEME_STORAGE_KEY, theme);
    } catch {
      // Not persisted; nothing else to do.
    }
  }
}
