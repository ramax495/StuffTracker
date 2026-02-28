import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { NgClass } from '@angular/common';
import { ErrorToastService, Toast } from './error-toast.service';

/**
 * Toast notification display component
 *
 * Renders toast notifications from ErrorToastService.
 * Should be placed once in the root app component.
 *
 * Features:
 * - Stacked vertical layout for multiple toasts
 * - Color-coded by type (error, success, warning, info)
 * - Manual dismiss button
 * - Slide-in animation
 *
 * @example
 * ```html
 * <!-- In app.html -->
 * <router-outlet />
 * <app-error-toast />
 * ```
 */
@Component({
  selector: 'app-error-toast',
  standalone: true,
  imports: [NgClass],
  template: `
    <div class="toast-container" aria-live="polite" aria-atomic="true">
      @for (toast of toastService.toasts(); track toast.id) {
        <div
          class="toast"
          [ngClass]="'toast--' + toast.type"
          role="alert"
        >
          <!-- Icon based on type -->
          <div class="toast__icon">
            @switch (toast.type) {
              @case ('error') {
                <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                  <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z"/>
                </svg>
              }
              @case ('success') {
                <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                  <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"/>
                </svg>
              }
              @case ('warning') {
                <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                  <path d="M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z"/>
                </svg>
              }
              @case ('info') {
                <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                  <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z"/>
                </svg>
              }
            }
          </div>

          <!-- Message -->
          <span class="toast__message">{{ toast.message }}</span>

          <!-- Dismiss button -->
          <button
            type="button"
            class="toast__dismiss"
            (click)="dismiss(toast)"
            aria-label="Dismiss notification"
          >
            <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
            </svg>
          </button>
        </div>
      }
    </div>
  `,
  styles: [`
    .toast-container {
      position: fixed;
      bottom: calc(var(--spacing-md) + env(safe-area-inset-bottom, 0px));
      left: var(--spacing-md);
      right: var(--spacing-md);
      display: flex;
      flex-direction: column;
      gap: var(--spacing-sm);
      z-index: var(--z-tooltip);
      pointer-events: none;
    }

    .toast {
      display: flex;
      align-items: center;
      gap: var(--spacing-sm);
      padding: var(--spacing-sm) var(--spacing-md);
      border-radius: var(--radius-lg);
      box-shadow: var(--shadow-lg);
      pointer-events: auto;
      animation: slideIn 0.2s ease-out;
      min-height: 48px;
    }

    /* Toast type colors */
    .toast--error {
      background-color: #fef2f2;
      border-left: 4px solid var(--tg-theme-destructive-text-color);
    }

    .toast--error .toast__icon {
      color: var(--tg-theme-destructive-text-color);
    }

    .toast--success {
      background-color: #f0fdf4;
      border-left: 4px solid #22c55e;
    }

    .toast--success .toast__icon {
      color: #22c55e;
    }

    .toast--warning {
      background-color: #fffbeb;
      border-left: 4px solid #f59e0b;
    }

    .toast--warning .toast__icon {
      color: #f59e0b;
    }

    .toast--info {
      background-color: #eff6ff;
      border-left: 4px solid var(--tg-theme-button-color);
    }

    .toast--info .toast__icon {
      color: var(--tg-theme-button-color);
    }

    /* Dark mode support */
    @media (prefers-color-scheme: dark) {
      .toast--error {
        background-color: #450a0a;
      }

      .toast--success {
        background-color: #052e16;
      }

      .toast--warning {
        background-color: #451a03;
      }

      .toast--info {
        background-color: #1e3a5f;
      }
    }

    .toast__icon {
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;

      svg {
        width: 20px;
        height: 20px;
      }
    }

    .toast__message {
      flex: 1;
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--tg-theme-text-color);
      line-height: 1.4;
    }

    .toast__dismiss {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 32px;
      height: 32px;
      padding: 0;
      border: none;
      background: transparent;
      border-radius: var(--radius-sm);
      cursor: pointer;
      flex-shrink: 0;
      transition: background-color var(--transition-fast);

      &:active {
        background-color: rgba(0, 0, 0, 0.1);
      }

      svg {
        width: 18px;
        height: 18px;
        color: var(--tg-theme-hint-color);
      }
    }

    /* Slide-in animation */
    @keyframes slideIn {
      from {
        transform: translateY(100%);
        opacity: 0;
      }
      to {
        transform: translateY(0);
        opacity: 1;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ErrorToastComponent {
  protected readonly toastService = inject(ErrorToastService);

  /**
   * Dismiss a specific toast notification
   */
  dismiss(toast: Toast): void {
    this.toastService.dismiss(toast.id);
  }
}
