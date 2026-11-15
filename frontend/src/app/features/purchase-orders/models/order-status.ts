// Mirrors OrderManagement.Domain.PurchaseOrders.OrderStatus (serialised as strings).
export const ORDER_STATUSES = ['Pending', 'Confirmed', 'Fulfilled', 'Cancelled'] as const;

export type OrderStatus = (typeof ORDER_STATUSES)[number];
