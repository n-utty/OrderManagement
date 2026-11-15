// No line total on the request: totals are always calculated on the server.
export interface LineItemRequest {
  sku: string;
  name: string;
  quantity: number;
  unitPrice: number;
}

export interface LineItemResponse {
  sku: string;
  name: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}
