import { PagedResult, PurchaseOrder } from '../models';

export function aPurchaseOrder(overrides: Partial<PurchaseOrder> = {}): PurchaseOrder {
  return {
    id: '7b0f3c4e-2d1a-4f6b-9c8e-1a2b3c4d5e6f',
    externalReference: 'PO-1001',
    customerId: 'CUST-01',
    customerName: 'Acme Trading',
    customerEmail: 'buyer@acme.test',
    currency: 'ZAR',
    notes: null,
    status: 'Pending',
    subtotal: 1234.5,
    total: 1234.5,
    createdAt: '2026-11-15T09:30:00Z',
    updatedAt: null,
    lineItems: [{ sku: 'SKU-1', name: 'Widget', quantity: 3, unitPrice: 411.5, lineTotal: 1234.5 }],
    allowedTransitions: ['Confirmed', 'Cancelled'],
    ...overrides,
  };
}

export function aPage(
  items: PurchaseOrder[],
  overrides: Partial<PagedResult<PurchaseOrder>> = {},
): PagedResult<PurchaseOrder> {
  return { items, page: 1, pageSize: 20, totalCount: items.length, totalPages: 1, ...overrides };
}
