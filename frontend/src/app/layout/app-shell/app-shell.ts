import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ThemeService } from '../../core/theme/theme.service';

@Component({
  selector: 'app-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppShell {
  protected readonly themeService = inject(ThemeService);
  protected readonly isDark = computed(() => this.themeService.theme() === 'dark');
  protected readonly toggleLabel = computed(() =>
    this.isDark() ? 'Switch to light theme' : 'Switch to dark theme',
  );
}
