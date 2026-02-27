import { Routes } from '@angular/router';

export const homeRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./home.component').then(m => m.HomeComponent),
    title: 'Home - StuffTracker'
  },
  {
    path: 'location/new',
    loadComponent: () => import('../location/location-form').then(m => m.LocationFormComponent),
    title: 'New Location - StuffTracker'
  },
  {
    path: 'location/:id',
    loadComponent: () => import('../location/location-detail').then(m => m.LocationDetailComponent),
    title: 'Location - StuffTracker'
  },
  {
    path: 'location/:id/edit',
    loadComponent: () => import('../location/location-form').then(m => m.LocationFormComponent),
    title: 'Edit Location - StuffTracker'
  },
  {
    path: 'location/:parentId/add-sublocation',
    loadComponent: () => import('../location/location-form').then(m => m.LocationFormComponent),
    title: 'Add Sub-location - StuffTracker'
  },
  {
    path: 'location/:locationId/add-item',
    loadComponent: () => import('../item/item-form').then(m => m.ItemFormComponent),
    title: 'Add Item - StuffTracker'
  }
];
