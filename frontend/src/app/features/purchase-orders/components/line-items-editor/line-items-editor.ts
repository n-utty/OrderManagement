import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormArray,
  NonNullableFormBuilder,
  ReactiveFormsModule,
} from '@angular/forms';
import { startWith, switchMap } from 'rxjs';
import { LineItemForm, createLineItem, errorMessage } from '../../forms/order-form';
import { ORDER_LIMITS, normaliseSku } from '../../forms/order-form.validators';

@Component({
  selector: 'app-line-items-editor',
  imports: [DecimalPipe, ReactiveFormsModule],
  templateUrl: './line-items-editor.html',
  styleUrl: './line-items-editor.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LineItemsEditor {
  private readonly fb = inject(NonNullableFormBuilder);

  readonly items = input.required<FormArray<LineItemForm>>();
  readonly currency = input('');
  /** Bumped by the form on every submit attempt, so untouched errors show and server errors render. */
  readonly submitAttempts = input(0);

  protected readonly maxLines = ORDER_LIMITS.maxLineItems;

  // Reactive forms aren't signal-based. Turning the array's events into a signal lets this OnPush
  // view refresh when something outside it changes the form, such as errors from the server.
  private readonly formEvents = toSignal(
    toObservable(this.items).pipe(switchMap((items) => items.events.pipe(startWith(null)))),
  );

  // Estimated in cents to avoid floating-point drift; the server's totals are authoritative.
  protected readonly estimate = computed(() => {
    this.formEvents();
    const lines = this.items().controls.map(({ controls: { quantity, unitPrice } }) =>
      quantity.valid && unitPrice.valid
        ? (quantity.value! * Math.round(unitPrice.value! * 100)) / 100
        : null,
    );
    const subtotal = lines.every((line) => line !== null)
      ? lines.reduce<number>((sum, line) => sum + line, 0)
      : null;
    return { lines, subtotal };
  });

  protected add(): void {
    this.items().push(createLineItem(this.fb));
  }

  protected remove(index: number): void {
    this.items().removeAt(index);
  }

  protected error(control: AbstractControl): string | null {
    return this.shouldShow(control) ? errorMessage(control) : null;
  }

  protected skuError(line: LineItemForm): string | null {
    const own = this.error(line.controls.sku);
    if (own) return own;
    const duplicates: string[] = this.items().errors?.['duplicateSkus'] ?? [];
    const sku = normaliseSku(line.controls.sku.value);
    return this.shouldShow(line.controls.sku, true) && duplicates.includes(sku)
      ? 'This SKU is on another line. Combine the quantities on one line.'
      : null;
  }

  protected listError(): string | null {
    const items = this.items();
    return items.errors && this.submitAttempts() > 0 ? errorMessage(items) : null;
  }

  private shouldShow(control: AbstractControl, ignoreValidity = false): boolean {
    return (ignoreValidity || control.invalid) && (control.touched || this.submitAttempts() > 0);
  }
}
