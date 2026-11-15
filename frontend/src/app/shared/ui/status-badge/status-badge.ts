import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import type { OrderStatus } from '../../../features/purchase-orders/models';

// Statuses are told apart by tint strength of the theme colours rather than new hues:
// neutral (pending) → accent tint (confirmed) → stronger accent (fulfilled) → muted outline (cancelled).
@Component({
  selector: 'app-status-badge',
  template: '{{ status() }}',
  styles: `
    :host {
      display: inline-block;
      padding: 0.0625rem var(--space-2);
      border: 1px solid var(--color-border-strong);
      border-radius: var(--radius-sm);
      background: var(--color-hover);
      color: var(--color-ink);
      font-size: var(--text-xs);
      font-weight: 500;
      line-height: 1.5;
      white-space: nowrap;
    }

    :host([data-status='Confirmed']) {
      border-color: color-mix(in srgb, var(--color-accent) 35%, var(--color-bg));
      background: var(--color-accent-tint);
      color: var(--color-accent);
    }

    :host([data-status='Fulfilled']) {
      border-color: color-mix(in srgb, var(--color-accent) 60%, var(--color-bg));
      background: color-mix(in srgb, var(--color-accent) 24%, var(--color-bg));
      color: var(--color-accent-strong);
    }

    :host([data-status='Cancelled']) {
      border-color: var(--color-border);
      background: transparent;
      color: var(--color-muted);
    }
  `,
  host: { '[attr.data-status]': 'status()' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusBadge {
  readonly status = input.required<OrderStatus>();
}
