import { Component, ChangeDetectionStrategy, input, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TelegramService } from '../../../telegram/telegram.service';

/**
 * Breadcrumbs component for navigation hierarchy display
 *
 * Displays: Home > Location1 > Location2 > Current
 * Supports clickable navigation when locationIds are provided
 */
@Component({
  selector: 'app-breadcrumbs',
  standalone: true,
  template: `
    <nav class="breadcrumbs" aria-label="Breadcrumb navigation">
      <ol class="breadcrumbs__list">
        <!-- Home link -->
        <li class="breadcrumbs__item">
          <button
            type="button"
            class="breadcrumbs__link"
            (click)="navigateToHome()"
            aria-label="Navigate to home"
          >
            <svg class="breadcrumbs__home-icon" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path d="M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z"/>
            </svg>
            <span class="breadcrumbs__home-text">Home</span>
          </button>
        </li>

        <!-- Location breadcrumbs -->
        @for (crumb of breadcrumbs(); track $index; let isLast = $last) {
          <li class="breadcrumbs__item">
            <span class="breadcrumbs__separator" aria-hidden="true">
              <svg viewBox="0 0 24 24" fill="currentColor">
                <path d="M8.59 16.59L13.17 12 8.59 7.41 10 6l6 6-6 6z"/>
              </svg>
            </span>
            @if (isLast) {
              <span class="breadcrumbs__current" aria-current="page">{{ crumb }}</span>
            } @else {
              @if (locationIds() && locationIds()![$index]) {
                <button
                  type="button"
                  class="breadcrumbs__link"
                  (click)="navigateToLocation(locationIds()![$index])"
                  [attr.aria-label]="'Navigate to ' + crumb"
                >
                  {{ crumb }}
                </button>
              } @else {
                <span class="breadcrumbs__text">{{ crumb }}</span>
              }
            }
          </li>
        }
      </ol>
    </nav>
  `,
  styles: [`
    .breadcrumbs {
      padding: var(--spacing-sm) 0;
      overflow-x: auto;
      -webkit-overflow-scrolling: touch;
      scrollbar-width: none;

      &::-webkit-scrollbar {
        display: none;
      }
    }

    .breadcrumbs__list {
      display: flex;
      align-items: center;
      flex-wrap: nowrap;
      gap: var(--spacing-xs);
      min-width: max-content;
    }

    .breadcrumbs__item {
      display: flex;
      align-items: center;
      gap: var(--spacing-xs);
    }

    .breadcrumbs__link {
      display: flex;
      align-items: center;
      gap: var(--spacing-xs);
      padding: var(--spacing-xs) var(--spacing-sm);
      font-size: 0.875rem;
      color: var(--tg-theme-link-color);
      background: transparent;
      border: none;
      border-radius: var(--radius-sm);
      cursor: pointer;
      transition: background-color var(--transition-fast);
      min-height: 44px;
      min-width: 44px;
      justify-content: center;

      &:active {
        background-color: var(--tg-theme-secondary-bg-color);
      }
    }

    .breadcrumbs__home-icon {
      width: 18px;
      height: 18px;
      flex-shrink: 0;
    }

    .breadcrumbs__home-text {
      @media (max-width: 360px) {
        display: none;
      }
    }

    .breadcrumbs__separator {
      display: flex;
      align-items: center;
      color: var(--tg-theme-hint-color);

      svg {
        width: 16px;
        height: 16px;
      }
    }

    .breadcrumbs__text {
      font-size: 0.875rem;
      color: var(--tg-theme-hint-color);
      padding: var(--spacing-xs) var(--spacing-sm);
    }

    .breadcrumbs__current {
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--tg-theme-text-color);
      padding: var(--spacing-xs) var(--spacing-sm);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      max-width: 200px;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BreadcrumbsComponent {
  private readonly router = inject(Router);
  private readonly telegramService = inject(TelegramService);

  /** Array of breadcrumb labels to display */
  readonly breadcrumbs = input.required<string[]>();

  /** Optional array of location IDs for clickable navigation */
  readonly locationIds = input<string[]>();

  /**
   * Navigate to the home page
   */
  navigateToHome(): void {
    this.triggerHapticFeedback();
    this.router.navigate(['/']);
  }

  /**
   * Navigate to a specific location
   */
  navigateToLocation(locationId: string): void {
    this.triggerHapticFeedback();
    this.router.navigate(['/location', locationId]);
  }

  private triggerHapticFeedback(): void {
    if (this.telegramService.isInTelegram()) {
      // Trigger light haptic feedback for navigation
      try {
        // @ts-expect-error - HapticFeedback may not be typed in SDK
        window.Telegram?.WebApp?.HapticFeedback?.impactOccurred('light');
      } catch {
        // Silently ignore if haptic feedback is not available
      }
    }
  }
}
