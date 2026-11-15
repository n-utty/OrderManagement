import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found-page',
  imports: [RouterLink],
  template: `
    <h1>Page not found</h1>
    <p class="muted message">The page you were looking for doesn't exist or has moved.</p>
    <a class="btn btn-secondary" routerLink="/orders">Back to orders</a>
  `,
  styles: `
    .message {
      margin: var(--space-2) 0 var(--space-5);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotFoundPage {}
