import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  OnInit,
  OnDestroy,
  DestroyRef
} from '@angular/core';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { LocationCardComponent } from '../../shared/components/location-card';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner';
import { EmptyStateComponent } from '../../shared/components/empty-state';
import { LocationApiService, LocationListItem } from '../../core/api/location-api.service';
import { TelegramService } from '../../telegram/telegram.service';
import { ErrorToastService } from '../../shared/components/error-toast';

/**
 * Home component displaying top-level locations
 *
 * Features:
 * - Loads and displays top-level locations on init
 * - "Add Location" button using Telegram MainButton
 * - Empty state when no locations exist
 * - Loading state during data fetch
 */
@Component({
  selector: 'app-home',
  standalone: true,
  imports: [LocationCardComponent, LoadingSpinnerComponent, EmptyStateComponent],
  template: `
    <div class="home-container">
      <!-- Header -->
      <header class="home-header">
        <h1 class="home-title">StuffTracker</h1>
        <p class="home-subtitle">Organize and track your belongings</p>
      </header>

      <!-- Loading state -->
      @if (isLoading()) {
        <app-loading-spinner size="medium" message="Loading locations..." />
      }

      <!-- Error state -->
      @if (error()) {
        <div class="home-error">
          <svg class="home-error__icon" viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z"/>
          </svg>
          <p class="home-error__text">{{ error() }}</p>
          <button
            type="button"
            class="btn btn-secondary"
            (click)="loadLocations()"
          >
            Try Again
          </button>
        </div>
      }

      <!-- Empty state -->
      @if (!isLoading() && !error() && locations().length === 0) {
        <app-empty-state
          icon="empty-locations"
          title="No locations yet"
          message="Create your first location to start organizing your stuff."
          actionLabel="Add Location"
          (action)="navigateToCreateLocation()"
        />
      }

      <!-- Locations list -->
      @if (!isLoading() && !error() && locations().length > 0) {
        <section class="home-locations">
          <h2 class="home-locations__title">Your Locations</h2>

          <!-- Quick action section -->
          <div class="home-quick-actions">
            <button
              type="button"
              class="home-quick-action"
              (click)="navigateToCreateLocation()"
            >
              <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                <path d="M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z"/>
              </svg>
              <span>Add Location</span>
            </button>
            <button
              type="button"
              class="home-quick-action home-quick-action--secondary"
              (click)="navigateToAddItem()"
            >
              <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                <path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/>
              </svg>
              <span>Add Item</span>
            </button>
          </div>

          <div class="home-locations__grid">
            @for (location of locations(); track location.id) {
              <app-location-card [location]="location" />
            }
          </div>
        </section>
      }
    </div>
  `,
  styles: [`
    .home-container {
      display: flex;
      flex-direction: column;
      min-height: 100%;
      padding: var(--spacing-md);
      padding-bottom: calc(var(--spacing-xl) + 60px); /* Space for MainButton */
    }

    .home-header {
      text-align: center;
      padding: var(--spacing-lg) 0;
    }

    .home-title {
      font-size: 1.75rem;
      font-weight: 700;
      color: var(--tg-theme-text-color);
      margin-bottom: var(--spacing-xs);
    }

    .home-subtitle {
      font-size: 0.875rem;
      color: var(--tg-theme-hint-color);
    }

    /* Error state */
    .home-error {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: var(--spacing-2xl) var(--spacing-md);
      gap: var(--spacing-md);
      text-align: center;
    }

    .home-error__icon {
      width: 48px;
      height: 48px;
      color: var(--tg-theme-destructive-text-color);
    }

    .home-error__text {
      font-size: 0.875rem;
      color: var(--tg-theme-hint-color);
      max-width: 280px;
    }

    /* Locations list */
    .home-locations {
      display: flex;
      flex-direction: column;
      gap: var(--spacing-md);
    }

    .home-locations__title {
      font-size: 1rem;
      font-weight: 600;
      color: var(--tg-theme-section-header-text-color);
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .home-locations__grid {
      display: flex;
      flex-direction: column;
      gap: var(--spacing-sm);
    }

    /* Quick actions section */
    .home-quick-actions {
      display: flex;
      gap: var(--spacing-sm);
      padding: var(--spacing-sm) 0;
      margin-bottom: var(--spacing-sm);
    }

    .home-quick-action {
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: var(--spacing-sm);
      padding: var(--spacing-md);
      background-color: var(--tg-theme-button-color);
      color: var(--tg-theme-button-text-color);
      border: none;
      border-radius: var(--radius-md);
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      transition: opacity var(--transition-fast), transform var(--transition-fast);
      min-height: 48px;

      svg {
        width: 20px;
        height: 20px;
        flex-shrink: 0;
      }

      span {
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }

      &:active {
        opacity: 0.9;
        transform: scale(0.98);
      }
    }

    .home-quick-action--secondary {
      background-color: var(--tg-theme-secondary-bg-color);
      color: var(--tg-theme-text-color);
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HomeComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly locationApiService = inject(LocationApiService);
  private readonly telegramService = inject(TelegramService);
  private readonly toastService = inject(ErrorToastService);
  private readonly destroyRef = inject(DestroyRef);

  /** List of top-level locations */
  readonly locations = signal<LocationListItem[]>([]);

  /** Loading state */
  readonly isLoading = signal(true);

  /** Error message if load fails */
  readonly error = signal<string | null>(null);

  /** Callback for MainButton click */
  private readonly mainButtonCallback = () => this.navigateToCreateLocation();

  ngOnInit(): void {
    this.loadLocations();
    this.setupMainButton();
  }

  ngOnDestroy(): void {
    this.cleanupMainButton();
  }

  /**
   * Load top-level locations from API
   */
  loadLocations(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.locationApiService
      .getTopLevelLocations()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (locations) => {
          this.locations.set(locations);
          this.isLoading.set(false);
          this.updateMainButtonVisibility(locations.length > 0);
        },
        error: (err) => {
          const errorMessage = err.message || 'Failed to load locations';
          this.error.set(errorMessage);
          this.isLoading.set(false);
          this.telegramService.hideMainButton();
          this.toastService.error(errorMessage);
        }
      });
  }

  /**
   * Navigate to create location form
   */
  navigateToCreateLocation(): void {
    this.triggerHapticFeedback();
    this.router.navigate(['/location/new']);
  }

  /**
   * Navigate to add item form (global, no pre-selected location)
   */
  navigateToAddItem(): void {
    this.triggerHapticFeedback();
    this.router.navigate(['/add-item']);
  }

  private setupMainButton(): void {
    this.telegramService.setMainButtonText('Add Location');
    this.telegramService.onMainButtonClick(this.mainButtonCallback);
  }

  private cleanupMainButton(): void {
    this.telegramService.offMainButtonClick(this.mainButtonCallback);
    this.telegramService.hideMainButton();
  }

  private updateMainButtonVisibility(hasLocations: boolean): void {
    if (hasLocations) {
      this.telegramService.showMainButton();
    } else {
      // Hide MainButton when empty state shows its own button
      this.telegramService.hideMainButton();
    }
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
