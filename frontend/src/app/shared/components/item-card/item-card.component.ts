import { Component, ChangeDetectionStrategy, input, inject } from '@angular/core';
import { Router } from '@angular/router';
import { ItemListItem } from '../../../core/api/item-api.service';
import { TelegramService } from '../../../telegram/telegram.service';

/**
 * Card component for displaying an item in a list
 *
 * Shows item name with box icon and quantity badge (if quantity > 1).
 * Clicking navigates to the item detail page.
 * Styled for Telegram Mini App with touch-friendly tap targets (min 44px).
 */
@Component({
  selector: 'app-item-card',
  standalone: true,
  template: `
    <button
      type="button"
      class="item-card"
      (click)="navigateToItem()"
      [attr.aria-label]="'View item: ' + item().name"
    >
      <!-- Box/item icon -->
      <div class="item-card__icon">
        <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
          <path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V5h14v14zm-7-2h2v-4h4v-2h-4V7h-2v4H8v2h4v4z"/>
        </svg>
      </div>

      <!-- Item info -->
      <div class="item-card__content">
        <span class="item-card__name">{{ item().name }}</span>
        @if (item().quantity > 1) {
          <span class="item-card__quantity">
            <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path d="M7 18c-1.1 0-1.99.9-1.99 2S5.9 22 7 22s2-.9 2-2-.9-2-2-2zM1 2v2h2l3.6 7.59-1.35 2.45c-.16.28-.25.61-.25.96 0 1.1.9 2 2 2h12v-2H7.42c-.14 0-.25-.11-.25-.25l.03-.12.9-1.63h7.45c.75 0 1.41-.41 1.75-1.03l3.58-6.49c.08-.14.12-.31.12-.49 0-.55-.45-1-1-1H5.21l-.94-2H1zm16 16c-1.1 0-1.99.9-1.99 2s.89 2 1.99 2 2-.9 2-2-.9-2-2-2z"/>
            </svg>
            {{ item().quantity }}
          </span>
        }
      </div>

      <!-- Chevron right -->
      <div class="item-card__arrow">
        <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
          <path d="M8.59 16.59L13.17 12 8.59 7.41 10 6l6 6-6 6z"/>
        </svg>
      </div>
    </button>
  `,
  styles: [`
    .item-card {
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
      min-height: 64px;
      text-align: left;

      &:active {
        background-color: var(--tg-theme-secondary-bg-color);
        transform: scale(0.98);
      }
    }

    .item-card__icon {
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

    .item-card__content {
      flex: 1;
      min-width: 0;
      display: flex;
      flex-direction: column;
      gap: var(--spacing-xs);
    }

    .item-card__name {
      font-size: 1rem;
      font-weight: 500;
      color: var(--tg-theme-text-color);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .item-card__quantity {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      padding: 2px 8px;
      font-size: 0.75rem;
      font-weight: 500;
      border-radius: var(--radius-full);
      background-color: var(--tg-theme-secondary-bg-color);
      color: var(--tg-theme-hint-color);
      width: fit-content;

      svg {
        width: 12px;
        height: 12px;
        color: var(--tg-theme-accent-text-color);
      }
    }

    .item-card__arrow {
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
export class ItemCardComponent {
  private readonly router = inject(Router);
  private readonly telegramService = inject(TelegramService);

  /** Item data to display */
  readonly item = input.required<ItemListItem>();

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
