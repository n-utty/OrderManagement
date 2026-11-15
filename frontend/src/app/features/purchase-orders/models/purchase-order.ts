import { LineItemResponse } from './line-item';
import { OrderStatus } from './order-status';

// Mirrors PurchaseOrderResponse. Ids are GUIDs and dates are ISO 8601 strings (UTC).
export interface PurchaseOrder {
  id: string;
  externalReference: string;
  customerId: string;
  customerName: string;
  customerEmail: string;
  currency: string;
  notes: string | null;
  status: OrderStatus;
  subtotal: number;
  total: number;
  createdAt: string;
  updatedAt: string | null;
  lineItems: LineItemResponse[];
  allowedTransitions: OrderStatus[];
}
