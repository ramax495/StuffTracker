import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  inject,
  computed
} from '@angular/core';
import { TelegramService } from '../../../telegram/telegram.service';
import { LocationTreeNode } from '../../../core/api/location-api.service';

/**
 * Flattened node for rendering the tree as a list
 */
interface FlatTreeNode {
  node: LocationTreeNode;
  depth: number;
  hasChildren: boolean;
  isExpanded: boolean;
}

/**
 * LocationPickerComponent displays a hierarchical tree of locations
 * for selecting a location filter.
 *
 * Features:
 * - Hierarchical tree with expandable/collapsible nodes
 * - Visual indentation based on depth
 * - Selected state with checkmark indicator
 * - "All locations" option to clear selection
 * - Touch-friendly with proper tap targets (48px min)
 * - Telegram theme styling
 * - Bottom sheet presentation on mobile
 */
@Component({
  selector: 'app-location-picker',
  standalone: true,
  template: `
    <!-- Backdrop -->
    <div
      class="location-picker__backdrop"
      (click)="onClose()"
      role="presentation"
    ></div>

    <!-- Bottom sheet / Modal -->
    <div
      class="location-picker__sheet"
      role="dialog"
      aria-modal="true"
      aria-labelledby="location-picker-title"
    >
      <!-- Header -->
      <div class="location-picker__header">
        <h2 id="location-picker-title" class="location-picker__title">
          Select Location
        </h2>
        <button
          type="button"
          class="location-picker__close"
          (click)="onClose()"
          aria-label="Close location picker"
        >
          <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
            <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
          </svg>
        </button>
      </div>

      <!-- Location list -->
      <div class="location-picker__content">
        <!-- All locations option -->
        <button
          type="button"
          class="location-picker__item location-picker__item--all"
          [class.location-picker__item--selected]="!selectedId()"
          (click)="selectLocation(null)"
        >
          <div class="location-picker__item-icon location-picker__item-icon--all">
            <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path d="M4 10.5c-.83 0-1.5.67-1.5 1.5s.67 1.5 1.5 1.5 1.5-.67 1.5-1.5-.67-1.5-1.5-1.5zm0-6c-.83 0-1.5.67-1.5 1.5S3.17 7.5 4 7.5 5.5 6.83 5.5 6 4.83 4.5 4 4.5zm0 12c-.83 0-1.5.68-1.5 1.5s.68 1.5 1.5 1.5 1.5-.68 1.5-1.5-.67-1.5-1.5-1.5zM7 19h14v-2H7v2zm0-6h14v-2H7v2zm0-8v2h14V5H7z"/>
            </svg>
          </div>
          <span class="location-picker__item-name">All locations</span>
          @if (!selectedId()) {
            <svg class="location-picker__checkmark" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"/>
            </svg>
          }
        </button>

        <!-- Tree nodes -->
        @for (flatNode of flattenedTree(); track flatNode.node.id) {
          <button
            type="button"
            class="location-picker__item"
            [class.location-picker__item--selected]="selectedId() === flatNode.node.id"
            [style.padding-left.px]="16 + (flatNode.depth * 24)"
            (click)="selectLocation(flatNode.node)"
          >
            <!-- Expand/collapse toggle -->
            @if (flatNode.hasChildren) {
              <button
                type="button"
                class="location-picker__expand"
                [class.location-picker__expand--expanded]="flatNode.isExpanded"
                (click)="toggleExpand($event, flatNode.node.id)"
                [attr.aria-label]="flatNode.isExpanded ? 'Collapse ' + flatNode.node.name : 'Expand ' + flatNode.node.name"
                aria-expanded="{{ flatNode.isExpanded }}"
              >
                <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                  <path d="M8.59 16.59L13.17 12 8.59 7.41 10 6l6 6-6 6z"/>
                </svg>
              </button>
            } @else {
              <span class="location-picker__expand-placeholder"></span>
            }

            <!-- Folder icon -->
            <div class="location-picker__item-icon">
              <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                <path d="M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z"/>
              </svg>
            </div>

            <!-- Location name -->
            <span class="location-picker__item-name">{{ flatNode.node.name }}</span>

            <!-- Selected checkmark -->
            @if (selectedId() === flatNode.node.id) {
              <svg class="location-picker__checkmark" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"/>
              </svg>
            }
          </button>
        }

        <!-- Empty state -->
        @if (locations().length === 0) {
          <div class="location-picker__empty">
            <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path d="M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z"/>
            </svg>
            <p>No locations available</p>
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: contents;
    }

    .location-picker__backdrop {
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

    .location-picker__sheet {
      position: fixed;
      bottom: 0;
      left: 0;
      right: 0;
      background-color: var(--tg-theme-bg-color);
      border-radius: var(--radius-xl) var(--radius-xl) 0 0;
      z-index: var(--z-modal);
      animation: slideUp 0.3s ease;
      max-height: 80vh;
      display: flex;
      flex-direction: column;
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
      .location-picker__sheet {
        top: 50%;
        left: 50%;
        right: auto;
        bottom: auto;
        transform: translate(-50%, -50%);
        border-radius: var(--radius-xl);
        max-width: 480px;
        width: 90%;
        max-height: 70vh;
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

    .location-picker__header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: var(--spacing-md) var(--spacing-md) var(--spacing-sm);
      border-bottom: 1px solid var(--tg-theme-secondary-bg-color);
      flex-shrink: 0;
    }

    .location-picker__title {
      font-size: 1.125rem;
      font-weight: 600;
      color: var(--tg-theme-text-color);
      margin: 0;
    }

    .location-picker__close {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 40px;
      height: 40px;
      border-radius: var(--radius-full);
      background-color: var(--tg-theme-secondary-bg-color);
      transition: opacity var(--transition-fast);

      &:active {
        opacity: 0.7;
      }

      svg {
        width: 20px;
        height: 20px;
        color: var(--tg-theme-text-color);
      }
    }

    .location-picker__content {
      flex: 1;
      overflow-y: auto;
      padding: var(--spacing-sm) 0;
      padding-bottom: calc(var(--spacing-md) + env(safe-area-inset-bottom, 0px));
    }

    .location-picker__item {
      display: flex;
      align-items: center;
      gap: var(--spacing-sm);
      width: 100%;
      min-height: 52px;
      padding: var(--spacing-sm) var(--spacing-md);
      background-color: transparent;
      text-align: left;
      transition: background-color var(--transition-fast);

      &:active {
        background-color: var(--tg-theme-secondary-bg-color);
      }
    }

    .location-picker__item--selected {
      background-color: color-mix(in srgb, var(--tg-theme-button-color) 10%, transparent);
    }

    .location-picker__item--all {
      padding-left: var(--spacing-md);
      border-bottom: 1px solid var(--tg-theme-secondary-bg-color);
      margin-bottom: var(--spacing-xs);
    }

    .location-picker__expand {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 28px;
      height: 28px;
      flex-shrink: 0;
      border-radius: var(--radius-sm);
      background-color: transparent;
      transition: transform var(--transition-fast), background-color var(--transition-fast);

      &:active {
        background-color: var(--tg-theme-secondary-bg-color);
      }

      svg {
        width: 18px;
        height: 18px;
        color: var(--tg-theme-hint-color);
        transition: transform var(--transition-fast);
      }
    }

    .location-picker__expand--expanded svg {
      transform: rotate(90deg);
    }

    .location-picker__expand-placeholder {
      width: 28px;
      height: 28px;
      flex-shrink: 0;
    }

    .location-picker__item-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 36px;
      height: 36px;
      background-color: var(--tg-theme-button-color);
      border-radius: var(--radius-md);
      flex-shrink: 0;

      svg {
        width: 20px;
        height: 20px;
        color: var(--tg-theme-button-text-color);
      }
    }

    .location-picker__item-icon--all {
      background-color: var(--tg-theme-secondary-bg-color);

      svg {
        color: var(--tg-theme-hint-color);
      }
    }

    .location-picker__item-name {
      flex: 1;
      font-size: 1rem;
      color: var(--tg-theme-text-color);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .location-picker__checkmark {
      width: 22px;
      height: 22px;
      color: var(--tg-theme-button-color);
      flex-shrink: 0;
    }

    .location-picker__empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: var(--spacing-2xl) var(--spacing-md);
      gap: var(--spacing-md);
      text-align: center;

      svg {
        width: 48px;
        height: 48px;
        color: var(--tg-theme-hint-color);
        opacity: 0.5;
      }

      p {
        font-size: 0.875rem;
        color: var(--tg-theme-hint-color);
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LocationPickerComponent {
  private readonly telegramService = inject(TelegramService);

  /** Tree of locations to display */
  readonly locations = input<LocationTreeNode[]>([]);

  /** Currently selected location ID */
  readonly selectedId = input<string | undefined>(undefined);

  /** Emitted when a location is selected (null means "All locations") */
  readonly locationSelected = output<LocationTreeNode | null>();

  /** Emitted when the picker is closed without selection */
  readonly closed = output<void>();

  /** Set of expanded node IDs */
  private readonly expandedIds = signal<Set<string>>(new Set());

  /** Computed flattened tree for rendering */
  readonly flattenedTree = computed(() => {
    const result: FlatTreeNode[] = [];
    const expanded = this.expandedIds();

    const flatten = (nodes: LocationTreeNode[], depth: number): void => {
      for (const node of nodes) {
        const hasChildren = node.children && node.children.length > 0;
        const isExpanded = expanded.has(node.id);

        result.push({
          node,
          depth,
          hasChildren,
          isExpanded
        });

        // Only recurse if expanded
        if (hasChildren && isExpanded) {
          flatten(node.children, depth + 1);
        }
      }
    };

    flatten(this.locations(), 0);
    return result;
  });

  /**
   * Toggle expand/collapse state of a node
   */
  toggleExpand(event: Event, nodeId: string): void {
    event.stopPropagation();
    this.triggerHapticFeedback();

    this.expandedIds.update(ids => {
      const newIds = new Set(ids);
      if (newIds.has(nodeId)) {
        newIds.delete(nodeId);
      } else {
        newIds.add(nodeId);
      }
      return newIds;
    });
  }

  /**
   * Select a location and emit the selection
   */
  selectLocation(node: LocationTreeNode | null): void {
    this.triggerHapticFeedback('selection');
    this.locationSelected.emit(node);
  }

  /**
   * Close the picker without making a selection
   */
  onClose(): void {
    this.triggerHapticFeedback();
    this.closed.emit();
  }

  private triggerHapticFeedback(type: 'light' | 'selection' = 'light'): void {
    if (this.telegramService.isInTelegram()) {
      try {
        if (type === 'selection') {
          // @ts-expect-error - HapticFeedback may not be typed in SDK
          window.Telegram?.WebApp?.HapticFeedback?.selectionChanged();
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
