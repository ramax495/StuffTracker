import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  OnInit,
  OnDestroy,
  DestroyRef,
  input,
  computed
} from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  LocationApiService,
  LocationResponse,
  CreateLocationRequest,
  UpdateLocationRequest
} from '../../../core/api/location-api.service';
import { TelegramService } from '../../../telegram/telegram.service';

/**
 * Form component for creating and editing locations
 *
 * Features:
 * - Create mode: new location with optional parent
 * - Edit mode: update existing location name
 * - Name validation (required, max 200 chars)
 * - Save using Telegram MainButton
 * - Loading and error states
 */
@Component({
  selector: 'app-location-form',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="location-form">
      <!-- Loading state for edit mode -->
      @if (isLoadingLocation()) {
        <div class="location-form__loading">
          <div class="location-form__spinner"></div>
          <p class="location-form__loading-text">Loading location...</p>
        </div>
      }

      <!-- Error state -->
      @if (loadError()) {
        <div class="location-form__error">
          <svg class="location-form__error-icon" viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z"/>
          </svg>
          <p class="location-form__error-text">{{ loadError() }}</p>
          <button
            type="button"
            class="btn btn-secondary"
            (click)="loadLocation()"
          >
            Try Again
          </button>
        </div>
      }

      <!-- Form -->
      @if (!isLoadingLocation() && !loadError()) {
        <header class="location-form__header">
          <button
            class="location-form__back-button"
            type="button"
            (click)="onBackClick()"
            aria-label="Return to previous location"
          >
            <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path d="M20 11H7.83l5.59-5.59L12 4l-8 8 8 8 1.41-1.41L7.83 13H20v-2z"/>
            </svg>
            Back
          </button>
          <div class="location-form__header-content">
            <h1 class="location-form__title">{{ formTitle() }}</h1>
            @if (parentName()) {
              <p class="location-form__subtitle">
                In: {{ parentName() }}
              </p>
            }
          </div>
        </header>

        <form class="location-form__form" (ngSubmit)="onSubmit()">
          <!-- Name field -->
          <div class="location-form__field">
            <label for="name" class="location-form__label">Name</label>
            <input
              type="text"
              id="name"
              name="name"
              class="location-form__input"
              [class.location-form__input--error]="nameError()"
              [(ngModel)]="name"
              (ngModelChange)="validateName()"
              placeholder="Enter location name"
              maxlength="200"
              autocomplete="off"
              [attr.aria-describedby]="nameError() ? 'name-error' : null"
              [attr.aria-invalid]="!!nameError()"
            />
            <div class="location-form__field-footer">
              @if (nameError()) {
                <span id="name-error" class="location-form__error-message" role="alert">
                  {{ nameError() }}
                </span>
              }
              <span class="location-form__char-count" [class.location-form__char-count--warning]="name().length > 180">
                {{ name().length }}/200
              </span>
            </div>
          </div>

          <!-- Submit error -->
          @if (submitError()) {
            <div class="location-form__submit-error" role="alert">
              <svg viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z"/>
              </svg>
              <span>{{ submitError() }}</span>
            </div>
          }

          <!-- Submit button (visible when not in Telegram) -->
          @if (!isInTelegram()) {
            <button
              type="submit"
              class="btn btn-primary location-form__submit"
              [disabled]="!isValid() || isSaving()"
            >
              @if (isSaving()) {
                <span class="location-form__submit-spinner"></span>
                Saving...
              } @else {
                {{ isEditMode() ? 'Update Location' : 'Create Location' }}
              }
            </button>
          }
        </form>
      }
    </div>
  `,
  styles: [`
    .location-form {
      display: flex;
      flex-direction: column;
      min-height: 100%;
      padding: var(--spacing-md);
      padding-bottom: calc(var(--spacing-xl) + 60px);
    }

    /* Loading state */
    .location-form__loading {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: var(--spacing-2xl) var(--spacing-md);
      gap: var(--spacing-md);
      flex: 1;
    }

    .location-form__spinner {
      width: 40px;
      height: 40px;
      border: 3px solid var(--tg-theme-secondary-bg-color);
      border-top-color: var(--tg-theme-button-color);
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .location-form__loading-text {
      font-size: 0.875rem;
      color: var(--tg-theme-hint-color);
    }

    /* Error state */
    .location-form__error {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: var(--spacing-2xl) var(--spacing-md);
      gap: var(--spacing-md);
      text-align: center;
      flex: 1;
    }

    .location-form__error-icon {
      width: 48px;
      height: 48px;
      color: var(--tg-theme-destructive-text-color);
    }

    .location-form__error-text {
      font-size: 0.875rem;
      color: var(--tg-theme-hint-color);
      max-width: 280px;
    }

    /* Header */
    .location-form__header {
      display: flex;
      align-items: flex-start;
      gap: var(--spacing-md);
      padding: var(--spacing-lg) 0;
    }

    .location-form__back-button {
      display: flex;
      align-items: center;
      gap: var(--spacing-xs);
      padding: var(--spacing-xs) var(--spacing-sm);
      background: none;
      border: none;
      color: var(--tg-theme-text-color);
      font-size: 1rem;
      cursor: pointer;
      border-radius: 4px;
      transition: background-color 0.2s ease-out;
      min-height: 44px;
      min-width: 44px;
      justify-content: center;
      flex-shrink: 0;

      svg {
        width: 20px;
        height: 20px;
        flex-shrink: 0;
      }

      &:hover {
        background-color: var(--tg-theme-accent-text-color, rgba(0, 0, 0, 0.1));
      }

      &:active {
        opacity: 0.7;
      }

      &:focus {
        outline: 2px solid var(--tg-theme-link-color);
        outline-offset: 2px;
      }
    }

    .location-form__header-content {
      flex: 1;
    }

    .location-form__title {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--tg-theme-text-color);
    }

    .location-form__subtitle {
      font-size: 0.875rem;
      color: var(--tg-theme-hint-color);
      margin-top: var(--spacing-xs);
    }

    /* Form */
    .location-form__form {
      display: flex;
      flex-direction: column;
      gap: var(--spacing-lg);
    }

    .location-form__field {
      display: flex;
      flex-direction: column;
      gap: var(--spacing-xs);
    }

    .location-form__label {
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--tg-theme-text-color);
    }

    .location-form__input {
      width: 100%;
      padding: var(--spacing-md);
      font-size: 1rem;
      color: var(--tg-theme-text-color);
      background-color: var(--tg-theme-secondary-bg-color);
      border: 2px solid transparent;
      border-radius: var(--radius-md);
      transition: border-color var(--transition-fast), background-color var(--transition-fast);
      min-height: 52px;

      &::placeholder {
        color: var(--tg-theme-hint-color);
      }

      &:focus {
        outline: none;
        border-color: var(--tg-theme-button-color);
        background-color: var(--tg-theme-bg-color);
      }
    }

    .location-form__input--error {
      border-color: var(--tg-theme-destructive-text-color);

      &:focus {
        border-color: var(--tg-theme-destructive-text-color);
      }
    }

    .location-form__field-footer {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: var(--spacing-sm);
      min-height: 20px;
    }

    .location-form__error-message {
      font-size: 0.75rem;
      color: var(--tg-theme-destructive-text-color);
      flex: 1;
    }

    .location-form__char-count {
      font-size: 0.75rem;
      color: var(--tg-theme-hint-color);
      flex-shrink: 0;
    }

    .location-form__char-count--warning {
      color: var(--tg-theme-destructive-text-color);
    }

    /* Submit error */
    .location-form__submit-error {
      display: flex;
      align-items: center;
      gap: var(--spacing-sm);
      padding: var(--spacing-md);
      background-color: color-mix(in srgb, var(--tg-theme-destructive-text-color) 10%, transparent);
      border-radius: var(--radius-md);
      font-size: 0.875rem;
      color: var(--tg-theme-destructive-text-color);

      svg {
        width: 20px;
        height: 20px;
        flex-shrink: 0;
      }
    }

    /* Submit button (non-Telegram) */
    .location-form__submit {
      width: 100%;
      padding: var(--spacing-md);
      font-size: 1rem;
      min-height: 52px;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: var(--spacing-sm);
    }

    .location-form__submit-spinner {
      width: 20px;
      height: 20px;
      border: 2px solid transparent;
      border-top-color: currentColor;
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LocationFormComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly locationApiService = inject(LocationApiService);
  private readonly telegramService = inject(TelegramService);
  private readonly destroyRef = inject(DestroyRef);

  /** Location ID for edit mode (from route param) */
  readonly id = input<string>();

  /** Parent ID for create with parent mode (from route param) */
  readonly parentId = input<string>();

  /** Form field: location name */
  readonly name = signal('');

  /** Name validation error */
  readonly nameError = signal<string | null>(null);

  /** Loading state for fetching existing location */
  readonly isLoadingLocation = signal(false);

  /** Error loading existing location */
  readonly loadError = signal<string | null>(null);

  /** Saving state */
  readonly isSaving = signal(false);

  /** Error during save */
  readonly submitError = signal<string | null>(null);

  /** Parent location name for display */
  readonly parentName = signal<string | null>(null);

  /** Existing location data for edit mode */
  private existingLocation: LocationResponse | null = null;

  /** Check if in edit mode */
  readonly isEditMode = computed(() => !!this.id());

  /** Form title based on mode */
  readonly formTitle = computed(() => {
    if (this.isEditMode()) {
      return 'Edit Location';
    }
    return this.parentId() ? 'Add Sub-location' : 'New Location';
  });

  /** Check if form is valid */
  readonly isValid = computed(() => {
    const n = this.name().trim();
    return n.length > 0 && n.length <= 200 && !this.nameError();
  });

  /** Check if running in Telegram */
  isInTelegram(): boolean {
    return this.telegramService.isInTelegram();
  }

  /** Callback for MainButton click */
  private readonly mainButtonCallback = () => this.onSubmit();

  ngOnInit(): void {
    this.setupMainButton();

    if (this.isEditMode()) {
      this.loadLocation();
    } else if (this.parentId()) {
      this.loadParentInfo();
    }
  }

  ngOnDestroy(): void {
    this.cleanupMainButton();
  }

  /**
   * Navigate back to previous location
   */
  onBackClick(): void {
    this.triggerHapticFeedback();

    // For Add Sub-location mode: navigate to parent
    if (this.parentId()) {
      this.router.navigate(['/location', this.parentId()]);
    }
    // For Edit mode: navigate to the location being edited
    else if (this.id()) {
      this.router.navigate(['/location', this.id()]);
    }
    // For new top-level location: navigate to home
    else {
      this.router.navigate(['/']);
    }
  }

  /**
   * Load existing location for edit mode
   */
  loadLocation(): void {
    const locationId = this.id();
    if (!locationId) return;

    this.isLoadingLocation.set(true);
    this.loadError.set(null);

    this.locationApiService
      .getLocation(locationId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (location) => {
          this.existingLocation = location;
          this.name.set(location.name);
          this.isLoadingLocation.set(false);
          this.updateMainButton();
        },
        error: (err) => {
          this.loadError.set(err.message || 'Failed to load location');
          this.isLoadingLocation.set(false);
        }
      });
  }

  /**
   * Load parent location info for create with parent mode
   */
  private loadParentInfo(): void {
    const pId = this.parentId();
    if (!pId) return;

    this.locationApiService
      .getLocation(pId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (location) => {
          this.parentName.set(location.name);
        },
        error: () => {
          // Silently ignore - parent name is just for display
        }
      });
  }

  /**
   * Validate name field
   */
  validateName(): void {
    const n = this.name().trim();

    if (n.length === 0) {
      this.nameError.set('Name is required');
    } else if (n.length > 200) {
      this.nameError.set('Name must be 200 characters or less');
    } else {
      this.nameError.set(null);
    }

    this.updateMainButton();
  }

  /**
   * Handle form submission
   */
  onSubmit(): void {
    // Trigger validation
    this.validateName();

    if (!this.isValid() || this.isSaving()) {
      return;
    }

    this.triggerHapticFeedback();
    this.isSaving.set(true);
    this.submitError.set(null);
    this.updateMainButton();

    if (this.isEditMode()) {
      this.updateLocation();
    } else {
      this.createLocation();
    }
  }

  private createLocation(): void {
    const request: CreateLocationRequest = {
      name: this.name().trim(),
      parentId: this.parentId() || undefined
    };

    this.locationApiService
      .createLocation(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (location) => {
          this.triggerHapticFeedback('success');
          this.isSaving.set(false);
          this.router.navigate(['/location', location.id]);
        },
        error: (err) => {
          this.triggerHapticFeedback('error');
          this.submitError.set(err.message || 'Failed to create location');
          this.isSaving.set(false);
          this.updateMainButton();
        }
      });
  }

  private updateLocation(): void {
    const locationId = this.id();
    if (!locationId) return;

    const request: UpdateLocationRequest = {
      name: this.name().trim()
    };

    this.locationApiService
      .updateLocation(locationId, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.triggerHapticFeedback('success');
          this.isSaving.set(false);
          this.router.navigate(['/location', locationId]);
        },
        error: (err) => {
          this.triggerHapticFeedback('error');
          this.submitError.set(err.message || 'Failed to update location');
          this.isSaving.set(false);
          this.updateMainButton();
        }
      });
  }

  private setupMainButton(): void {
    this.updateMainButton();
    this.telegramService.onMainButtonClick(this.mainButtonCallback);
  }

  private cleanupMainButton(): void {
    this.telegramService.offMainButtonClick(this.mainButtonCallback);
    this.telegramService.hideMainButton();
  }

  private updateMainButton(): void {
    const text = this.isEditMode() ? 'Update Location' : 'Create Location';
    this.telegramService.setMainButtonText(text);

    if (this.isValid() && !this.isSaving()) {
      this.telegramService.showMainButton();
    } else {
      this.telegramService.hideMainButton();
    }
  }

  private triggerHapticFeedback(type: 'light' | 'success' | 'error' = 'light'): void {
    if (this.telegramService.isInTelegram()) {
      try {
        if (type === 'success' || type === 'error') {
          // @ts-expect-error - HapticFeedback may not be typed in SDK
          window.Telegram?.WebApp?.HapticFeedback?.notificationOccurred(type);
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
