import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { TelegramService } from '../../../../telegram/telegram.service';

/**
 * LocationHierarchyComponent displays the full location hierarchy (breadcrumbs) for an item.
 *
 * This component:
 * - Renders location names from root to current location
 * - Makes parent locations clickable for navigation
 * - Keeps current location non-clickable
 * - Provides responsive styling for mobile/tablet/desktop
 * - Integrates with Telegram Mini App theme
 * - Triggers haptic feedback on breadcrumb clicks (in Telegram environment)
 *
 * @example
 * <app-location-hierarchy
 *   [breadcrumbs]="location()?.breadcrumbs"
 *   [breadcrumbIds]="location()?.breadcrumbIds"
 *   [currentLocationId]="location()?.id"
 *   (locationSelected)="navigateTo($event)"
 * />
 */
@Component({
  selector: 'app-location-hierarchy',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './location-hierarchy.component.html',
  styleUrl: './location-hierarchy.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LocationHierarchyComponent {
  private telegramService = inject(TelegramService);

  /**
   * Location names from root to current location
   * @example ['Home', 'Kitchen', 'Drawer']
   */
  readonly breadcrumbs = input<string[]>([]);

  /**
   * Location IDs corresponding to each breadcrumb (parallel array)
   * Enables clickable navigation to parent locations
   * @example ['loc-1', 'loc-2', 'loc-3']
   */
  readonly breadcrumbIds = input<string[]>([]);

  /**
   * ID of the current location (last breadcrumb)
   * Used to highlight the current location in the hierarchy
   * @example 'loc-3'
   */
  readonly currentLocationId = input<string>('');

  /**
   * Emitted when user clicks a breadcrumb to navigate to that location
   * Parent component should handle routing
   * @example locationSelected.emit('loc-2')
   */
  readonly locationSelected = output<string>();

  /**
   * Internal signal tracking the last breadcrumb index
   * Used to determine which breadcrumbs are clickable (all except last)
   * @internal
   */
  readonly lastBreadcrumbIndex = signal(0);

  constructor() {
    // Update lastBreadcrumbIndex when breadcrumbs change
    // This helps identify which items are clickable vs not
    this.trackBreadcrumbsLength();
    console.debug('[LocationHierarchyComponent] Initialized with vertical hierarchy layout (mobile-optimized)');
  }

  private trackBreadcrumbsLength(): void {
    const breadcrumbs = this.breadcrumbs();
    if (breadcrumbs.length > 0) {
      const lastIndex = breadcrumbs.length - 1;
      console.debug('[LocationHierarchyComponent] Vertical breadcrumb hierarchy rendering', {
        level_count: breadcrumbs.length,
        hierarchy: breadcrumbs.join(' > '),
        ids_available: this.breadcrumbIds().length > 0,
        depth_levels: Array.from({ length: lastIndex }, (_, i) => `depth-${i}`),
        message: `Rendering ${breadcrumbs.length}-level hierarchy on ${lastIndex === 0 ? 'single' : 'multi'}-level location`
      });
      this.lastBreadcrumbIndex.set(lastIndex);
    }
  }

  /**
   * Navigate to a parent location via breadcrumb click
   * @param locationId The ID of the location to navigate to
   * @param breadcrumbName The display name of the breadcrumb (for logging)
   */
  navigateToLocation(locationId: string, breadcrumbName: string): void {
    console.debug(
      `[LocationHierarchyComponent] Navigating to location: ${breadcrumbName} (${locationId})`
    );
    this.triggerHapticFeedback();
    this.locationSelected.emit(locationId);
  }

  /**
   * Trigger light haptic feedback for navigation action (Telegram only)
   * Safe to call in any environment - gracefully degrades if haptics unavailable
   */
  private triggerHapticFeedback(): void {
    if (this.telegramService.isInTelegram()) {
      try {
        // @ts-expect-error - HapticFeedback may not be typed in SDK
        window.Telegram?.WebApp?.HapticFeedback?.impactOccurred('light');
      } catch {
        // Silently ignore if haptic feedback is not available
        console.debug('[LocationHierarchyComponent] Haptic feedback unavailable');
      }
    }
  }

  /**
   * Check if a breadcrumb at the given index is the last one
   * Last breadcrumb is non-clickable (current location)
   */
  isLastBreadcrumb(index: number): boolean {
    return index === this.lastBreadcrumbIndex();
  }

  /**
   * Check if a breadcrumb has a corresponding ID for navigation
   */
  hasNavigationId(index: number): boolean {
    const ids = this.breadcrumbIds();
    return ids && ids.length > index && ids[index] != null;
  }

  /**
   * Get the navigation ID for a breadcrumb
   */
  getNavigationId(index: number): string {
    const ids = this.breadcrumbIds();
    return ids?.[index] ?? '';
  }
}
