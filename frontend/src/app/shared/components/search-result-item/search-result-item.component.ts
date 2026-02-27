import { Component, ChangeDetectionStrategy, input, inject } from '@angular/core';
import { Router } from '@angular/router';
import { SearchResultItem } from '../../../core/api/search-api.service';
import { TelegramService } from '../../../telegram/telegram.service';

/**
 * Component for displaying a search result item
 *
 * Shows item name with quantity badge (if quantity > 1),
 * and location path as breadcrumb text.
 * Clicking navigates to the item detail page.
 * Styled for Telegram Mini App with touch-friendly tap targets (min 44px).
 */
@Component({
  selector: 'app-search-result-item',
  standalone: true,
  template: `
    <button
      type="button"
      class="search-result-item"
      (click)="navigateToItem()"
      [attr.aria-label]="'View item: ' + item().name"
    >
      <!-- Item icon -->
      <div class="search-result-item__icon">
        <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
          <path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V5h14v14zm-7-2h2v-4h4v-2h-4V7h-2v4H8v2h4v4z"/>
        </svg>
      </div>

      <!-- Item info -->
      <div class="search-result-item__content">
        <div class="search-result-item__header">
          <span class="search-result-item__name">{{ item().name }}</span>
          @if (item().quantity > 1) {
            <span class="search-result-item__quantity">
              x{{ item().quantity }}
            </span>
          }
        </div>

        <!-- Location path breadcrumb -->
        <div class="search-result-item__location">
          <svg class="search-result-item__location-icon" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
            <path d="M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z"/>
          </svg>
          <span class="search-result-item__path">{{ formatLocationPath() }}</span>
        </div>

        @if (item().description) {
          <p class="search-result-item__description">{{ item().description }}</p>
        }
      </div>

      <!-- Chevron right -->
      <div class="search-result-item__arrow">
        <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
          <path d="M8.59 16.59L13.17 12 8.59 7.41 10 6l6 6-6 6z"/>
        </svg>
      </div>
    </button>
  `,
  styles: [`
    .search-result-item {
      display: flex;
      align-items: center;
      gap: var(--spacing-md);
      width: 100%;
      padding: var(--spacing-md);
      background-color: var(--tg-theme-section-bg-color);
      border: none;
      border-radius: var(--radius-lg);
      cursor: pointer;
      transition: background-color var(--transition-fast), transform var(--transition-fast);
      min-height: 72px;
      text-align: left;

      &:active {
        background-color: var(--tg-theme-secondary-bg-color);
        transform: scale(0.98);
      }
    }

    .search-result-item__icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 44px;
      height: 44px;
      background-color: var(--tg-theme-accent-text-color);
      border-radius: var(--radius-md);
      flex-shrink: 0;

      svg {
        width: 24px;
        height: 24px;
        color: var(--tg-theme-button-text-color);
      }
    }

    .search-result-item__content {
      flex: 1;
      min-width: 0;
      display: flex;
      flex-direction: column;
      gap: var(--spacing-xs);
    }

    .search-result-item__header {
      display: flex;
      align-items: center;
      gap: var(--spacing-sm);
    }

    .search-result-item__name {
      font-size: 1rem;
      font-weight: 500;
      color: var(--tg-theme-text-color);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .search-result-item__quantity {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      padding: 2px 8px;
      font-size: 0.75rem;
      font-weight: 600;
      border-radius: var(--radius-full);
      background-color: var(--tg-theme-button-color);
      color: var(--tg-theme-button-text-color);
      flex-shrink: 0;
    }

    .search-result-item__location {
      display: flex;
      align-items: center;
      gap: 4px;
      color: var(--tg-theme-hint-color);
    }

    .search-result-item__location-icon {
      width: 14px;
      height: 14px;
      flex-shrink: 0;
    }

    .search-result-item__path {
      font-size: 0.75rem;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .search-result-item__description {
      font-size: 0.8125rem;
      color: var(--tg-theme-subtitle-text-color);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .search-result-item__arrow {
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;

      svg {
        width: 20px;
        height: 20px;
        color: var(--tg-theme-hint-color);
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SearchResultItemComponent {
  private readonly router = inject(Router);
  private readonly telegramService = inject(TelegramService);

  /** Search result item data to display */
  readonly item = input.required<SearchResultItem>();

  /**
   * Format location path as breadcrumb text
   * Example: ["Home", "Garage", "Shelf"] -> "Home > Garage > Shelf"
   */
  formatLocationPath(): string {
    const path = this.item().locationPath;
    if (!path || path.length === 0) {
      return 'Unknown location';
    }
    return path.join(' > ');
  }

  /**
   * Navigate to the item detail page
   */
  navigateToItem(): void {
    this.triggerHapticFeedback();
    this.router.navigate(['/item', this.item().id]);
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
