import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

// Placeholder so "New order" doesn't fall through to /orders/:id; the form arrives in stage 3.
@Component({
  selector: 'app-new-order-page',
  imports: [RouterLink],
  template: `
    <h1>New order</h1>
    <p class="muted message">Order entry isn't available yet.</p>
    <a class="btn btn-secondary" routerLink="/orders">Back to orders</a>
  `,
  styles: `
    .message {
      margin: var(--space-2) 0 var(--space-5);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewOrderPage {}
