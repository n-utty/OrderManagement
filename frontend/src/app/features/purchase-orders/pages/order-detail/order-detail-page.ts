import { DatePipe, DecimalPipe, Location } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  afterNextRender,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiError } from '../../../../core/http/api-error';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { ErrorMessage } from '../../../../shared/ui/error-message/error-message';
import { LoadingState } from '../../../../shared/ui/loading-state/loading-state';
import { StatusBadge } from '../../../../shared/ui/status-badge/status-badge';
import { OrderStatusActions } from '../../components/order-status-actions/order-status-actions';
import { PurchaseOrdersApi } from '../../data-access/purchase-orders.api';
import { OrderStatus } from '../../models';
import { OrderBanner, readOrderNavigationState } from '../../order-navigation-state';

@Component({
  selector: 'app-order-detail-page',
  imports: [
    DatePipe,
    DecimalPipe,
    EmptyState,
    ErrorMessage,
    LoadingState,
    OrderStatusActions,
    RouterLink,
    StatusBadge,
  ],
  templateUrl: './order-detail-page.html',
  styleUrl: './order-detail-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderDetailPage {
  private readonly api = inject(PurchaseOrdersApi);
  private readonly destroyRef = inject(DestroyRef);

  private readonly location = inject(Location);
  private readonly navigationState = readOrderNavigationState(inject(Router), this.location);

  readonly id = input.required<string>();

  protected readonly backQuery = this.navigationState.listQuery ?? {};
  protected readonly banner = signal<OrderBanner | null>(this.navigationState.banner ?? null);

  constructor() {
    afterNextRender(() => this.forgetBanner());
  }

  protected readonly order = rxResource({
    params: () => this.id(),
    stream: ({ params: id }) => this.api.get(id),
  });

  protected readonly notFound = computed(() => {
    const error = this.order.error();
    return error instanceof ApiError && error.status === 404;
  });

  protected readonly savingStatus = signal<OrderStatus | null>(null);
  protected readonly statusError = signal<unknown>(null);

  protected changeStatus(status: OrderStatus): void {
    this.savingStatus.set(status);
    this.statusError.set(null);

    this.api
      .changeStatus(this.id(), status)
      .pipe(
        finalize(() => this.savingStatus.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (order) => this.order.set(order),
        error: (error: unknown) => {
          this.statusError.set(error);
          // A 422 means the order moved on elsewhere; refresh so the buttons match the server.
          if (error instanceof ApiError && error.status === 422) {
            this.order.reload();
          }
        },
      });
  }

  // The banner is shown once: drop it from history so a refresh or back/forward doesn't repeat it.
  // The rest of the state (the router's own bookkeeping, the list query) is kept.
  private forgetBanner(): void {
    const state = this.location.getState();
    if (state && typeof state === 'object' && 'banner' in state) {
      const { banner: _, ...rest } = state as Record<string, unknown>;
      this.location.replaceState(this.location.path(), '', rest);
    }
  }
}
