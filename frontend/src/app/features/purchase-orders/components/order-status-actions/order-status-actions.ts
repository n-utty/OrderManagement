import { ChangeDetectionStrategy, Component, input, linkedSignal, output } from '@angular/core';
import { OrderStatus } from '../../models';

const ACTION_LABELS: Record<OrderStatus, string> = {
  Pending: 'Move back to pending',
  Confirmed: 'Confirm order',
  Fulfilled: 'Mark as fulfilled',
  Cancelled: 'Cancel order',
};

/** Offers only the moves the server says are allowed; cancelling asks for confirmation first. */
@Component({
  selector: 'app-order-status-actions',
  templateUrl: './order-status-actions.html',
  styleUrl: './order-status-actions.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderStatusActions {
  readonly allowed = input.required<readonly OrderStatus[]>();
  /** The status currently being saved, if any. */
  readonly saving = input<OrderStatus | null>(null);
  readonly statusChange = output<OrderStatus>();

  protected readonly labels = ACTION_LABELS;

  // Resets whenever the order's options change, e.g. after a successful update.
  protected readonly confirmingCancel = linkedSignal({
    source: this.allowed,
    computation: () => false,
  });

  protected choose(status: OrderStatus): void {
    if (status === 'Cancelled') {
      this.confirmingCancel.set(true);
      return;
    }
    this.statusChange.emit(status);
  }

  protected confirmCancel(): void {
    this.confirmingCancel.set(false);
    this.statusChange.emit('Cancelled');
  }
}
