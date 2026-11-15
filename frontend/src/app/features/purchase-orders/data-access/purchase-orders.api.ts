import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from '../../../core/http/api-base-url';
import {
  ChangeOrderStatusRequest,
  CreatePurchaseOrderRequest,
  OrderStatus,
  PagedResult,
  PurchaseOrder,
  SubmitPurchaseOrderResult,
} from '../models';

export const DEFAULT_PAGE_SIZE = 20;

@Injectable({ providedIn: 'root' })
export class PurchaseOrdersApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/purchase-orders`;

  list(
    status?: OrderStatus | null,
    page = 1,
    pageSize = DEFAULT_PAGE_SIZE,
  ): Observable<PagedResult<PurchaseOrder>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) {
      params = params.set('status', status);
    }
    return this.http.get<PagedResult<PurchaseOrder>>(this.baseUrl, { params });
  }

  get(id: string): Observable<PurchaseOrder> {
    return this.http.get<PurchaseOrder>(this.orderUrl(id));
  }

  submit(request: CreatePurchaseOrderRequest): Observable<SubmitPurchaseOrderResult> {
    return this.http
      .post<PurchaseOrder>(this.baseUrl, request, { observe: 'response' })
      .pipe(
        map((response) => ({
          order: response.body as PurchaseOrder,
          created: response.status === 201,
        })),
      );
  }

  changeStatus(id: string, status: OrderStatus): Observable<PurchaseOrder> {
    const body: ChangeOrderStatusRequest = { status };
    return this.http.put<PurchaseOrder>(`${this.orderUrl(id)}/status`, body);
  }

  private orderUrl(id: string): string {
    return `${this.baseUrl}/${encodeURIComponent(id)}`;
  }
}
