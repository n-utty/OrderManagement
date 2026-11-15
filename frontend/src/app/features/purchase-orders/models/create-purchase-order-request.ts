import { LineItemRequest } from './line-item';

export interface CreatePurchaseOrderRequest {
  externalReference: string;
  customerId: string;
  customerName: string;
  customerEmail: string;
  currency: string;
  notes?: string | null;
  lineItems: LineItemRequest[];
}
