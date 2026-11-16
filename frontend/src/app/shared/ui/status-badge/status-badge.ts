import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import type { OrderStatus } from '../../../features/purchase-orders/models';

// One muted hue per status, each tinted against the background so badges sit on the surface:
// amber (pending), accent blue (confirmed), green (fulfilled), grey (cancelled). Red stays for errors.
// Text colours were checked for WCAG AA (4.5:1) against their badge background in both themes.
@Component({
  selector: 'app-status-badge',
  template: '{{ status() }}',
  styles: `
    :host {
      display: inline-block;
      padding: 0.0625rem var(--space-2);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-sm);
      background: var(--color-hover);
      color: color-mix(in srgb, var(--color-ink) 72%, var(--color-bg));
      font-size: var(--text-xs);
      font-weight: 500;
      line-height: 1.5;
      white-space: nowrap;
    }

    :host([data-status='Pending']) {
      border-color: color-mix(in srgb, var(--color-pending) 35%, var(--color-bg));
      background: var(--color-pending-tint);
      color: var(--color-pending);
    }

    :host([data-status='Confirmed']) {
      border-color: color-mix(in srgb, var(--color-accent) 35%, var(--color-bg));
      background: var(--color-accent-tint);
      color: var(--color-accent);
    }

    :host([data-status='Fulfilled']) {
      border-color: color-mix(in srgb, var(--color-fulfilled) 35%, var(--color-bg));
      background: var(--color-fulfilled-tint);
      color: var(--color-fulfilled);
    }
  `,
  host: { '[attr.data-status]': 'status()' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusBadge {
  readonly status = input.required<OrderStatus>();
}
