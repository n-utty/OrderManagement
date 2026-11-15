import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-loading-state',
  template: '{{ label() }}',
  styles: `
    :host {
      display: block;
      padding: var(--space-7) var(--space-4);
      color: var(--color-muted);
      font-size: var(--text-sm);
      text-align: center;
    }
  `,
  host: { role: 'status', 'aria-live': 'polite' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoadingState {
  readonly label = input('Loading…');
}
