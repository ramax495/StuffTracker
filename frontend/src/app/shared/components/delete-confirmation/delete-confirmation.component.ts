import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  inject
} from '@angular/core';
import { TelegramService } from '../../../telegram/telegram.service';

/**
 * Data model for delete confirmation
 */
export interface DeleteConfirmationData {
  /** Name of the item being deleted */
  name: string;
  /** Number of direct child locations */
  childCount: number;
  /** Number of direct items */
  itemCount: number;
  /** Total items in all descendants (including nested) */
  totalDescendantItems: number;
}

/**
 * Delete confirmation component/dialog
 *
 * Features:
 * - Warning message about cascade delete
 * - Shows counts of affected children and items
 * - Confirm/Cancel buttons
 * - Can be used as modal overlay or bottom sheet
 * - Telegram theme styling
 */
@Component({
  selector: 'app-delete-confirmation',
  standalone: true,
  template: `
    <!-- Backdrop -->
    <div
      class="delete-confirmation__backdrop"
      (click)="onCancel()"
      role="presentation"
    ></div>

    <!-- Dialog -->
    <div
      class="delete-confirmation__dialog"
      role="alertdialog"
      aria-modal="true"
      [attr.aria-labelledby]="'delete-title'"
      [attr.aria-describedby]="'delete-description'"
    >
      <!-- Warning icon -->
      <div class="delete-confirmation__icon">
        <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
          <path d="M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z"/>
        </svg>
      </div>

      <!-- Title -->
      <h2 id="delete-title" class="delete-confirmation__title">
        Delete "{{ data().name }}"?
      </h2>

      <!-- Description -->
      <div id="delete-description" class="delete-confirmation__description">
        @if (hasContent()) {
          <p class="delete-confirmation__warning">
            This action cannot be undone. The following will be permanently deleted:
          </p>
          <ul class="delete-confirmation__list">
            @if (data().childCount > 0) {
              <li>
                <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                  <path d="M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z"/>
                </svg>
                {{ data().childCount }} sub-location{{ data().childCount > 1 ? 's' : '' }}
              </li>
            }
            @if (data().itemCount > 0) {
              <li>
                <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                  <path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V5h14v14zm-5-7l-3 3.72L9 13l-3 4h12l-4-5z"/>
                </svg>
                {{ data().itemCount }} item{{ data().itemCount > 1 ? 's' : '' }} in this location
              </li>
            }
            @if (data().totalDescendantItems > data().itemCount) {
              <li class="delete-confirmation__nested">
                <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                  <path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V5h14v14zm-5-7l-3 3.72L9 13l-3 4h12l-4-5z"/>
                </svg>
                {{ data().totalDescendantItems - data().itemCount }} item{{ (data().totalDescendantItems - data().itemCount) > 1 ? 's' : '' }} in nested locations
              </li>
            }
          </ul>
        } @else {
          <p class="delete-confirmation__simple">
            This action cannot be undone.
          </p>
        }
      </div>

      <!-- Actions -->
      <div class="delete-confirmation__actions">
        <button
          type="button"
          class="delete-confirmation__btn delete-confirmation__btn--cancel"
          (click)="onCancel()"
        >
          Cancel
        </button>
        <button
          type="button"
          class="delete-confirmation__btn delete-confirmation__btn--confirm"
          (click)="onConfirm()"
        >
          Delete
        </button>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: contents;
    }

    .delete-confirmation__backdrop {
      position: fixed;
      inset: 0;
      background-color: rgba(0, 0, 0, 0.5);
      z-index: var(--z-modal-backdrop);
      animation: fadeIn 0.2s ease;
    }

    @keyframes fadeIn {
      from { opacity: 0; }
      to { opacity: 1; }
    }

    .delete-confirmation__dialog {
      position: fixed;
      bottom: 0;
      left: 0;
      right: 0;
      background-color: var(--tg-theme-bg-color);
      border-radius: var(--radius-xl) var(--radius-xl) 0 0;
      padding: var(--spacing-lg);
      padding-bottom: calc(var(--spacing-lg) + env(safe-area-inset-bottom, 0px));
      z-index: var(--z-modal);
      animation: slideUp 0.3s ease;
      max-height: 90vh;
      overflow-y: auto;
    }

    @keyframes slideUp {
      from {
        transform: translateY(100%);
        opacity: 0;
      }
      to {
        transform: translateY(0);
        opacity: 1;
      }
    }

    /* Tablet and desktop: center the dialog */
    @media (min-width: 600px) {
      .delete-confirmation__dialog {
        top: 50%;
        left: 50%;
        right: auto;
        bottom: auto;
        transform: translate(-50%, -50%);
        border-radius: var(--radius-xl);
        max-width: 400px;
        width: 90%;
        animation: scaleIn 0.2s ease;
      }

      @keyframes scaleIn {
        from {
          opacity: 0;
          transform: translate(-50%, -50%) scale(0.95);
        }
        to {
          opacity: 1;
          transform: translate(-50%, -50%) scale(1);
        }
      }
    }

    .delete-confirmation__icon {
      display: flex;
      justify-content: center;
      margin-bottom: var(--spacing-md);

      svg {
        width: 48px;
        height: 48px;
        color: var(--tg-theme-destructive-text-color);
      }
    }

    .delete-confirmation__title {
      font-size: 1.25rem;
      font-weight: 600;
      color: var(--tg-theme-text-color);
      text-align: center;
      margin-bottom: var(--spacing-md);
      word-break: break-word;
    }

    .delete-confirmation__description {
      margin-bottom: var(--spacing-lg);
    }

    .delete-confirmation__warning,
    .delete-confirmation__simple {
      font-size: 0.875rem;
      color: var(--tg-theme-hint-color);
      text-align: center;
      margin-bottom: var(--spacing-md);
    }

    .delete-confirmation__list {
      display: flex;
      flex-direction: column;
      gap: var(--spacing-sm);
      padding: var(--spacing-md);
      background-color: var(--tg-theme-secondary-bg-color);
      border-radius: var(--radius-md);

      li {
        display: flex;
        align-items: center;
        gap: var(--spacing-sm);
        font-size: 0.875rem;
        color: var(--tg-theme-text-color);

        svg {
          width: 18px;
          height: 18px;
          flex-shrink: 0;
          color: var(--tg-theme-hint-color);
        }
      }
    }

    .delete-confirmation__nested {
      padding-left: var(--spacing-md);
      opacity: 0.8;
    }

    .delete-confirmation__actions {
      display: flex;
      gap: var(--spacing-sm);
    }

    .delete-confirmation__btn {
      flex: 1;
      padding: var(--spacing-md);
      font-size: 1rem;
      font-weight: 500;
      border: none;
      border-radius: var(--radius-md);
      cursor: pointer;
      transition: opacity var(--transition-fast), transform var(--transition-fast);
      min-height: 52px;

      &:active {
        transform: scale(0.98);
      }
    }

    .delete-confirmation__btn--cancel {
      background-color: var(--tg-theme-secondary-bg-color);
      color: var(--tg-theme-text-color);
    }

    .delete-confirmation__btn--confirm {
      background-color: var(--tg-theme-destructive-text-color);
      color: #ffffff;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DeleteConfirmationComponent {
  private readonly telegramService = inject(TelegramService);

  /** Data about what is being deleted */
  readonly data = input.required<DeleteConfirmationData>();

  /** Emitted when user confirms deletion */
  readonly confirm = output<void>();

  /** Emitted when user cancels */
  readonly cancel = output<void>();

  /**
   * Check if location has any content
   */
  hasContent(): boolean {
    const d = this.data();
    return d.childCount > 0 || d.itemCount > 0 || d.totalDescendantItems > 0;
  }

  /**
   * Handle confirm button click
   */
  onConfirm(): void {
    this.triggerHapticFeedback('warning');
    this.confirm.emit();
  }

  /**
   * Handle cancel button click or backdrop click
   */
  onCancel(): void {
    this.triggerHapticFeedback('light');
    this.cancel.emit();
  }

  private triggerHapticFeedback(type: 'light' | 'warning' = 'light'): void {
    if (this.telegramService.isInTelegram()) {
      try {
        if (type === 'warning') {
          // @ts-expect-error - HapticFeedback may not be typed in SDK
          window.Telegram?.WebApp?.HapticFeedback?.notificationOccurred('warning');
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
