import { Component, ChangeDetectionStrategy, input, inject } from '@angular/core';
import { Router } from '@angular/router';
import { LocationListItem } from '../../../core/api/location-api.service';
import { TelegramService } from '../../../telegram/telegram.service';

/**
 * Card component for displaying a location in a list
 *
 * Shows location name with folder icon, child count badge, and item count badge.
 * Clicking navigates to the location detail page.
 */
@Component({
  selector: 'app-location-card',
  standalone: true,
  template: `
    <button
      type="button"
      class="location-card"
      (click)="navigateToLocation()"
      [attr.aria-label]="'View location: ' + location().name"
    >
      <!-- Folder icon -->
      <div class="location-card__icon">
        <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
          <path d="M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z"/>
        </svg>
      </div>

      <!-- Location info -->
      <div class="location-card__content">
        <span class="location-card__name">{{ location().name }}</span>
        <div class="location-card__meta">
          @if (location().childCount > 0) {
            <span class="location-card__badge location-card__badge--children">
              <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                <path d="M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z"/>
              </svg>
              {{ location().childCount }}
            </span>
          }
          @if (location().itemCount > 0) {
            <span class="location-card__badge location-card__badge--items">
              <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                <path d="M20 6h-8l-2-2H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2zm-6 10H6v-2h8v2zm4-4H6v-2h12v2z"/>
              </svg>
              {{ location().itemCount }}
            </span>
          }
        </div>
      </div>

      <!-- Chevron right -->
      <div class="location-card__arrow">
        <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
          <path d="M8.59 16.59L13.17 12 8.59 7.41 10 6l6 6-6 6z"/>
        </svg>
      </div>
    </button>
  `,
  styles: [`
    .location-card {
      display: flex;
      align-items: center;
      gap: var(--spacing-md);
      width: 100%;
      padding: var(--spacing-md);
      background-color: var(--tg-theme-section-bg-color);
      border: none;
      border-radius: var(--radius-lg);
      cursor: pointer;
      transition: background-color var(--transition-fast), transform var(--transition-fast), box-shadow var(--transition-fast);
      min-height: 64px;
      text-align: left;

      &:hover {
        background-color: var(--tg-theme-secondary-bg-color);
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
      }

      &:active {
        background-color: var(--tg-theme-secondary-bg-color);
        transform: scale(0.98);
        box-shadow: none;
      }

      &:focus {
        outline: 2px solid var(--tg-theme-link-color);
        outline-offset: 2px;
      }

      &:focus:not(:focus-visible) {
        outline: none;
      }
    }

    .location-card__icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 44px;
      height: 44px;
      background-color: var(--tg-theme-button-color);
      border-radius: var(--radius-md);
      flex-shrink: 0;

      svg {
        width: 24px;
        height: 24px;
        color: var(--tg-theme-button-text-color);
      }
    }

    .location-card__content {
      flex: 1;
      min-width: 0;
      display: flex;
      flex-direction: column;
      gap: var(--spacing-xs);
    }

    .location-card__name {
      font-size: 1rem;
      font-weight: 500;
      color: var(--tg-theme-text-color);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .location-card__meta {
      display: flex;
      gap: var(--spacing-sm);
      flex-wrap: wrap;
    }

    .location-card__badge {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      padding: 2px 8px;
      font-size: 0.75rem;
      font-weight: 500;
      border-radius: var(--radius-full);
      background-color: var(--tg-theme-secondary-bg-color);
      color: var(--tg-theme-hint-color);

      svg {
        width: 12px;
        height: 12px;
      }
    }

    .location-card__badge--children {
      svg {
        color: var(--tg-theme-button-color);
      }
    }

    .location-card__badge--items {
      svg {
        color: var(--tg-theme-accent-text-color);
      }
    }

    .location-card__arrow {
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
export class LocationCardComponent {
  private readonly router = inject(Router);
  private readonly telegramService = inject(TelegramService);

  /** Location data to display */
  readonly location = input.required<LocationListItem>();

  /**
   * Navigate to the location detail page
   */
  navigateToLocation(): void {
    this.triggerHapticFeedback();
    this.router.navigate(['/location', this.location().id]);
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
