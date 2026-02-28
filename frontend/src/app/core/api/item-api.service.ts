import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

/**
 * Represents an item in a list view (compact format)
 */
export interface ItemListItem {
  id: string;
  name: string;
  quantity: number;
}

/**
 * Response model for item operations (create, update, get)
 */
export interface ItemResponse {
  id: string;
  name: string;
  description?: string;
  quantity: number;
  locationId: string;
  createdAt: string;
  updatedAt: string;
}

/**
 * Detailed item response including location context
 */
export interface ItemDetail extends ItemResponse {
  locationPath: string[];
  locationName: string;
}

/**
 * Request model for creating a new item
 */
export interface CreateItemRequest {
  name: string;
  description?: string;
  quantity?: number;
  locationId: string;
}

/**
 * Request model for updating an existing item
 */
export interface UpdateItemRequest {
  name?: string;
  description?: string;
  quantity?: number;
}

/**
 * Service for item-related API operations
 */
@Injectable({
  providedIn: 'root'
})
export class ItemApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = '/items';

  /**
   * Get a single item by ID with full details
   * @param id - Item ID
   * @returns Observable with item details including location path
   */
  getItem(id: string): Observable<ItemDetail> {
    return this.api.get<ItemDetail>(`${this.basePath}/${id}`);
  }

  /**
   * Create a new item
   * @param request - Item creation request with name, description, quantity, and locationId
   * @returns Observable with the created item response
   */
  createItem(request: CreateItemRequest): Observable<ItemResponse> {
    return this.api.post<ItemResponse>(this.basePath, request);
  }

  /**
   * Update an existing item
   * @param id - Item ID to update
   * @param request - Partial update request (name, description, and/or quantity)
   * @returns Observable with the updated item response
   */
  updateItem(id: string, request: UpdateItemRequest): Observable<ItemResponse> {
    return this.api.patch<ItemResponse>(`${this.basePath}/${id}`, request);
  }

  /**
   * Delete an item
   * @param id - Item ID to delete
   * @returns Observable that completes when deletion is successful
   */
  deleteItem(id: string): Observable<void> {
    return this.api.delete<void>(`${this.basePath}/${id}`);
  }

  /**
   * Move an item to a different location
   * @param itemId - Item ID to move
   * @param locationId - Target location ID
   * @returns Observable with the updated item response
   */
  moveItem(itemId: string, locationId: string): Observable<ItemResponse> {
    return this.api.patch<ItemResponse>(`${this.basePath}/${itemId}/move`, { locationId });
  }
}
