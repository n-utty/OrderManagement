import { Routes } from '@angular/router';

export const PURCHASE_ORDER_ROUTES: Routes = [
  {
    path: '',
    title: 'Orders · Order Management',
    loadComponent: () => import('./pages/order-list/order-list-page').then((m) => m.OrderListPage),
  },
  {
    // Declared before ':id' so "new" is never read as an order id.
    path: 'new',
    title: 'New order · Order Management',
    loadComponent: () => import('./pages/new-order/new-order-page').then((m) => m.NewOrderPage),
  },
  {
    path: ':id',
    title: 'Order · Order Management',
    loadComponent: () =>
      import('./pages/order-detail/order-detail-page').then((m) => m.OrderDetailPage),
  },
];
