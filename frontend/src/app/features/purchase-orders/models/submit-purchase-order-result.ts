import { PurchaseOrder } from './purchase-order';

// created is true when the API answered 201 (new order), false for 200 (a replay of an existing one).
export interface SubmitPurchaseOrderResult {
  order: PurchaseOrder;
  created: boolean;
}
