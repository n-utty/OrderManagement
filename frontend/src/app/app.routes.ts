import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'orders' },
  {
    path: 'orders',
    loadChildren: () =>
      import('./features/purchase-orders/purchase-orders.routes').then(
        (m) => m.PURCHASE_ORDER_ROUTES,
      ),
  },
  {
    path: '**',
    title: 'Page not found · Order Management',
    loadComponent: () => import('./layout/not-found/not-found-page').then((m) => m.NotFoundPage),
  },
];
