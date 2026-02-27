import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadChildren: () => import('./features/home/home.routes').then(m => m.homeRoutes),
    title: 'Home - StuffTracker'
  },
  {
    path: 'item/:id',
    loadChildren: () => import('./features/item/item.routes').then(m => m.itemRoutes),
    title: 'Item - StuffTracker'
  },
  {
    path: 'search',
    loadChildren: () => import('./features/search/search.routes').then(m => m.searchRoutes),
    title: 'Search - StuffTracker'
  },
  {
    path: '**',
    redirectTo: ''
  }
];
