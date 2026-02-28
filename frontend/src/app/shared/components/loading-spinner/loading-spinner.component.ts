import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { NgClass } from '@angular/common';

/**
 * Loading spinner component with configurable sizes
 *
 * Features:
 * - Three size options: small, medium, large
 * - Optional loading message text
 * - Overlay mode for full-screen loading
 * - Uses Telegram theme colors via CSS variables
 *
 * @example
 * ```html
 * <app-loading-spinner size="medium" message="Loading..." />
 * <app-loading-spinner size="large" [overlay]="true" message="Processing..." />
 * ```
 */
@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  imports: [NgClass],
  template: `
    @if (overlay()) {
      <div class="spinner-overlay">
        <div class="spinner-container">
          <div
            class="spinner"
            [ngClass]="'spinner--' + size()"
            role="status"
            aria-live="polite"
          >
            <span class="spinner__circle"></span>
            <span class="visually-hidden">Loading</span>
          </div>
          @if (message()) {
            <p class="spinner-message">{{ message() }}</p>
          }
        </div>
      </div>
    } @else {
      <div class="spinner-inline">
        <div
          class="spinner"
          [ngClass]="'spinner--' + size()"
          role="status"
          aria-live="polite"
        >
          <span class="spinner__circle"></span>
          <span class="visually-hidden">Loading</span>
        </div>
        @if (message()) {
          <p class="spinner-message">{{ message() }}</p>
        }
      </div>
    }
  `,
  styles: [`
    /* Visually hidden but accessible to screen readers */
    .visually-hidden {
      position: absolute;
      width: 1px;
      height: 1px;
      padding: 0;
      margin: -1px;
      overflow: hidden;
      clip: rect(0, 0, 0, 0);
      white-space: nowrap;
      border: 0;
    }

    /* Inline spinner container */
    .spinner-inline {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: var(--spacing-md);
      padding: var(--spacing-lg);
    }

    /* Overlay mode - full screen with backdrop */
    .spinner-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      display: flex;
      align-items: center;
      justify-content: center;
      background-color: rgba(0, 0, 0, 0.4);
      z-index: var(--z-modal);
      backdrop-filter: blur(2px);
    }

    .spinner-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--spacing-md);
      padding: var(--spacing-xl);
      background-color: var(--tg-theme-section-bg-color);
      border-radius: var(--radius-xl);
      box-shadow: var(--shadow-lg);
    }

    /* Spinner element */
    .spinner {
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .spinner__circle {
      display: block;
      border-radius: 50%;
      border-style: solid;
      border-color: var(--tg-theme-secondary-bg-color);
      border-top-color: var(--tg-theme-button-color);
      animation: spin 0.8s linear infinite;
    }

    /* Size variants */
    .spinner--small .spinner__circle {
      width: 20px;
      height: 20px;
      border-width: 2px;
    }

    .spinner--medium .spinner__circle {
      width: 36px;
      height: 36px;
      border-width: 3px;
    }

    .spinner--large .spinner__circle {
      width: 48px;
      height: 48px;
      border-width: 4px;
    }

    /* Message text */
    .spinner-message {
      font-size: 0.875rem;
      color: var(--tg-theme-hint-color);
      text-align: center;
      margin: 0;
    }

    .spinner-overlay .spinner-message {
      color: var(--tg-theme-text-color);
    }

    /* Spin animation */
    @keyframes spin {
      0% {
        transform: rotate(0deg);
      }
      100% {
        transform: rotate(360deg);
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoadingSpinnerComponent {
  /** Size of the spinner: small (20px), medium (36px), or large (48px) */
  readonly size = input<'small' | 'medium' | 'large'>('medium');

  /** Optional message to display below the spinner */
  readonly message = input<string>('');

  /** Enable overlay mode for full-screen loading */
  readonly overlay = input<boolean>(false);
}
