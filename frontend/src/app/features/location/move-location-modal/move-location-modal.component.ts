import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  inject,
  OnInit,
  DestroyRef,
  computed
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  LocationApiService,
  LocationTreeNode,
  LocationResponse
} from '../../../core/api/location-api.service';
import { TelegramService } from '../../../telegram/telegram.service';

/**
 * Flattened node for rendering the tree as a list
 */
interface FlatTreeNode {
  node: LocationTreeNode;
  depth: number;
  hasChildren: boolean;
  isExpanded: boolean;
  isDisabled: boolean;
  isCurrent: boolean;
}

/**
 * MoveLocationModalComponent displays a modal for moving a location to a different parent.
 *
 * Features:
 * - Uses hierarchical tree display for location selection
 * - Filters out the location being moved and all its descendants (prevents cycles)
 * - Shows "(current)" badge on current parent location
 * - Allows selecting "No parent (root)" option
 * - Loading state during move operation
 * - Error handling with user-friendly messages
 * - Mobile-first bottom sheet style
 * - Touch-friendly tap targets
 * - Haptic feedback for Telegram Mini App
 */
@Component({
  selector: 'app-move-location-modal',
  standalone: true,
  imports: [],
  template: `
    <!-- Backdrop -->
    <div
      class="move-location-modal__backdrop"
      (click)="onClose()"
      role="presentation"
    ></div>

    <!-- Bottom sheet / Modal -->
    <div
      class="move-location-modal__sheet"
      role="dialog"
      aria-modal="true"
      aria-labelledby="move-location-title"
    >
      <!-- Header -->
      <div class="move-location-modal__header">
        <div class="move-location-modal__header-content">
          <h2 id="move-location-title" class="move-location-modal__title">
            Move Location
          </h2>
          @if (locationName()) {
            <p class="move-location-modal__subtitle">{{ locationName() }}</p>
          }
        </div>
        <button
          type="button"
          class="move-location-modal__close"
          (click)="onClose()"
          aria-label="Close move dialog"
        >
          <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
            <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
          </svg>
        </button>
      </div>

      <!-- Loading state for tree -->
      @if (isLoadingTree()) {
        <div class="move-location-modal__loading">
          <div class="move-location-modal__spinner"></div>
          <p class="move-location-modal__loading-text">Loading locations...</p>
        </div>
      }

      <!-- Error state -->
      @if (error()) {
        <div class="move-location-modal__error">
          <svg class="move-location-modal__error-icon" viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z"/>
          </svg>
          <p class="move-location-modal__error-text">{{ error() }}</p>
          <button
            type="button"
            class="move-location-modal__retry-btn"
            (click)="loadLocationTree()"
          >
            Try Again
          </button>
        </div>
      }

      <!-- Location list -->
      @if (!isLoadingTree() && !error()) {
        <div class="move-location-modal__content">
          <!-- Root option (no parent) -->
          <button
            type="button"
            class="move-location-modal__item move-location-modal__item--root"
            [class.move-location-modal__item--selected]="selectedParentId() === null"
            [class.move-location-modal__item--disabled]="currentParentId() === null"
            (click)="selectRootLevel()"
            [disabled]="currentParentId() === null"
          >
            <!-- Home/root icon -->
            <span class="move-location-modal__expand-placeholder"></span>
            <div class="move-location-modal__item-icon move-location-modal__item-icon--root">
              <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                <path d="M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z"/>
              </svg>
            </div>
            <span class="move-location-modal__item-name">
              No parent (root level)
              @if (currentParentId() === null) {
                <span class="move-location-modal__current-badge">(current)</span>
              }
            </span>
            @if (selectedParentId() === null) {
              <svg class="move-location-modal__checkmark" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"/>
              </svg>
            }
          </button>

          <!-- Location tree -->
          @for (flatNode of flattenedTree(); track flatNode.node.id) {
            <button
              type="button"
              class="move-location-modal__item"
              [class.move-location-modal__item--selected]="selectedParentId() === flatNode.node.id"
              [class.move-location-modal__item--disabled]="flatNode.isDisabled"
              [style.padding-left.px]="16 + (flatNode.depth * 24)"
              (click)="selectLocation(flatNode.node)"
              [disabled]="flatNode.isDisabled"
            >
              <!-- Expand/collapse toggle -->
              @if (flatNode.hasChildren) {
                <button
                  type="button"
                  class="move-location-modal__expand"
                  [class.move-location-modal__expand--expanded]="flatNode.isExpanded"
                  (click)="toggleExpand($event, flatNode.node.id)"
                  [attr.aria-label]="flatNode.isExpanded ? 'Collapse ' + flatNode.node.name : 'Expand ' + flatNode.node.name"
                  aria-expanded="{{ flatNode.isExpanded }}"
                >
                  <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                    <path d="M8.59 16.59L13.17 12 8.59 7.41 10 6l6 6-6 6z"/>
                  </svg>
                </button>
              } @else {
                <span class="move-location-modal__expand-placeholder"></span>
              }

              <!-- Folder icon -->
              <div class="move-location-modal__item-icon">
                <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                  <path d="M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z"/>
                </svg>
              </div>

              <!-- Location name -->
              <span class="move-location-modal__item-name">
                {{ flatNode.node.name }}
                @if (flatNode.isCurrent) {
                  <span class="move-location-modal__current-badge">(current)</span>
                }
                @if (flatNode.node.id === locationId()) {
                  <span class="move-location-modal__self-badge">(this location)</span>
                }
              </span>

              <!-- Selected checkmark -->
              @if (selectedParentId() === flatNode.node.id) {
                <svg class="move-location-modal__checkmark" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                  <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"/>
                </svg>
              }
            </button>
          }

          <!-- Empty state -->
          @if (locationTree().length === 0) {
            <div class="move-location-modal__empty">
              <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                <path d="M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z"/>
              </svg>
              <p>No other locations available</p>
            </div>
          }
        </div>

        <!-- Actions -->
        <div class="move-location-modal__actions">
          <button
            type="button"
            class="move-location-modal__btn move-location-modal__btn--cancel"
            (click)="onClose()"
            [disabled]="isMoving()"
          >
            Cancel
          </button>
          <button
            type="button"
            class="move-location-modal__btn move-location-modal__btn--confirm"
            (click)="confirmMove()"
            [disabled]="!canMove() || isMoving()"
          >
            @if (isMoving()) {
              <div class="move-location-modal__btn-spinner"></div>
              Moving...
            } @else {
              {{ confirmButtonText() }}
            }
          </button>
        </div>
      }
    </div>
  `,
  styles: [`
    :host {
      display: contents;
    }

    .move-location-modal__backdrop {
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

    .move-location-modal__sheet {
      position: fixed;
      bottom: 0;
      left: 0;
      right: 0;
      background-color: var(--tg-theme-bg-color);
      border-radius: var(--radius-xl) var(--radius-xl) 0 0;
      z-index: var(--z-modal);
      animation: slideUp 0.3s ease;
      max-height: 85vh;
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
      .move-location-modal__sheet {
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

    .move-location-modal__header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      padding: var(--spacing-md) var(--spacing-md) var(--spacing-sm);
      border-bottom: 1px solid var(--tg-theme-secondary-bg-color);
      flex-shrink: 0;
    }

    .move-location-modal__header-content {
      flex: 1;
      min-width: 0;
    }

    .move-location-modal__title {
      font-size: 1.125rem;
      font-weight: 600;
      color: var(--tg-theme-text-color);
      margin: 0;
    }

    .move-location-modal__subtitle {
      font-size: 0.875rem;
      color: var(--tg-theme-hint-color);
      margin: 4px 0 0 0;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .move-location-modal__close {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 40px;
      height: 40px;
      border-radius: var(--radius-full);
      background-color: var(--tg-theme-secondary-bg-color);
      border: none;
      cursor: pointer;
      transition: opacity var(--transition-fast);
      flex-shrink: 0;

      &:active {
        opacity: 0.7;
      }

      svg {
        width: 20px;
        height: 20px;
        color: var(--tg-theme-text-color);
      }
    }

    /* Loading state */
    .move-location-modal__loading {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: var(--spacing-2xl) var(--spacing-md);
      gap: var(--spacing-md);
    }

    .move-location-modal__spinner {
      width: 32px;
      height: 32px;
      border: 3px solid var(--tg-theme-secondary-bg-color);
      border-top-color: var(--tg-theme-button-color);
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .move-location-modal__loading-text {
      font-size: 0.875rem;
      color: var(--tg-theme-hint-color);
    }

    /* Error state */
    .move-location-modal__error {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: var(--spacing-2xl) var(--spacing-md);
      gap: var(--spacing-md);
      text-align: center;
    }

    .move-location-modal__error-icon {
      width: 48px;
      height: 48px;
      color: var(--tg-theme-destructive-text-color);
    }

    .move-location-modal__error-text {
      font-size: 0.875rem;
      color: var(--tg-theme-hint-color);
      max-width: 280px;
    }

    .move-location-modal__retry-btn {
      padding: var(--spacing-sm) var(--spacing-md);
      background-color: var(--tg-theme-secondary-bg-color);
      color: var(--tg-theme-text-color);
      border: none;
      border-radius: var(--radius-md);
      font-size: 0.875rem;
      cursor: pointer;
      min-height: 44px;

      &:active {
        opacity: 0.7;
      }
    }

    /* Content */
    .move-location-modal__content {
      flex: 1;
      overflow-y: auto;
      padding: var(--spacing-sm) 0;
    }

    .move-location-modal__item {
      display: flex;
      align-items: center;
      gap: var(--spacing-sm);
      width: 100%;
      min-height: 52px;
      padding: var(--spacing-sm) var(--spacing-md);
      background-color: transparent;
      border: none;
      text-align: left;
      cursor: pointer;
      transition: background-color var(--transition-fast);

      &:active:not(:disabled) {
        background-color: var(--tg-theme-secondary-bg-color);
      }
    }

    .move-location-modal__item--root {
      border-bottom: 1px solid var(--tg-theme-secondary-bg-color);
      margin-bottom: var(--spacing-xs);
    }

    .move-location-modal__item--selected {
      background-color: color-mix(in srgb, var(--tg-theme-button-color) 10%, transparent);
    }

    .move-location-modal__item--disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .move-location-modal__expand {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 28px;
      height: 28px;
      flex-shrink: 0;
      border-radius: var(--radius-sm);
      background-color: transparent;
      border: none;
      cursor: pointer;
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

    .move-location-modal__expand--expanded svg {
      transform: rotate(90deg);
    }

    .move-location-modal__expand-placeholder {
      width: 28px;
      height: 28px;
      flex-shrink: 0;
    }

    .move-location-modal__item-icon {
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

    .move-location-modal__item-icon--root {
      background-color: var(--tg-theme-secondary-bg-color);

      svg {
        color: var(--tg-theme-hint-color);
      }
    }

    .move-location-modal__item-name {
      flex: 1;
      font-size: 1rem;
      color: var(--tg-theme-text-color);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .move-location-modal__current-badge {
      font-size: 0.75rem;
      color: var(--tg-theme-hint-color);
      margin-left: var(--spacing-xs);
    }

    .move-location-modal__self-badge {
      font-size: 0.75rem;
      color: var(--tg-theme-destructive-text-color);
      margin-left: var(--spacing-xs);
    }

    .move-location-modal__checkmark {
      width: 22px;
      height: 22px;
      color: var(--tg-theme-button-color);
      flex-shrink: 0;
    }

    .move-location-modal__empty {
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
        margin: 0;
      }
    }

    /* Actions */
    .move-location-modal__actions {
      display: flex;
      gap: var(--spacing-sm);
      padding: var(--spacing-md);
      padding-bottom: calc(var(--spacing-md) + env(safe-area-inset-bottom, 0px));
      border-top: 1px solid var(--tg-theme-secondary-bg-color);
      flex-shrink: 0;
    }

    .move-location-modal__btn {
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: var(--spacing-sm);
      padding: var(--spacing-md);
      font-size: 1rem;
      font-weight: 500;
      border: none;
      border-radius: var(--radius-md);
      cursor: pointer;
      transition: opacity var(--transition-fast), transform var(--transition-fast);
      min-height: 52px;

      &:active:not(:disabled) {
        transform: scale(0.98);
      }

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }

    .move-location-modal__btn--cancel {
      background-color: var(--tg-theme-secondary-bg-color);
      color: var(--tg-theme-text-color);
    }

    .move-location-modal__btn--confirm {
      background-color: var(--tg-theme-button-color);
      color: var(--tg-theme-button-text-color);
    }

    .move-location-modal__btn-spinner {
      width: 18px;
      height: 18px;
      border: 2px solid rgba(255, 255, 255, 0.3);
      border-top-color: currentColor;
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MoveLocationModalComponent implements OnInit {
  private readonly locationApiService = inject(LocationApiService);
  private readonly telegramService = inject(TelegramService);
  private readonly destroyRef = inject(DestroyRef);

  /** ID of the location to move */
  readonly locationId = input.required<string>();

  /** Current parent ID of the location (null if root level) */
  readonly currentParentId = input.required<string | null>();

  /** Name of the location (for display in header) */
  readonly locationName = input<string>('');

  /** Emitted when location is successfully moved */
  readonly moved = output<LocationResponse>();

  /** Emitted when modal is closed without moving */
  readonly closed = output<void>();

  /** Location tree data */
  readonly locationTree = signal<LocationTreeNode[]>([]);

  /** Loading state for tree */
  readonly isLoadingTree = signal(true);

  /** Loading state for move operation */
  readonly isMoving = signal(false);

  /** Error message */
  readonly error = signal<string | null>(null);

  /** Currently selected parent ID (null means root level) */
  readonly selectedParentId = signal<string | null | undefined>(undefined);

  /** Selected location node name */
  readonly selectedLocationName = signal<string | null>(null);

  /** Set of expanded node IDs */
  private readonly expandedIds = signal<Set<string>>(new Set());

  /** Set of IDs that are disabled (self and descendants) */
  private readonly disabledIds = signal<Set<string>>(new Set());

  /** Computed flattened tree for rendering */
  readonly flattenedTree = computed(() => {
    const result: FlatTreeNode[] = [];
    const expanded = this.expandedIds();
    const disabled = this.disabledIds();
    const currentParent = this.currentParentId();
    const selfId = this.locationId();

    const flatten = (nodes: LocationTreeNode[], depth: number): void => {
      for (const node of nodes) {
        const hasChildren = node.children && node.children.length > 0;
        const isExpanded = expanded.has(node.id);
        const isDisabled = disabled.has(node.id);
        const isCurrent = node.id === currentParent;

        result.push({
          node,
          depth,
          hasChildren,
          isExpanded,
          isDisabled,
          isCurrent
        });

        // Only recurse if expanded and not the self node (descendants are always collapsed if disabled)
        if (hasChildren && isExpanded && node.id !== selfId) {
          flatten(node.children, depth + 1);
        }
      }
    };

    flatten(this.locationTree(), 0);
    return result;
  });

  /** Whether a move can be performed */
  readonly canMove = computed(() => {
    const selectedId = this.selectedParentId();
    const currentId = this.currentParentId();

    // Must have made a selection (not undefined)
    if (selectedId === undefined) {
      return false;
    }

    // Can't move to current parent (same location)
    if (selectedId === currentId) {
      return false;
    }

    return true;
  });

  /** Confirm button text */
  readonly confirmButtonText = computed(() => {
    const selectedId = this.selectedParentId();
    if (selectedId === undefined) {
      return 'Select location';
    }
    if (selectedId === null) {
      return 'Move to root level';
    }
    const name = this.selectedLocationName();
    if (name) {
      return `Move to ${name}`;
    }
    return 'Move';
  });

  ngOnInit(): void {
    this.loadLocationTree();
  }

  /**
   * Load the location tree from API
   */
  loadLocationTree(): void {
    this.isLoadingTree.set(true);
    this.error.set(null);

    this.locationApiService
      .getLocationTree()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (tree) => {
          this.locationTree.set(tree);
          this.isLoadingTree.set(false);

          // Build set of disabled IDs (self and descendants)
          const disabled = this.collectSelfAndDescendants(tree, this.locationId());
          this.disabledIds.set(disabled);

          // Auto-expand to show the current parent
          this.expandPathToLocation(tree, this.currentParentId());
        },
        error: (err) => {
          this.error.set(err.message || 'Failed to load locations');
          this.isLoadingTree.set(false);
        }
      });
  }

  /**
   * Collect the location being moved and all its descendants
   * These will be disabled in the tree to prevent cycles
   */
  private collectSelfAndDescendants(nodes: LocationTreeNode[], targetId: string): Set<string> {
    const result = new Set<string>();

    const findAndCollect = (nodes: LocationTreeNode[]): boolean => {
      for (const node of nodes) {
        if (node.id === targetId) {
          // Found the target, collect it and all descendants
          this.collectAllDescendants(node, result);
          return true;
        }
        if (node.children && node.children.length > 0) {
          if (findAndCollect(node.children)) {
            return true;
          }
        }
      }
      return false;
    };

    findAndCollect(nodes);
    return result;
  }

  /**
   * Recursively collect a node and all its descendants into a set
   */
  private collectAllDescendants(node: LocationTreeNode, result: Set<string>): void {
    result.add(node.id);
    if (node.children && node.children.length > 0) {
      for (const child of node.children) {
        this.collectAllDescendants(child, result);
      }
    }
  }

  /**
   * Expand the path to a specific location
   */
  private expandPathToLocation(nodes: LocationTreeNode[], targetId: string | null): boolean {
    if (targetId === null) {
      return false;
    }

    for (const node of nodes) {
      if (node.id === targetId) {
        return true;
      }
      if (node.children && node.children.length > 0) {
        if (this.expandPathToLocation(node.children, targetId)) {
          this.expandedIds.update(ids => {
            const newIds = new Set(ids);
            newIds.add(node.id);
            return newIds;
          });
          return true;
        }
      }
    }
    return false;
  }

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
   * Select root level (no parent)
   */
  selectRootLevel(): void {
    if (this.currentParentId() === null) {
      return; // Already at root level
    }

    this.triggerHapticFeedback('selection');
    this.selectedParentId.set(null);
    this.selectedLocationName.set(null);
  }

  /**
   * Select a location as the new parent
   */
  selectLocation(node: LocationTreeNode): void {
    const disabled = this.disabledIds();
    if (disabled.has(node.id)) {
      return;
    }

    if (node.id === this.currentParentId()) {
      return; // Already the current parent
    }

    this.triggerHapticFeedback('selection');
    this.selectedParentId.set(node.id);
    this.selectedLocationName.set(node.name);
  }

  /**
   * Confirm the move operation
   */
  confirmMove(): void {
    const selectedId = this.selectedParentId();
    if (selectedId === undefined || !this.canMove()) {
      return;
    }

    this.isMoving.set(true);
    this.error.set(null);

    this.locationApiService
      .moveLocation(this.locationId(), selectedId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.isMoving.set(false);
          this.moved.emit(response);
        },
        error: (err) => {
          this.error.set(err.message || 'Failed to move location');
          this.isMoving.set(false);
          this.triggerHapticFeedback('error');
        }
      });
  }

  /**
   * Close the modal
   */
  onClose(): void {
    if (this.isMoving()) {
      return;
    }
    this.triggerHapticFeedback();
    this.closed.emit();
  }

  private triggerHapticFeedback(type: 'light' | 'selection' | 'error' = 'light'): void {
    if (this.telegramService.isInTelegram()) {
      try {
        if (type === 'selection') {
          // @ts-expect-error - HapticFeedback may not be typed in SDK
          window.Telegram?.WebApp?.HapticFeedback?.selectionChanged();
        } else if (type === 'error') {
          // @ts-expect-error - HapticFeedback may not be typed in SDK
          window.Telegram?.WebApp?.HapticFeedback?.notificationOccurred('error');
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
