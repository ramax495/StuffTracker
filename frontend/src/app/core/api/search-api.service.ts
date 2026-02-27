import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

/**
 * Parameters for search requests
 */
export interface SearchParams {
  /** Search query string */
  q?: string;
  /** Filter by location ID */
  locationId?: string;
  /** Maximum number of results to return */
  limit?: number;
  /** Offset for pagination */
  offset?: number;
}

/**
 * Individual search result item
 */
export interface SearchResultItem {
  /** Item unique identifier */
  id: string;
  /** Item name */
  name: string;
  /** Item description (optional) */
  description?: string;
  /** Item quantity */
  quantity: number;
  /** ID of the location containing this item */
  locationId: string;
  /** Full path of location names from root to item's location */
  locationPath: string[];
}

/**
 * Search results response
 */
export interface SearchResults {
  /** Array of matching items */
  items: SearchResultItem[];
  /** Total number of matching items (for pagination) */
  total: number;
  /** Whether there are more results available */
  hasMore: boolean;
}

/**
 * Service for search-related API operations
 */
@Injectable({
  providedIn: 'root'
})
export class SearchApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = '/search';

  /**
   * Search for items across all locations
   * @param params - Search parameters including query, filters, and pagination
   * @returns Observable with search results
   */
  searchItems(params: SearchParams): Observable<SearchResults> {
    const queryParams: Record<string, string | number | boolean> = {};

    if (params.q) {
      queryParams['q'] = params.q;
    }
    if (params.locationId) {
      queryParams['locationId'] = params.locationId;
    }
    if (params.limit !== undefined) {
      queryParams['limit'] = params.limit;
    }
    if (params.offset !== undefined) {
      queryParams['offset'] = params.offset;
    }

    return this.api.get<SearchResults>(`${this.basePath}/items`, queryParams);
  }
}
