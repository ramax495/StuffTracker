import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  OnInit,
  OnDestroy,
  DestroyRef
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil, tap } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SearchApiService, SearchResultItem, SearchResults } from '../../core/api/search-api.service';
import { LocationApiService, LocationTreeNode } from '../../core/api/location-api.service';
import { SearchResultItemComponent } from '../../shared/components/search-result-item';
import { LocationPickerComponent } from '../../shared/components/location-picker';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner';
import { EmptyStateComponent } from '../../shared/components/empty-state';
import { ErrorToastService } from '../../shared/components/error-toast';
import { TelegramService } from '../../telegram/telegram.service';

/**
 * Search component with debounced search input, results list,
 * pagination support, location filter, and various states (loading, empty, error).
 *
 * Features:
 * - Debounced search with 300ms delay
 * - Pagination with "Load more" button
 * - Location filter with tree picker (T068)
 * - Filter chip/badge display for active location filter (T069)
 * - Multiple states: initial, loading, results, empty, error
 */
@Component({
  selector: 'app-search',
  standalone: true,
  imports: [FormsModule, SearchResultItemComponent, LocationPickerComponent, LoadingSpinnerComponent, EmptyStateComponent],
  template: `
    <div class="search-container">
      <!-- Search input section -->
      <div class="search-input-wrapper">
        <div class="search-input-container">
          <!-- Search icon -->
          <svg class="search-input__icon" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
            <path d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/>
          </svg>

          <input
            type="text"
            class="search-input"
            placeholder="Search items..."
            [ngModel]="searchQuery()"
            (ngModelChange)="onSearchInputChange($event)"
            autocomplete="off"
            autocapitalize="off"
            autocorrect="off"
            spellcheck="false"
          />

          <!-- Clear button -->
          @if (searchQuery()) {
            <button
              type="button"
              class="search-input__clear"
              (click)="clearSearch()"
              aria-label="Clear search"
            >
              <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
              </svg>
            </button>
          }

          <!-- Filter button -->
          <button
            type="button"
            class="search-input__filter"
            [class.search-input__filter--active]="selectedLocation()"
            (click)="openLocationPicker()"
            [attr.aria-label]="selectedLocation() ? 'Filter: ' + selectedLocation()!.name : 'Filter by location'"
          >
            <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path d="M10 18h4v-2h-4v2zM3 6v2h18V6H3zm3 7h12v-2H6v2z"/>
            </svg>
            @if (selectedLocation()) {
              <span class="search-input__filter-dot"></span>
            }
          </button>
        </div>

        <!-- Filter chip (T069) -->
        @if (selectedLocation()) {
          <div class="search-filter-chip-container">
            <button
              type="button"
              class="search-filter-chip"
              (click)="openLocationPicker()"
              [attr.aria-label]="'Filtering by: ' + selectedLocation()!.name + '. Click to change or clear filter.'"
            >
              <!-- Folder icon -->
              <svg class="search-filter-chip__icon" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                <path d="M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z"/>
              </svg>
              <span class="search-filter-chip__text">{{ selectedLocation()!.name }}</span>
              <!-- Clear button -->
              <button
                type="button"
                class="search-filter-chip__clear"
                (click)="clearLocationFilter($event)"
                aria-label="Clear location filter"
              >
                <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                  <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
                </svg>
              </button>
            </button>
          </div>
        }
      </div>

      <!-- Results count -->
      @if (!isLoading() && !error() && hasSearched() && totalCount() > 0) {
        <div class="search-results-count">
          {{ totalCount() }} {{ totalCount() === 1 ? 'result' : 'results' }} found
          @if (selectedLocation()) {
            <span class="search-results-count__filter"> in {{ selectedLocation()!.name }}</span>
          }
        </div>
      }

      <!-- Loading state -->
      @if (isLoading()) {
        <app-loading-spinner size="medium" message="Searching..." />
      }

      <!-- Error state -->
      @if (error()) {
        <div class="search-state search-error">
          <svg class="search-error__icon" viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z"/>
          </svg>
          <p class="search-error__text">{{ error() }}</p>
          <button
            type="button"
            class="btn btn-secondary"
            (click)="retrySearch()"
          >
            Try Again
          </button>
        </div>
      }

      <!-- Initial state (no search yet) -->
      @if (!isLoading() && !error() && !hasSearched()) {
        <app-empty-state
          icon="empty-search"
          title="Search your items"
          [message]="selectedLocation() ? 'Type to search for items in ' + selectedLocation()!.name : 'Type to search for items across all your locations'"
        />
      }

      <!-- Empty state (no results) -->
      @if (!isLoading() && !error() && hasSearched() && results().length === 0) {
        <app-empty-state
          icon="no-results"
          title="No results found"
          [message]="selectedLocation() ? 'Try a different search term or clear the location filter' : 'Try a different search term or check your spelling'"
        />
      }

      <!-- Results list -->
      @if (!isLoading() && !error() && results().length > 0) {
        <div class="search-results">
          <div class="search-results__list">
            @for (item of results(); track item.id) {
              <app-search-result-item [item]="item" />
            }
          </div>

          <!-- Load more button -->
          @if (hasMore()) {
            <div class="search-load-more">
              <button
                type="button"
                class="btn btn-secondary search-load-more__button"
                [disabled]="isLoadingMore()"
                (click)="loadMore()"
              >
                @if (isLoadingMore()) {
                  <span class="search-load-more__spinner"></span>
                  Loading...
                } @else {
                  Load more results
                }
              </button>
            </div>
          }
        </div>
      }
    </div>

    <!-- Location picker modal -->
    @if (showLocationPicker()) {
      <app-location-picker
        [locations]="locationTree()"
        [selectedId]="selectedLocation()?.id"
        (locationSelected)="onLocationSelected($event)"
        (closed)="closeLocationPicker()"
      />
    }
  `,
  styles: [`
    .search-container {
      display: flex;
      flex-direction: column;
      min-height: 100%;
      padding: var(--spacing-md);
    }

    /* Search input styles */
    .search-input-wrapper {
      position: sticky;
      top: 0;
      z-index: var(--z-sticky);
      background-color: var(--tg-theme-bg-color);
      padding-bottom: var(--spacing-md);
    }

    .search-input-container {
      display: flex;
      align-items: center;
      gap: var(--spacing-sm);
      padding: var(--spacing-sm) var(--spacing-md);
      background-color: var(--tg-theme-secondary-bg-color);
      border-radius: var(--radius-lg);
      min-height: 48px;
    }

    .search-input__icon {
      width: 20px;
      height: 20px;
      color: var(--tg-theme-hint-color);
      flex-shrink: 0;
    }

    .search-input {
      flex: 1;
      border: none;
      background: transparent;
      font-size: 1rem;
      color: var(--tg-theme-text-color);
      outline: none;
      min-width: 0;

      &::placeholder {
        color: var(--tg-theme-hint-color);
      }
    }

    .search-input__clear {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 28px;
      height: 28px;
      padding: 0;
      border: none;
      background-color: var(--tg-theme-hint-color);
      border-radius: var(--radius-full);
      cursor: pointer;
      flex-shrink: 0;
      transition: opacity var(--transition-fast);

      &:active {
        opacity: 0.7;
      }

      svg {
        width: 16px;
        height: 16px;
        color: var(--tg-theme-secondary-bg-color);
      }
    }

    /* Filter button */
    .search-input__filter {
      position: relative;
      display: flex;
      align-items: center;
      justify-content: center;
      width: 36px;
      height: 36px;
      padding: 0;
      border: none;
      background-color: transparent;
      border-radius: var(--radius-md);
      cursor: pointer;
      flex-shrink: 0;
      transition: background-color var(--transition-fast);

      &:active {
        background-color: var(--tg-theme-bg-color);
      }

      svg {
        width: 22px;
        height: 22px;
        color: var(--tg-theme-hint-color);
      }
    }

    .search-input__filter--active {
      svg {
        color: var(--tg-theme-button-color);
      }
    }

    .search-input__filter-dot {
      position: absolute;
      top: 4px;
      right: 4px;
      width: 8px;
      height: 8px;
      background-color: var(--tg-theme-button-color);
      border-radius: var(--radius-full);
    }

    /* Filter chip styles (T069) */
    .search-filter-chip-container {
      display: flex;
      padding-top: var(--spacing-sm);
    }

    .search-filter-chip {
      display: inline-flex;
      align-items: center;
      gap: var(--spacing-xs);
      padding: var(--spacing-xs) var(--spacing-xs) var(--spacing-xs) var(--spacing-sm);
      background-color: color-mix(in srgb, var(--tg-theme-button-color) 15%, transparent);
      border: 1px solid color-mix(in srgb, var(--tg-theme-button-color) 30%, transparent);
      border-radius: var(--radius-full);
      cursor: pointer;
      transition: background-color var(--transition-fast);
      max-width: 100%;

      &:active {
        background-color: color-mix(in srgb, var(--tg-theme-button-color) 25%, transparent);
      }
    }

    .search-filter-chip__icon {
      width: 16px;
      height: 16px;
      color: var(--tg-theme-button-color);
      flex-shrink: 0;
    }

    .search-filter-chip__text {
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--tg-theme-button-color);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .search-filter-chip__clear {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 24px;
      height: 24px;
      padding: 0;
      border: none;
      background-color: color-mix(in srgb, var(--tg-theme-button-color) 20%, transparent);
      border-radius: var(--radius-full);
      cursor: pointer;
      flex-shrink: 0;
      transition: background-color var(--transition-fast);

      &:active {
        background-color: color-mix(in srgb, var(--tg-theme-button-color) 35%, transparent);
      }

      svg {
        width: 14px;
        height: 14px;
        color: var(--tg-theme-button-color);
      }
    }

    /* Results count */
    .search-results-count {
      font-size: 0.875rem;
      color: var(--tg-theme-hint-color);
      padding-bottom: var(--spacing-md);
    }

    .search-results-count__filter {
      color: var(--tg-theme-button-color);
      font-weight: 500;
    }

    /* State containers */
    .search-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: var(--spacing-2xl) var(--spacing-md);
      gap: var(--spacing-md);
      text-align: center;
      flex: 1;
    }

    /* Error state */
    .search-error__icon {
      width: 48px;
      height: 48px;
      color: var(--tg-theme-destructive-text-color);
    }

    .search-error__text {
      font-size: 0.875rem;
      color: var(--tg-theme-hint-color);
      max-width: 280px;
    }

    /* Results list */
    .search-results {
      display: flex;
      flex-direction: column;
      gap: var(--spacing-md);
    }

    .search-results__list {
      display: flex;
      flex-direction: column;
      gap: var(--spacing-sm);
    }

    /* Load more */
    .search-load-more {
      display: flex;
      justify-content: center;
      padding: var(--spacing-md) 0;
    }

    .search-load-more__button {
      display: flex;
      align-items: center;
      gap: var(--spacing-sm);
      min-width: 180px;
      justify-content: center;
    }

    .search-load-more__spinner {
      width: 16px;
      height: 16px;
      border: 2px solid currentColor;
      border-top-color: transparent;
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SearchComponent implements OnInit, OnDestroy {
  private readonly searchApiService = inject(SearchApiService);
  private readonly locationApiService = inject(LocationApiService);
  private readonly telegramService = inject(TelegramService);
  private readonly toastService = inject(ErrorToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /** Current search query */
  readonly searchQuery = signal('');

  /** Search results */
  readonly results = signal<SearchResultItem[]>([]);

  /** Total count of results */
  readonly totalCount = signal(0);

  /** Whether there are more results to load */
  readonly hasMore = signal(false);

  /** Whether a search has been performed */
  readonly hasSearched = signal(false);

  /** Loading state for initial search */
  readonly isLoading = signal(false);

  /** Loading state for load more */
  readonly isLoadingMore = signal(false);

  /** Error message */
  readonly error = signal<string | null>(null);

  /** Location filter - selected location node */
  readonly selectedLocation = signal<LocationTreeNode | null>(null);

  /** Location tree for picker */
  readonly locationTree = signal<LocationTreeNode[]>([]);

  /** Whether location picker is visible */
  readonly showLocationPicker = signal(false);

  /** Whether location tree is being loaded */
  readonly isLoadingLocations = signal(false);

  /** Search input subject for debouncing */
  private readonly searchSubject = new Subject<string>();

  /** Destroy subject */
  private readonly destroy$ = new Subject<void>();

  /** Items per page */
  private readonly pageSize = 20;

  /** Current offset for pagination */
  private currentOffset = 0;

  /** Flag to track if location tree has been loaded */
  private locationTreeLoaded = false;

  ngOnInit(): void {
    this.setupSearchDebounce();
    this.handleQueryParams();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Handle search input changes
   */
  onSearchInputChange(query: string): void {
    this.searchQuery.set(query);
    this.searchSubject.next(query);
    this.updateQueryParam(query);
  }

  /**
   * Clear search input and results
   */
  clearSearch(): void {
    this.searchQuery.set('');
    this.results.set([]);
    this.totalCount.set(0);
    this.hasMore.set(false);
    this.hasSearched.set(false);
    this.error.set(null);
    this.currentOffset = 0;
    this.updateQueryParam('');
    this.triggerHapticFeedback();
  }

  /**
   * Retry the last search
   */
  retrySearch(): void {
    const query = this.searchQuery();
    if (query) {
      this.performSearch(query);
    }
  }

  /**
   * Load more results
   */
  loadMore(): void {
    const query = this.searchQuery();
    if (!query || this.isLoadingMore() || !this.hasMore()) {
      return;
    }

    this.isLoadingMore.set(true);
    this.currentOffset += this.pageSize;

    const locationId = this.selectedLocation()?.id;

    this.searchApiService
      .searchItems({
        q: query,
        locationId,
        limit: this.pageSize,
        offset: this.currentOffset
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response: SearchResults) => {
          this.results.update(current => [...current, ...response.items]);
          this.hasMore.set(response.hasMore);
          this.isLoadingMore.set(false);
        },
        error: (err) => {
          const errorMessage = err.message || 'Failed to load more results';
          this.error.set(errorMessage);
          this.isLoadingMore.set(false);
          this.currentOffset -= this.pageSize;
          this.toastService.error(errorMessage);
        }
      });
  }

  /**
   * Open the location picker modal
   */
  openLocationPicker(): void {
    this.triggerHapticFeedback();
    this.showLocationPicker.set(true);

    // Lazy load location tree if not already loaded
    if (!this.locationTreeLoaded) {
      this.loadLocationTree();
    }
  }

  /**
   * Close the location picker modal
   */
  closeLocationPicker(): void {
    this.showLocationPicker.set(false);
  }

  /**
   * Handle location selection from picker
   */
  onLocationSelected(location: LocationTreeNode | null): void {
    this.selectedLocation.set(location);
    this.showLocationPicker.set(false);

    // Re-run search with new filter if there's a query
    const query = this.searchQuery();
    if (query.trim()) {
      this.performSearch(query);
    }
  }

  /**
   * Clear the location filter
   */
  clearLocationFilter(event: Event): void {
    event.stopPropagation();
    this.triggerHapticFeedback();
    this.selectedLocation.set(null);

    // Re-run search without filter if there's a query
    const query = this.searchQuery();
    if (query.trim()) {
      this.performSearch(query);
    }
  }

  private setupSearchDebounce(): void {
    this.searchSubject
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        tap(query => {
          if (!query.trim()) {
            this.results.set([]);
            this.totalCount.set(0);
            this.hasMore.set(false);
            this.hasSearched.set(false);
            this.error.set(null);
            this.currentOffset = 0;
          }
        }),
        takeUntil(this.destroy$)
      )
      .subscribe(query => {
        if (query.trim()) {
          this.performSearch(query);
        }
      });
  }

  private handleQueryParams(): void {
    this.route.queryParams
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(params => {
        const query = params['q'] || '';
        if (query && query !== this.searchQuery()) {
          this.searchQuery.set(query);
          this.performSearch(query);
        }
      });
  }

  private performSearch(query: string): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.currentOffset = 0;

    const locationId = this.selectedLocation()?.id;

    this.searchApiService
      .searchItems({
        q: query,
        locationId,
        limit: this.pageSize,
        offset: 0
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response: SearchResults) => {
          this.results.set(response.items);
          this.totalCount.set(response.total);
          this.hasMore.set(response.hasMore);
          this.hasSearched.set(true);
          this.isLoading.set(false);
        },
        error: (err) => {
          const errorMessage = err.message || 'Search failed. Please try again.';
          this.error.set(errorMessage);
          this.isLoading.set(false);
          this.hasSearched.set(true);
          this.toastService.error(errorMessage);
        }
      });
  }

  private loadLocationTree(): void {
    this.isLoadingLocations.set(true);

    this.locationApiService
      .getLocationTree()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (tree: LocationTreeNode[]) => {
          this.locationTree.set(tree);
          this.locationTreeLoaded = true;
          this.isLoadingLocations.set(false);
        },
        error: () => {
          // Silently fail - picker will show empty state
          this.isLoadingLocations.set(false);
        }
      });
  }

  private updateQueryParam(query: string): void {
    const queryParams = query ? { q: query } : {};
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  private triggerHapticFeedback(): void {
    if (this.telegramService.isInTelegram()) {
      try {
        // @ts-expect-error - HapticFeedback may not be typed in SDK
        window.Telegram?.WebApp?.HapticFeedback?.impactOccurred('light');
      } catch {
        // Silently ignore if haptic feedback is not available
      }
    }
  }
}
