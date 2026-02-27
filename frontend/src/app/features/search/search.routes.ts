import { Routes } from '@angular/router';

/**
 * Search feature routes
 *
 * Supports direct search links via query parameter:
 * - /search - Opens search page
 * - /search?q=query - Opens search page and performs search
 */
export const searchRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./search.component').then(m => m.SearchComponent),
    title: 'Search - StuffTracker'
  }
];
