import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { ItemListItem } from './item-api.service';

/**
 * Re-export ItemListItem from item-api.service for backward compatibility
 */
export type { ItemListItem } from './item-api.service';

/**
 * Represents a location item in a list view
 */
export interface LocationListItem {
  id: string;
  name: string;
  childCount: number;
  itemCount: number;
}

/**
 * Response model for location operations (create, update, get)
 */
export interface LocationResponse {
  id: string;
  name: string;
  parentId?: string;
  breadcrumbs: string[];
  breadcrumbIds: string[];
  depth: number;
  createdAt: string;
  updatedAt: string;
}

/**
 * Detailed location response including children and items
 */
export interface LocationDetail extends LocationResponse {
  children: LocationListItem[];
  items: ItemListItem[];
}

/**
 * Tree node for hierarchical location display
 */
export interface LocationTreeNode {
  id: string;
  name: string;
  depth: number;
  children: LocationTreeNode[];
}

/**
 * Request model for creating a new location
 */
export interface CreateLocationRequest {
  name: string;
  parentId?: string;
}

/**
 * Request model for updating an existing location
 */
export interface UpdateLocationRequest {
  name: string;
}

/**
 * Service for location-related API operations
 */
@Injectable({
  providedIn: 'root'
})
export class LocationApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = '/locations';

  /**
   * Get all top-level locations (locations without a parent)
   */
  getTopLevelLocations(): Observable<LocationListItem[]> {
    return this.api.get<LocationListItem[]>(this.basePath);
  }

  /**
   * Get a single location by ID with full details
   */
  getLocation(id: string): Observable<LocationDetail> {
    return this.api.get<LocationDetail>(`${this.basePath}/${id}`);
  }

  /**
   * Create a new location
   */
  createLocation(request: CreateLocationRequest): Observable<LocationResponse> {
    return this.api.post<LocationResponse>(this.basePath, request);
  }

  /**
   * Update an existing location
   */
  updateLocation(id: string, request: UpdateLocationRequest): Observable<LocationResponse> {
    return this.api.put<LocationResponse>(`${this.basePath}/${id}`, request);
  }

  /**
   * Delete a location
   * @param id - Location ID to delete
   * @param force - If true, cascade delete all children and items
   */
  deleteLocation(id: string, force?: boolean): Observable<void> {
    const endpoint = force
      ? `${this.basePath}/${id}?force=true`
      : `${this.basePath}/${id}`;
    return this.api.delete<void>(endpoint);
  }

  /**
   * Get the complete location tree hierarchy
   */
  getLocationTree(): Observable<LocationTreeNode[]> {
    return this.api.get<LocationTreeNode[]>(`${this.basePath}/tree`);
  }

  /**
   * Move a location to a new parent
   * @param locationId - ID of the location to move
   * @param parentId - ID of the new parent location, or null for root level
   */
  moveLocation(locationId: string, parentId: string | null): Observable<LocationResponse> {
    return this.api.post<LocationResponse>(
      `${this.basePath}/${locationId}/move`,
      { parentId }
    );
  }
}
