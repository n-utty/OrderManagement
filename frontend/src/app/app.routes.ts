import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'orders' },
  {
    path: 'orders',
    title: 'Orders · Order Management',
    loadComponent: () =>
      import('./features/purchase-orders/pages/orders-page').then((m) => m.OrdersPage),
  },
  {
    path: '**',
    title: 'Page not found · Order Management',
    loadComponent: () => import('./layout/not-found/not-found-page').then((m) => m.NotFoundPage),
  },
];
