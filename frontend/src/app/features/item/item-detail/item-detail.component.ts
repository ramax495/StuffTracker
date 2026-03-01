import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  OnInit,
  OnDestroy,
  DestroyRef,
  input
} from '@angular/core';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BreadcrumbsComponent } from '../../../shared/components/breadcrumbs';
import { ItemApiService, ItemDetail } from '../../../core/api/item-api.service';
import { LocationApiService } from '../../../core/api/location-api.service';
import { TelegramService } from '../../../telegram/telegram.service';
import { MoveItemModalComponent } from '../move-item-modal';

/**
 * Component for displaying item details
 *
 * Features:
 * - Load item by route param id
 * - Display breadcrumbs (location path)
 * - Display item properties (name, description, quantity)
 * - Edit/Delete actions in header
 * - Move item to different location with modal picker
 * - Loading/error states
 * - Haptic feedback for Telegram Mini App
 */
@Component({
  selector: 'app-item-detail',
  standalone: true,
  imports: [BreadcrumbsComponent, MoveItemModalComponent],
  template: `
    <div class="item-detail">
      <!-- Loading state -->
      @if (isLoading()) {
        <div class="item-detail__loading">
          <div class="item-detail__spinner"></div>
          <p class="item-detail__loading-text">Loading item...</p>
        </div>
      }

      <!-- Error state -->
      @if (error()) {
        <div class="item-detail__error">
          <svg class="item-detail__error-icon" viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z"/>
          </svg>
          <p class="item-detail__error-text">{{ error() }}</p>
          <button
            type="button"
            class="btn btn-secondary"
            (click)="loadItem()"
          >
            Try Again
          </button>
        </div>
      }

      <!-- Item content -->
      @if (!isLoading() && !error() && item()) {
        <!-- Breadcrumbs -->
        <app-breadcrumbs
          [breadcrumbs]="getBreadcrumbs()"
          [locationIds]="breadcrumbIds()"
        />

        <!-- Header -->
        <header class="item-detail__header">
          <div class="item-detail__header-content">
            <div class="item-detail__icon">
              <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                <path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V5h14v14zm-7-2h2v-4h4v-2h-4V7h-2v4H8v2h4v4z"/>
              </svg>
            </div>
            <h1 class="item-detail__title">{{ item()!.name }}</h1>
          </div>
          <div class="item-detail__actions">
            <button
              type="button"
              class="item-detail__action-btn"
              (click)="navigateToEdit()"
              aria-label="Edit item"
            >
              <svg viewBox="0 0 24 24" fill="currentColor">
                <path d="M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z"/>
              </svg>
            </button>
            <button
              type="button"
              class="item-detail__action-btn item-detail__action-btn--danger"
              (click)="confirmDelete()"
              aria-label="Delete item"
            >
              <svg viewBox="0 0 24 24" fill="currentColor">
                <path d="M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z"/>
              </svg>
            </button>
          </div>
        </header>

        <!-- Item properties -->
        <div class="item-detail__properties">
          <!-- Quantity -->
          <div class="item-detail__property">
            <div class="item-detail__property-icon">
              <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                <path d="M7 18c-1.1 0-1.99.9-1.99 2S5.9 22 7 22s2-.9 2-2-.9-2-2-2zM1 2v2h2l3.6 7.59-1.35 2.45c-.16.28-.25.61-.25.96 0 1.1.9 2 2 2h12v-2H7.42c-.14 0-.25-.11-.25-.25l.03-.12.9-1.63h7.45c.75 0 1.41-.41 1.75-1.03l3.58-6.49c.08-.14.12-.31.12-.49 0-.55-.45-1-1-1H5.21l-.94-2H1zm16 16c-1.1 0-1.99.9-1.99 2s.89 2 1.99 2 2-.9 2-2-.9-2-2-2z"/>
              </svg>
            </div>
            <div class="item-detail__property-content">
              <span class="item-detail__property-label">Quantity</span>
              <span class="item-detail__property-value">{{ item()!.quantity }}</span>
            </div>
          </div>

          <!-- Location -->
          <div class="item-detail__property">
            <div class="item-detail__property-icon">
              <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                <path d="M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z"/>
              </svg>
            </div>
            <div class="item-detail__property-content">
              <span class="item-detail__property-label">Location</span>
              <button
                type="button"
                class="item-detail__property-link"
                (click)="navigateToLocation()"
              >
                {{ item()!.locationName }}
              </button>
            </div>
          </div>

          <!-- Description (if present) -->
          @if (item()!.description) {
            <div class="item-detail__property item-detail__property--description">
              <div class="item-detail__property-icon">
                <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                  <path d="M14 2H6c-1.1 0-1.99.9-1.99 2L4 20c0 1.1.89 2 1.99 2H18c1.1 0 2-.9 2-2V8l-6-6zm2 16H8v-2h8v2zm0-4H8v-2h8v2zm-3-5V3.5L18.5 9H13z"/>
                </svg>
              </div>
              <div class="item-detail__property-content">
                <span class="item-detail__property-label">Description</span>
                <p class="item-detail__description">{{ item()!.description }}</p>
              </div>
            </div>
          }
        </div>

        <!-- Quick actions -->
        <div class="item-detail__quick-actions">
          <button
            type="button"
            class="item-detail__quick-action"
            (click)="openMoveModal()"
          >
            <!-- folder-move icon -->
            <svg viewBox="0 0 24 24" fill="currentColor">
              <path d="M14 8V6c0-1.1-.9-2-2-2H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V10c0-1.1-.9-2-2-2h-6zm-2 6l-3 3v-2H6v-2h3V11l3 3zm8 4H4V6h8v4h8v8z"/>
            </svg>
            <span>Move Item</span>
          </button>
        </div>

        <!-- Move item modal -->
        @if (showMoveModal()) {
          <app-move-item-modal
            [itemId]="item()!.id"
            [currentLocationId]="item()!.locationId"
            [itemName]="item()!.name"
            (moved)="onItemMoved()"
            (closed)="closeMoveModal()"
          />
        }

        <!-- Metadata footer -->
        <footer class="item-detail__footer">
          <p class="item-detail__meta">
            Created: {{ formatDate(item()!.createdAt) }}
          </p>
          @if (item()!.updatedAt !== item()!.createdAt) {
            <p class="item-detail__meta">
              Updated: {{ formatDate(item()!.updatedAt) }}
            </p>
          }
        </footer>
      }
    </div>
  `,
  styles: [`
    .item-detail {
      display: flex;
      flex-direction: column;
      min-height: 100%;
      padding: var(--spacing-md);
      padding-bottom: calc(var(--spacing-xl) + 60px);
    }

    /* Loading state */
    .item-detail__loading {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: var(--spacing-2xl) var(--spacing-md);
      gap: var(--spacing-md);
      flex: 1;
    }

    .item-detail__spinner {
      width: 40px;
      height: 40px;
      border: 3px solid var(--tg-theme-secondary-bg-color);
      border-top-color: var(--tg-theme-button-color);
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .item-detail__loading-text {
      font-size: 0.875rem;
      color: var(--tg-theme-hint-color);
    }

    /* Error state */
    .item-detail__error {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: var(--spacing-2xl) var(--spacing-md);
      gap: var(--spacing-md);
      text-align: center;
      flex: 1;
    }

    .item-detail__error-icon {
      width: 48px;
      height: 48px;
      color: var(--tg-theme-destructive-text-color);
    }

    .item-detail__error-text {
      font-size: 0.875rem;
      color: var(--tg-theme-hint-color);
      max-width: 280px;
    }

    /* Header */
    .item-detail__header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: var(--spacing-md);
      padding: var(--spacing-sm) 0;
    }

    .item-detail__header-content {
      display: flex;
      align-items: center;
      gap: var(--spacing-md);
      flex: 1;
      min-width: 0;
    }

    .item-detail__icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 48px;
      height: 48px;
      background-color: var(--tg-theme-accent-text-color);
      border-radius: var(--radius-md);
      flex-shrink: 0;

      svg {
        width: 28px;
        height: 28px;
        color: var(--tg-theme-button-text-color);
      }
    }

    .item-detail__title {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--tg-theme-text-color);
      flex: 1;
      min-width: 0;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .item-detail__actions {
      display: flex;
      gap: var(--spacing-xs);
      flex-shrink: 0;
    }

    .item-detail__action-btn {
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

    .item-detail__action-btn--danger {
      svg {
        color: var(--tg-theme-destructive-text-color);
      }
    }

    /* Properties */
    .item-detail__properties {
      display: flex;
      flex-direction: column;
      gap: var(--spacing-md);
      padding: var(--spacing-lg) 0;
    }

    .item-detail__property {
      display: flex;
      align-items: flex-start;
      gap: var(--spacing-md);
      padding: var(--spacing-md);
      background-color: var(--tg-theme-section-bg-color);
      border-radius: var(--radius-lg);
    }

    .item-detail__property--description {
      align-items: flex-start;
    }

    .item-detail__property-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 40px;
      height: 40px;
      background-color: var(--tg-theme-secondary-bg-color);
      border-radius: var(--radius-md);
      flex-shrink: 0;

      svg {
        width: 20px;
        height: 20px;
        color: var(--tg-theme-hint-color);
      }
    }

    .item-detail__property-content {
      flex: 1;
      min-width: 0;
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .item-detail__property-label {
      font-size: 0.75rem;
      font-weight: 500;
      color: var(--tg-theme-hint-color);
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .item-detail__property-value {
      font-size: 1rem;
      font-weight: 500;
      color: var(--tg-theme-text-color);
    }

    .item-detail__property-link {
      font-size: 1rem;
      font-weight: 500;
      color: var(--tg-theme-link-color);
      background: none;
      border: none;
      padding: 0;
      cursor: pointer;
      text-align: left;
      min-height: 44px;
      display: flex;
      align-items: center;
      margin: -8px 0;

      &:active {
        opacity: 0.7;
      }
    }

    .item-detail__description {
      font-size: 0.9375rem;
      color: var(--tg-theme-text-color);
      line-height: 1.5;
      margin: 0;
      white-space: pre-wrap;
      word-break: break-word;
    }

    /* Quick actions */
    .item-detail__quick-actions {
      display: flex;
      gap: var(--spacing-sm);
      padding: var(--spacing-md) 0;
    }

    .item-detail__quick-action {
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

    /* Footer */
    .item-detail__footer {
      margin-top: auto;
      padding-top: var(--spacing-lg);
      display: flex;
      flex-direction: column;
      gap: var(--spacing-xs);
    }

    .item-detail__meta {
      font-size: 0.75rem;
      color: var(--tg-theme-hint-color);
      margin: 0;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ItemDetailComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly itemApiService = inject(ItemApiService);
  private readonly locationApiService = inject(LocationApiService);
  private readonly telegramService = inject(TelegramService);
  private readonly destroyRef = inject(DestroyRef);

  /** Item ID from route param */
  readonly id = input.required<string>();

  /** Item data */
  readonly item = signal<ItemDetail | null>(null);

  /** Loading state */
  readonly isLoading = signal(true);

  /** Error message if load fails */
  readonly error = signal<string | null>(null);

  /** Whether to show the move item modal */
  readonly showMoveModal = signal(false);

  /** Location breadcrumb IDs for clickable navigation — loaded after item */
  readonly breadcrumbIds = signal<string[]>([]);

  ngOnInit(): void {
    this.loadItem();
  }

  ngOnDestroy(): void {
    this.telegramService.hideMainButton();
  }

  /**
   * Load item details from API
   */
  loadItem(): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.breadcrumbIds.set([]);

    this.itemApiService
      .getItem(this.id())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (item) => {
          this.item.set(item);
          this.isLoading.set(false);

          // Non-blocking: fetch location to get breadcrumb IDs for clickable navigation
          this.locationApiService
            .getLocation(item.locationId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
              next: (loc) => {
                console.debug('[ItemDetail] Breadcrumb IDs loaded for location', item.locationId, loc.breadcrumbIds);
                this.breadcrumbIds.set(loc.breadcrumbIds);
              },
              error: (err) => {
                console.warn('[ItemDetail] Failed to load breadcrumb IDs, breadcrumbs will be non-clickable', err);
                // Graceful degradation: breadcrumbs still display as text
              }
            });
        },
        error: (err) => {
          this.error.set(err.message || 'Failed to load item');
          this.isLoading.set(false);
        }
      });
  }

  /**
   * Get breadcrumbs array for display
   */
  getBreadcrumbs(): string[] {
    const currentItem = this.item();
    if (!currentItem) return [];

    // Include location path and current item name
    return [...currentItem.locationPath, currentItem.name];
  }

  /**
   * Navigate to edit item form
   */
  navigateToEdit(): void {
    this.triggerHapticFeedback();
    this.router.navigate(['/item', this.id(), 'edit']);
  }

  /**
   * Navigate to the item's location
   */
  navigateToLocation(): void {
    this.triggerHapticFeedback();
    const currentItem = this.item();
    if (currentItem) {
      this.router.navigate(['/location', currentItem.locationId]);
    }
  }

  /**
   * Open the move item modal
   */
  openMoveModal(): void {
    this.triggerHapticFeedback();
    this.showMoveModal.set(true);
  }

  /**
   * Close the move item modal
   */
  closeMoveModal(): void {
    this.showMoveModal.set(false);
  }

  /**
   * Handle successful item move
   */
  onItemMoved(): void {
    this.showMoveModal.set(false);
    this.triggerHapticFeedback('success');
    // Refresh item data to show new location
    this.loadItem();
  }

  /**
   * Show delete confirmation
   */
  confirmDelete(): void {
    this.triggerHapticFeedback('warning');

    const currentItem = this.item();
    if (!currentItem) return;

    if (confirm(`Delete "${currentItem.name}"?`)) {
      this.deleteItem();
    }
  }

  /**
   * Delete the item
   */
  private deleteItem(): void {
    this.itemApiService
      .deleteItem(this.id())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.triggerHapticFeedback('success');
          const currentItem = this.item();
          if (currentItem) {
            this.router.navigate(['/location', currentItem.locationId]);
          } else {
            this.router.navigate(['/']);
          }
        },
        error: (err) => {
          this.triggerHapticFeedback('error');
          this.error.set(err.message || 'Failed to delete item');
        }
      });
  }

  /**
   * Format date for display
   */
  formatDate(dateString: string): string {
    try {
      const date = new Date(dateString);
      return date.toLocaleDateString(undefined, {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
      });
    } catch {
      return dateString;
    }
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
