import { TestBed } from '@angular/core/testing';
import { THEME_STORAGE_KEY, ThemeService } from './theme.service';

describe('ThemeService', () => {
  const root = document.documentElement;

  function stubSystemPreference(dark: boolean): void {
    Object.defineProperty(window, 'matchMedia', {
      configurable: true,
      writable: true,
      value: (query: string) => ({ matches: dark && query === '(prefers-color-scheme: dark)' }),
    });
  }

  beforeEach(() => {
    localStorage.clear();
    root.removeAttribute('data-theme');
    stubSystemPreference(false);
  });

  afterEach(() => {
    Reflect.deleteProperty(window, 'matchMedia');
  });

  it('follows the system preference when nothing is stored', () => {
    stubSystemPreference(true);

    const service = TestBed.inject(ThemeService);

    expect(service.theme()).toBe('dark');
    expect(root.getAttribute('data-theme')).toBe('dark');
  });

  it('prefers a stored choice over the system preference', () => {
    stubSystemPreference(true);
    localStorage.setItem(THEME_STORAGE_KEY, 'light');

    const service = TestBed.inject(ThemeService);

    expect(service.theme()).toBe('light');
    expect(root.getAttribute('data-theme')).toBe('light');
  });

  it('ignores an unrecognised stored value', () => {
    localStorage.setItem(THEME_STORAGE_KEY, 'sepia');

    expect(TestBed.inject(ThemeService).theme()).toBe('light');
  });

  it('does not persist the system default until the user chooses', () => {
    TestBed.inject(ThemeService);

    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBeNull();
  });

  it('toggles the theme, applies it to <html> and saves it', () => {
    const service = TestBed.inject(ThemeService);

    service.toggle();

    expect(service.theme()).toBe('dark');
    expect(root.getAttribute('data-theme')).toBe('dark');
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('dark');

    service.toggle();

    expect(service.theme()).toBe('light');
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('light');
  });
});
