import { Component, ChangeDetectionStrategy, input, inject } from '@angular/core';
import { Router } from '@angular/router';
import { ItemListItem } from '../../../core/api/item-api.service';
import { ItemCardComponent } from '../item-card';
import { TelegramService } from '../../../telegram/telegram.service';

/**
 * List component for displaying items
 *
 * Shows a list of ItemCardComponent instances with an empty state
 * when no items exist. Includes an "Add Item" button for creating
 * new items in the context of a specific location.
 */
@Component({
  selector: 'app-item-list',
  standalone: true,
  imports: [ItemCardComponent],
  template: `
    @if (items().length > 0) {
      <!-- Items list -->
      <div class="item-list">
        @for (item of items(); track item.id) {
          <app-item-card [item]="item" />
        }
      </div>
    } @else {
      <!-- Empty state -->
      <div class="item-list__empty">
        <div class="item-list__empty-icon">
          <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
            <path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V5h14v14zm-7-2h2v-4h4v-2h-4V7h-2v4H8v2h4v4z"/>
          </svg>
        </div>
        <p class="item-list__empty-title">No items yet</p>
        <p class="item-list__empty-text">Add items to keep track of your stuff in this location.</p>
        <button
          type="button"
          class="item-list__add-btn"
          (click)="navigateToAddItem()"
        >
          <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
            <path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/>
          </svg>
          <span>Add Item</span>
        </button>
      </div>
    }
  `,
  styles: [`
    .item-list {
      display: flex;
      flex-direction: column;
      gap: var(--spacing-sm);
    }

    /* Empty state */
    .item-list__empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: var(--spacing-xl) var(--spacing-md);
      gap: var(--spacing-sm);
      text-align: center;
    }

    .item-list__empty-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 64px;
      height: 64px;
      background-color: var(--tg-theme-secondary-bg-color);
      border-radius: var(--radius-lg);
      margin-bottom: var(--spacing-sm);

      svg {
        width: 32px;
        height: 32px;
        color: var(--tg-theme-hint-color);
        opacity: 0.6;
      }
    }

    .item-list__empty-title {
      font-size: 1rem;
      font-weight: 600;
      color: var(--tg-theme-text-color);
      margin: 0;
    }

    .item-list__empty-text {
      font-size: 0.875rem;
      color: var(--tg-theme-hint-color);
      margin: 0;
      max-width: 260px;
    }

    .item-list__add-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: var(--spacing-sm);
      padding: var(--spacing-md) var(--spacing-lg);
      margin-top: var(--spacing-md);
      background-color: var(--tg-theme-button-color);
      color: var(--tg-theme-button-text-color);
      border: none;
      border-radius: var(--radius-md);
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      transition: opacity var(--transition-fast), transform var(--transition-fast);
      min-height: 44px;

      svg {
        width: 20px;
        height: 20px;
        flex-shrink: 0;
      }

      &:active {
        opacity: 0.9;
        transform: scale(0.98);
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ItemListComponent {
  private readonly router = inject(Router);
  private readonly telegramService = inject(TelegramService);

  /** List of items to display */
  readonly items = input.required<ItemListItem[]>();

  /** Location ID for "Add Item" context */
  readonly locationId = input.required<string>();

  /**
   * Navigate to add item form with location context
   */
  navigateToAddItem(): void {
    this.triggerHapticFeedback();
    this.router.navigate(['/location', this.locationId(), 'add-item']);
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
