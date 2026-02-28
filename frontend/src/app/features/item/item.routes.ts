import { Routes } from '@angular/router';

export const itemRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./item-detail').then(m => m.ItemDetailComponent)
  },
  {
    path: 'edit',
    loadComponent: () => import('./item-form').then(m => m.ItemFormComponent),
    title: 'Edit Item - StuffTracker'
  }
];
