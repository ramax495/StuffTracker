import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';

/**
 * Preset icon types for common empty states
 */
export type EmptyStateIcon = 'empty-locations' | 'empty-items' | 'empty-search' | 'no-results' | string;

/**
 * Empty state component for displaying when lists have no data
 *
 * Features:
 * - Preset SVG icons for common scenarios
 * - Custom emoji or icon string support
 * - Optional action button with event output
 * - Telegram theme integration
 *
 * @example
 * ```html
 * <app-empty-state
 *   icon="empty-items"
 *   title="No items yet"
 *   message="Add your first item to this location"
 *   actionLabel="Add Item"
 *   (action)="onAddItem()"
 * />
 * ```
 */
@Component({
  selector: 'app-empty-state',
  standalone: true,
  template: `
    <div class="empty-state">
      <!-- Icon display -->
      <div class="empty-state__icon">
        @switch (icon()) {
          @case ('empty-locations') {
            <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path d="M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z"/>
            </svg>
          }
          @case ('empty-items') {
            <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path d="M20 6h-8l-2-2H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2zm-2.06 11L15 14.06 12.06 17l-1.41-1.41L13.59 12.65 10.65 9.71l1.41-1.41L15 11.24l2.94-2.94 1.41 1.41L16.41 12.65l2.94 2.94-1.41 1.41z"/>
            </svg>
          }
          @case ('empty-search') {
            <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/>
            </svg>
          }
          @case ('no-results') {
            <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z" opacity="0.3"/>
              <path d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14zM7.5 8h4v1.5h-4z"/>
            </svg>
          }
          @default {
            <!-- Display as emoji or custom character -->
            <span class="empty-state__emoji" aria-hidden="true">{{ icon() }}</span>
          }
        }
      </div>

      <!-- Title -->
      <h2 class="empty-state__title">{{ title() }}</h2>

      <!-- Message -->
      @if (message()) {
        <p class="empty-state__message">{{ message() }}</p>
      }

      <!-- Optional action button -->
      @if (actionLabel()) {
        <button
          type="button"
          class="btn btn-primary empty-state__action"
          (click)="onActionClick()"
        >
          {{ actionLabel() }}
        </button>
      }
    </div>
  `,
  styles: [`
    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: var(--spacing-2xl) var(--spacing-md);
      gap: var(--spacing-md);
      text-align: center;
    }

    .empty-state__icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 80px;
      height: 80px;
      border-radius: var(--radius-full);
      background-color: var(--tg-theme-secondary-bg-color);
      margin-bottom: var(--spacing-sm);

      svg {
        width: 40px;
        height: 40px;
        color: var(--tg-theme-hint-color);
        opacity: 0.6;
      }
    }

    .empty-state__emoji {
      font-size: 2.5rem;
      line-height: 1;
    }

    .empty-state__title {
      font-size: 1.25rem;
      font-weight: 600;
      color: var(--tg-theme-text-color);
      margin: 0;
    }

    .empty-state__message {
      font-size: 0.875rem;
      color: var(--tg-theme-hint-color);
      max-width: 280px;
      line-height: 1.5;
      margin: 0;
    }

    .empty-state__action {
      margin-top: var(--spacing-sm);
      min-width: 160px;
      min-height: 44px;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EmptyStateComponent {
  /** Icon to display - can be a preset name or emoji/custom character */
  readonly icon = input.required<EmptyStateIcon>();

  /** Title text for the empty state */
  readonly title = input.required<string>();

  /** Optional descriptive message */
  readonly message = input<string>('');

  /** Optional action button label - if provided, button will be shown */
  readonly actionLabel = input<string>('');

  /** Event emitted when action button is clicked */
  readonly action = output<void>();

  /**
   * Handle action button click
   */
  onActionClick(): void {
    this.action.emit();
  }
}
