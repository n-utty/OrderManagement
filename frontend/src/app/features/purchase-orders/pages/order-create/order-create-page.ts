import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  afterNextRender,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiError } from '../../../../core/http/api-error';
import { ErrorMessage } from '../../../../shared/ui/error-message/error-message';
import { LineItemsEditor } from '../../components/line-items-editor/line-items-editor';
import { PurchaseOrdersApi } from '../../data-access/purchase-orders.api';
import {
  applyServerErrors,
  createOrderForm,
  errorMessage,
  toRequest,
} from '../../forms/order-form';
import { CURRENCIES, ORDER_LIMITS } from '../../forms/order-form.validators';
import { OrderNavigationState } from '../../order-navigation-state';

type SubmitFailure =
  | { kind: 'invalid'; title: string; messages: string[] }
  | { kind: 'conflict'; message: string; existingOrderId: string | null }
  | { kind: 'network' }
  | { kind: 'error'; error: unknown };

@Component({
  selector: 'app-order-create-page',
  imports: [ErrorMessage, LineItemsEditor, ReactiveFormsModule, RouterLink],
  templateUrl: './order-create-page.html',
  styleUrl: './order-create-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderCreatePage {
  private readonly api = inject(PurchaseOrdersApi);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);
  private readonly host: ElementRef<HTMLElement> = inject(ElementRef);

  protected readonly form = createOrderForm(inject(NonNullableFormBuilder));
  protected readonly currencies = CURRENCIES;
  protected readonly limits = ORDER_LIMITS;

  protected readonly submitting = signal(false);
  protected readonly submitAttempts = signal(0);
  protected readonly failure = signal<SubmitFailure | null>(null);

  protected submit(): void {
    // The button is disabled while in flight; this also covers Enter pressed in a field.
    if (this.submitting()) return;

    this.submitAttempts.update((count) => count + 1);
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.showFailure({ kind: 'invalid', title: 'Some fields need attention.', messages: [] });
      return;
    }

    this.submitting.set(true);
    this.failure.set(null);
    this.api
      .submit(toRequest(this.form.getRawValue()))
      .pipe(
        finalize(() => this.submitting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: ({ order, created }) => {
          const state: OrderNavigationState = { banner: created ? 'created' : 'already-submitted' };
          void this.router.navigate(['/orders', order.id], { state });
        },
        error: (error: unknown) => this.showFailure(this.toFailure(error)),
      });
  }

  protected error(control: AbstractControl): string | null {
    const visible = control.invalid && (control.touched || this.submitAttempts() > 0);
    return visible ? errorMessage(control) : null;
  }

  private toFailure(error: unknown): SubmitFailure {
    if (!(error instanceof ApiError)) return { kind: 'error', error };
    if (error.isNetworkError) return { kind: 'network' };
    if (error.status === 409) {
      return {
        kind: 'conflict',
        message: error.detail ?? error.title,
        existingOrderId: error.existingOrderId,
      };
    }
    if (error.status === 400) {
      const unplaced = applyServerErrors(this.form, error.fieldErrors);
      return { kind: 'invalid', title: error.title, messages: unplaced };
    }
    return { kind: 'error', error };
  }

  // Moves focus to the summary so keyboard and screen-reader users land on what went wrong.
  private showFailure(failure: SubmitFailure): void {
    this.failure.set(failure);
    afterNextRender(
      () => this.host.nativeElement.querySelector<HTMLElement>('#submit-feedback')?.focus(),
      { injector: this.injector },
    );
  }
}
