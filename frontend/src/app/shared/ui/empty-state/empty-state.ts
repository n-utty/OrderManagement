import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** A titled message with an optional action projected below it. */
@Component({
  selector: 'app-empty-state',
  template: `
    <h2>{{ title() }}</h2>
    @if (message()) {
      <p class="muted">{{ message() }}</p>
    }
    <div class="actions"><ng-content /></div>
  `,
  styles: `
    :host {
      display: block;
      padding: var(--space-7) var(--space-5);
      background: var(--color-surface);
      border: 1px dashed var(--color-border-strong);
      border-radius: var(--radius);
      text-align: center;
    }

    h2 {
      font-size: var(--text-md);
    }

    p {
      margin-top: var(--space-2);
      font-size: var(--text-sm);
    }

    .actions:not(:empty) {
      margin-top: var(--space-5);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmptyState {
  readonly title = input.required<string>();
  readonly message = input<string>();
}
