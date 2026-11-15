import { ChangeDetectionStrategy, Component } from '@angular/core';

// Placeholder until the order list lands in the next stage.
@Component({
  selector: 'app-orders-page',
  template: `
    <h1>Orders</h1>
    <div class="panel placeholder">
      <p class="muted">Purchase orders will be listed here.</p>
    </div>
  `,
  styles: `
    .placeholder {
      margin-top: var(--space-5);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrdersPage {}
