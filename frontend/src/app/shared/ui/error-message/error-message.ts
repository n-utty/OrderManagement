import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  input,
  output,
} from '@angular/core';
import { ApiError } from '../../../core/http/api-error';

@Component({
  selector: 'app-error-message',
  template: `
    <div class="alert-error" role="alert">
      <p class="title">{{ heading() }}</p>
      @if (detail()) {
        <p>{{ detail() }}</p>
      }
      @if (retryable()) {
        <button type="button" class="btn btn-secondary" (click)="retry.emit()">Try again</button>
      }
    </div>
  `,
  styles: `
    :host {
      display: block;
    }

    .title {
      font-weight: 600;
    }

    p + p {
      margin-top: var(--space-1);
    }

    .btn {
      margin-top: var(--space-3);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ErrorMessage {
  readonly error = input.required<unknown>();
  readonly retryable = input(false, { transform: booleanAttribute });
  readonly retry = output();

  protected readonly heading = computed(() => {
    const error = this.error();
    if (error instanceof ApiError) return error.title;
    return 'Something went wrong.';
  });

  protected readonly detail = computed(() => {
    const error = this.error();
    return error instanceof ApiError ? error.detail : null;
  });
}
