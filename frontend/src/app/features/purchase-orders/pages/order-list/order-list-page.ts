import { DatePipe, DecimalPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  linkedSignal,
} from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Params, Router, RouterLink } from '@angular/router';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { ErrorMessage } from '../../../../shared/ui/error-message/error-message';
import { LoadingState } from '../../../../shared/ui/loading-state/loading-state';
import { StatusBadge } from '../../../../shared/ui/status-badge/status-badge';
import { PurchaseOrdersApi } from '../../data-access/purchase-orders.api';
import { ORDER_STATUSES, OrderStatus, PagedResult, PurchaseOrder } from '../../models';

@Component({
  selector: 'app-order-list-page',
  imports: [DatePipe, DecimalPipe, EmptyState, ErrorMessage, LoadingState, RouterLink, StatusBadge],
  templateUrl: './order-list-page.html',
  styleUrl: './order-list-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderListPage {
  private readonly api = inject(PurchaseOrdersApi);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  // Bound from the query string, so the filter and page survive refresh and the back button.
  readonly status = input<string>();
  readonly page = input<string>();

  protected readonly statuses = ORDER_STATUSES;
  protected readonly selectedStatus = computed(() => toStatus(this.status()));
  protected readonly currentPage = computed(() => toPage(this.page()));

  protected readonly orders = rxResource({
    params: () => ({ status: this.selectedStatus(), page: this.currentPage() }),
    stream: ({ params }) => this.api.list(params.status, params.page),
  });

  // Keeps the previous page on screen while the next one loads, so paging doesn't flash.
  protected readonly result = linkedSignal<
    PagedResult<PurchaseOrder> | undefined,
    PagedResult<PurchaseOrder> | undefined
  >({
    source: () => (this.orders.hasValue() ? this.orders.value() : undefined),
    computation: (value, previous) => value ?? previous?.value,
  });

  protected readonly range = computed(() => {
    const result = this.result();
    if (!result?.items.length) return null;
    const from = (result.page - 1) * result.pageSize + 1;
    return { from, to: from + result.items.length - 1, total: result.totalCount };
  });

  // Handed to the detail page so "Back to orders" returns to this filter and page.
  protected readonly listQuery = computed<Params>(() => ({
    status: this.selectedStatus() ?? undefined,
    page: this.currentPage() > 1 ? this.currentPage() : undefined,
  }));

  protected filterBy(status: string): void {
    this.navigate({ status: status || null, page: null });
  }

  protected goToPage(page: number): void {
    this.navigate({ page: page > 1 ? page : null });
  }

  private navigate(queryParams: Params): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge',
    });
  }
}

function toStatus(value: string | undefined): OrderStatus | null {
  return ORDER_STATUSES.find((status) => status === value) ?? null;
}

function toPage(value: string | undefined): number {
  const page = Number(value);
  return Number.isInteger(page) && page > 0 ? page : 1;
}
