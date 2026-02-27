import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  OnInit,
  OnDestroy,
  DestroyRef,
  input,
  computed
} from '@angular/core';
import { Router } from '@angular/router';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { switchMap, catchError } from 'rxjs/operators';
import { EMPTY } from 'rxjs';
import { BreadcrumbsComponent } from '../../../shared/components/breadcrumbs';
import { LocationCardComponent } from '../../../shared/components/location-card';
import { ItemListComponent } from '../../../shared/components/item-list';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner';
import { EmptyStateComponent } from '../../../shared/components/empty-state';
import { ErrorToastService } from '../../../shared/components/error-toast';
import {
  LocationApiService,
  LocationDetail
} from '../../../core/api/location-api.service';
import { TelegramService } from '../../../telegram/telegram.service';
import { MoveLocationModalComponent } from '../move-location-modal';

/**
 * Component for displaying location details
 *
 * Features:
 * - Load location by route param id
 * - Display breadcrumbs for navigation
 * - List child locations using LocationCardComponent
 * - List items (placeholder for now)
 * - "Add Sub-location" and "Add Item" actions
 * - Edit/Delete/Move actions in header
 * - Move location to different parent with modal picker
 * - Haptic feedback for Telegram Mini App
 */
@Component({
  selector: 'app-location-detail',
  standalone: true,
  imports: [BreadcrumbsComponent, LocationCardComponent, ItemListComponent, MoveLocationModalComponent, LoadingSpinnerComponent, EmptyStateComponent],
  template: `
    <div class="location-detail">
      <!-- Loading state -->
      @if (isLoading()) {
        <app-loading-spinner size="medium" message="Loading location..." />
      }

      <!-- Error state -->
      @if (error()) {
        <div class="location-detail__error">
          <svg class="location-detail__error-icon" viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z"/>
          </svg>
          <p class="location-detail__error-text">{{ error() }}</p>
          <button
            type="button"
            class="btn btn-secondary"
            (click)="loadLocation()"
          >
            Try Again
          </button>
        </div>
      }

      <!-- Location content -->
      @if (!isLoading() && !error() && location()) {
        <!-- Breadcrumbs -->
        <app-breadcrumbs
          [breadcrumbs]="location()!.breadcrumbs"
          [locationIds]="breadcrumbLocationIds()"
        />

        <!-- Header -->
        <header class="location-detail__header">
          <h1 class="location-detail__title">{{ location()!.name }}</h1>
          <div class="location-detail__actions">
            <button
              type="button"
              class="location-detail__action-btn"
              (click)="openMoveModal()"
              aria-label="Move location"
            >
              <!-- folder-move icon -->
              <svg viewBox="0 0 24 24" fill="currentColor">
                <path d="M14 8V6c0-1.1-.9-2-2-2H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V10c0-1.1-.9-2-2-2h-6zm-2 6l-3 3v-2H6v-2h3V11l3 3zm8 4H4V6h8v4h8v8z"/>
              </svg>
            </button>
            <button
              type="button"
              class="location-detail__action-btn"
              (click)="navigateToEdit()"
              aria-label="Edit location"
            >
              <svg viewBox="0 0 24 24" fill="currentColor">
                <path d="M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z"/>
              </svg>
            </button>
            <button
              type="button"
              class="location-detail__action-btn location-detail__action-btn--danger"
              (click)="confirmDelete()"
              aria-label="Delete location"
            >
              <svg viewBox="0 0 24 24" fill="currentColor">
                <path d="M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z"/>
              </svg>
            </button>
          </div>
        </header>

        <!-- Quick actions -->
        <div class="location-detail__quick-actions">
          <button
            type="button"
            class="location-detail__quick-action"
            (click)="navigateToAddSublocation()"
          >
            <svg viewBox="0 0 24 24" fill="currentColor">
              <path d="M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z"/>
            </svg>
            <span>Add Sub-location</span>
          </button>
          <button
            type="button"
            class="location-detail__quick-action"
            (click)="navigateToAddItem()"
          >
            <svg viewBox="0 0 24 24" fill="currentColor">
              <path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-2 10h-4v4h-2v-4H7v-2h4V7h2v4h4v2z"/>
            </svg>
            <span>Add Item</span>
          </button>
        </div>

        <!-- Child locations section -->
        @if (location()!.children.length > 0) {
          <section class="location-detail__section">
            <h2 class="location-detail__section-title">Sub-locations</h2>
            <div class="location-detail__list">
              @for (child of location()!.children; track child.id) {
                <app-location-card [location]="child" />
              }
            </div>
          </section>
        }

        <!-- Items section -->
        <section class="location-detail__section">
          <h2 class="location-detail__section-title">Items</h2>
          <app-item-list
            [items]="location()!.items"
            [locationId]="id()"
          />
        </section>

        <!-- Empty state when no children and no items -->
        @if (location()!.children.length === 0 && location()!.items.length === 0) {
          <app-empty-state
            icon="empty-items"
            title="This location is empty"
            message="Add sub-locations or items to organize your stuff."
          />
        }

        <!-- Move location modal -->
        @if (showMoveModal()) {
          <app-move-location-modal
            [locationId]="location()!.id"
            [currentParentId]="location()!.parentId ?? null"
            [locationName]="location()!.name"
            (moved)="onLocationMoved()"
            (closed)="closeMoveModal()"
          />
        }
      }
    </div>
  `,
  styles: [`
    .location-detail {
      display: flex;
      flex-direction: column;
      min-height: 100%;
      padding: var(--spacing-md);
      padding-bottom: calc(var(--spacing-xl) + 60px);
    }

    /* Error state */
    .location-detail__error {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: var(--spacing-2xl) var(--spacing-md);
      gap: var(--spacing-md);
      text-align: center;
      flex: 1;
    }

    .location-detail__error-icon {
      width: 48px;
      height: 48px;
      color: var(--tg-theme-destructive-text-color);
    }

    .location-detail__error-text {
      font-size: 0.875rem;
      color: var(--tg-theme-hint-color);
      max-width: 280px;
    }

    /* Header */
    .location-detail__header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--spacing-md);
      padding: var(--spacing-sm) 0;
    }

    .location-detail__title {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--tg-theme-text-color);
      flex: 1;
      min-width: 0;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .location-detail__actions {
      display: flex;
      gap: var(--spacing-xs);
      flex-shrink: 0;
    }

    .location-detail__action-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 44px;
      height: 44px;
      background-color: var(--tg-theme-secondary-bg-color);
      border: none;
      border-radius: var(--radius-md);
      cursor: pointer;
      transition: background-color var(--transition-fast), transform var(--transition-fast);

      svg {
        width: 20px;
        height: 20px;
        color: var(--tg-theme-text-color);
      }

      &:active {
        transform: scale(0.95);
      }
    }

    .location-detail__action-btn--danger {
      svg {
        color: var(--tg-theme-destructive-text-color);
      }
    }

    /* Quick actions */
    .location-detail__quick-actions {
      display: flex;
      gap: var(--spacing-sm);
      padding: var(--spacing-md) 0;
    }

    .location-detail__quick-action {
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: var(--spacing-sm);
      padding: var(--spacing-md);
      background-color: var(--tg-theme-button-color);
      color: var(--tg-theme-button-text-color);
      border: none;
      border-radius: var(--radius-md);
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      transition: opacity var(--transition-fast), transform var(--transition-fast);
      min-height: 48px;

      svg {
        width: 20px;
        height: 20px;
        flex-shrink: 0;
      }

      span {
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }

      &:active {
        opacity: 0.9;
        transform: scale(0.98);
      }
    }

    /* Sections */
    .location-detail__section {
      margin-top: var(--spacing-lg);
    }

    .location-detail__section-title {
      font-size: 0.875rem;
      font-weight: 600;
      color: var(--tg-theme-section-header-text-color);
      text-transform: uppercase;
      letter-spacing: 0.5px;
      margin-bottom: var(--spacing-sm);
    }

    .location-detail__list {
      display: flex;
      flex-direction: column;
      gap: var(--spacing-sm);
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LocationDetailComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly locationApiService = inject(LocationApiService);
  private readonly telegramService = inject(TelegramService);
  private readonly toastService = inject(ErrorToastService);
  private readonly destroyRef = inject(DestroyRef);

  /** Location ID from route param */
  readonly id = input.required<string>();

  /** Location data */
  readonly location = signal<LocationDetail | null>(null);

  /** Loading state */
  readonly isLoading = signal(true);

  /** Error message if load fails */
  readonly error = signal<string | null>(null);

  /** Signal to trigger delete confirmation */
  readonly showDeleteConfirmation = signal(false);

  /** Whether to show the move location modal */
  readonly showMoveModal = signal(false);

  /**
   * Compute location IDs for breadcrumb navigation
   * Each breadcrumb should link to its parent location
   * Uses breadcrumbIds from API response for navigation
   */
  readonly breadcrumbLocationIds = computed(() => {
    const loc = this.location();
    if (!loc || !loc.breadcrumbIds || loc.breadcrumbIds.length === 0) {
      return [];
    }
    // Return breadcrumbIds from API (matches breadcrumbs array 1:1)
    return loc.breadcrumbIds;
  });

  /** Callback for MainButton click */
  private readonly mainButtonCallback = () => this.navigateToAddItem();

  constructor() {
    // React to route param changes; switchMap cancels in-flight requests when id changes
    toObservable(this.id)
      .pipe(
        switchMap(id => {
          console.debug('[LocationDetail] Loading location id=%s', id);
          this.isLoading.set(true);
          this.error.set(null);
          return this.locationApiService.getLocation(id).pipe(
            catchError(err => {
              const errorMessage = err.message || 'Failed to load location';
              console.error('[LocationDetail] Failed to load location', err);
              this.error.set(errorMessage);
              this.isLoading.set(false);
              this.toastService.error(errorMessage);
              return EMPTY;
            })
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(location => {
        console.debug('[LocationDetail] Location loaded: %o', location);
        this.location.set(location);
        this.isLoading.set(false);
      });
  }

  ngOnInit(): void {
    this.setupMainButton();
  }

  ngOnDestroy(): void {
    this.cleanupMainButton();
  }

  /**
   * Setup MainButton for "Add Item" action
   */
  private setupMainButton(): void {
    this.telegramService.setMainButtonText('Add Item');
    this.telegramService.showMainButton();
    this.telegramService.onMainButtonClick(this.mainButtonCallback);
  }

  /**
   * Cleanup MainButton handlers
   */
  private cleanupMainButton(): void {
    this.telegramService.offMainButtonClick(this.mainButtonCallback);
    this.telegramService.hideMainButton();
  }

  /**
   * Manually reload location data (used for "Try Again" and after move).
   * For route-param-driven loading, see the toObservable pipeline in the constructor.
   */
  loadLocation(): void {
    console.debug('[LocationDetail] Manual reload for location id=%s', this.id());
    this.isLoading.set(true);
    this.error.set(null);

    this.locationApiService
      .getLocation(this.id())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (location) => {
          console.debug('[LocationDetail] Location reloaded: %o', location);
          this.location.set(location);
          this.isLoading.set(false);
        },
        error: (err) => {
          const errorMessage = err.message || 'Failed to load location';
          console.error('[LocationDetail] Failed to reload location', err);
          this.error.set(errorMessage);
          this.isLoading.set(false);
          this.toastService.error(errorMessage);
        }
      });
  }

  /**
   * Navigate to edit location form
   */
  navigateToEdit(): void {
    this.triggerHapticFeedback();
    this.router.navigate(['/location', this.id(), 'edit']);
  }

  /**
   * Navigate to add sub-location form
   */
  navigateToAddSublocation(): void {
    this.triggerHapticFeedback();
    this.router.navigate(['/location', this.id(), 'add-sublocation']);
  }

  /**
   * Navigate to add item form
   */
  navigateToAddItem(): void {
    this.triggerHapticFeedback();
    this.router.navigate(['/location', this.id(), 'add-item']);
  }

  /**
   * Navigate to item detail
   */
  navigateToItem(itemId: string): void {
    this.triggerHapticFeedback();
    this.router.navigate(['/item', itemId]);
  }

  /**
   * Open the move location modal
   */
  openMoveModal(): void {
    this.triggerHapticFeedback();
    this.showMoveModal.set(true);
  }

  /**
   * Close the move location modal
   */
  closeMoveModal(): void {
    this.showMoveModal.set(false);
  }

  /**
   * Handle successful location move
   */
  onLocationMoved(): void {
    this.showMoveModal.set(false);
    this.triggerHapticFeedback('success');
    // Refresh location data to show updated parent info
    this.loadLocation();
  }

  /**
   * Show delete confirmation
   */
  confirmDelete(): void {
    this.triggerHapticFeedback('warning');

    const loc = this.location();
    if (!loc) return;

    // Calculate total counts for confirmation
    const childCount = loc.children.length;
    const itemCount = loc.items.length;

    // Navigate to delete confirmation or show modal
    // For now, we'll use a simple confirm (will be replaced with DeleteConfirmationComponent)
    if (childCount > 0 || itemCount > 0) {
      const message = `This will delete ${loc.name} and all its contents:\n` +
        `- ${childCount} sub-location(s)\n` +
        `- ${itemCount} item(s)\n\n` +
        `Are you sure?`;

      if (confirm(message)) {
        this.deleteLocation(true);
      }
    } else {
      if (confirm(`Delete "${loc.name}"?`)) {
        this.deleteLocation(false);
      }
    }
  }

  /**
   * Delete the location
   */
  private deleteLocation(force: boolean): void {
    this.locationApiService
      .deleteLocation(this.id(), force)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.triggerHapticFeedback('success');
          this.router.navigate(['/']);
        },
        error: (err) => {
          this.triggerHapticFeedback('error');
          const errorMessage = err.message || 'Failed to delete location';
          this.error.set(errorMessage);
          this.toastService.error(errorMessage);
        }
      });
  }

  private triggerHapticFeedback(type: 'light' | 'warning' | 'success' | 'error' = 'light'): void {
    if (this.telegramService.isInTelegram()) {
      try {
        if (type === 'warning' || type === 'error') {
          // @ts-expect-error - HapticFeedback may not be typed in SDK
          window.Telegram?.WebApp?.HapticFeedback?.notificationOccurred(type);
        } else if (type === 'success') {
          // @ts-expect-error - HapticFeedback may not be typed in SDK
          window.Telegram?.WebApp?.HapticFeedback?.notificationOccurred('success');
        } else {
          // @ts-expect-error - HapticFeedback may not be typed in SDK
          window.Telegram?.WebApp?.HapticFeedback?.impactOccurred('light');
        }
      } catch {
        // Silently ignore if haptic feedback is not available
      }
    }
  }
}
