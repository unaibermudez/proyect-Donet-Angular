import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'products' },
  {
    path: 'products',
    loadComponent: () =>
      import('./features/products/product-list/product-list').then((m) => m.ProductList),
    title: 'Productos · Tienda Tech',
  },
];
