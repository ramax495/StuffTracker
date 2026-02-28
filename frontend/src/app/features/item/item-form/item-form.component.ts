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
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  ItemApiService,
  ItemResponse,
  CreateItemRequest,
  UpdateItemRequest
} from '../../../core/api/item-api.service';
import { LocationApiService } from '../../../core/api/location-api.service';
import { TelegramService } from '../../../telegram/telegram.service';

/**
 * Form component for creating and editing items
 *
 * Features:
 * - Create mode: new item with locationId from route param
 * - Edit mode: update existing item by id
 * - Input fields: name (required), description (optional), quantity (number, min 1)
 * - Save using Telegram MainButton
 * - Validation: name required (max 200 chars), quantity >= 1
 * - Loading and error states
 */
@Component({
  selector: 'app-item-form',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="item-form">
      <!-- Loading state for edit mode -->
      @if (isLoadingItem()) {
        <div class="item-form__loading">
          <div class="item-form__spinner"></div>
          <p class="item-form__loading-text">Loading item...</p>
        </div>
      }

      <!-- Error state -->
      @if (loadError()) {
        <div class="item-form__error">
          <svg class="item-form__error-icon" viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z"/>
          </svg>
          <p class="item-form__error-text">{{ loadError() }}</p>
          <button
            type="button"
            class="btn btn-secondary"
            (click)="loadItem()"
          >
            Try Again
          </button>
        </div>
      }

      <!-- Form -->
      @if (!isLoadingItem() && !loadError()) {
        <header class="item-form__header">
          <button
            class="item-form__back-button"
            type="button"
            (click)="onBackClick()"
            aria-label="Return to previous location"
          >
            <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path d="M20 11H7.83l5.59-5.59L12 4l-8 8 8 8 1.41-1.41L7.83 13H20v-2z"/>
            </svg>
            Back
          </button>
          <div class="item-form__header-content">
            <h1 class="item-form__title">{{ formTitle() }}</h1>
            @if (locationName()) {
              <p class="item-form__subtitle">
                In: {{ locationName() }}
              </p>
            }
          </div>
        </header>

        <form class="item-form__form" (ngSubmit)="onSubmit()">
          <!-- Name field -->
          <div class="item-form__field">
            <label for="name" class="item-form__label">Name *</label>
            <input
              type="text"
              id="name"
              name="name"
              class="item-form__input"
              [class.item-form__input--error]="nameError()"
              [(ngModel)]="name"
              (ngModelChange)="validateName()"
              placeholder="Enter item name"
              maxlength="200"
              autocomplete="off"
              [attr.aria-describedby]="nameError() ? 'name-error' : null"
              [attr.aria-invalid]="!!nameError()"
            />
            <div class="item-form__field-footer">
              @if (nameError()) {
                <span id="name-error" class="item-form__error-message" role="alert">
                  {{ nameError() }}
                </span>
              }
              <span class="item-form__char-count" [class.item-form__char-count--warning]="name().length > 180">
                {{ name().length }}/200
              </span>
            </div>
          </div>

          <!-- Description field -->
          <div class="item-form__field">
            <label for="description" class="item-form__label">Description</label>
            <textarea
              id="description"
              name="description"
              class="item-form__textarea"
              [(ngModel)]="description"
              placeholder="Enter item description (optional)"
              maxlength="2000"
              rows="4"
            ></textarea>
            <div class="item-form__field-footer">
              <span></span>
              <span class="item-form__char-count" [class.item-form__char-count--warning]="description().length > 1800">
                {{ description().length }}/2000
              </span>
            </div>
          </div>

          <!-- Quantity field -->
          <div class="item-form__field">
            <label for="quantity" class="item-form__label">Quantity</label>
            <div class="item-form__quantity-wrapper">
              <button
                type="button"
                class="item-form__quantity-btn"
                (click)="decrementQuantity()"
                [disabled]="quantity() <= 1"
                aria-label="Decrease quantity"
              >
                <svg viewBox="0 0 24 24" fill="currentColor">
                  <path d="M19 13H5v-2h14v2z"/>
                </svg>
              </button>
              <input
                type="number"
                id="quantity"
                name="quantity"
                class="item-form__quantity-input"
                [class.item-form__input--error]="quantityError()"
                [(ngModel)]="quantity"
                (ngModelChange)="validateQuantity()"
                min="1"
                max="999999"
                [attr.aria-describedby]="quantityError() ? 'quantity-error' : null"
                [attr.aria-invalid]="!!quantityError()"
              />
              <button
                type="button"
                class="item-form__quantity-btn"
                (click)="incrementQuantity()"
                [disabled]="quantity() >= 999999"
                aria-label="Increase quantity"
              >
                <svg viewBox="0 0 24 24" fill="currentColor">
                  <path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/>
                </svg>
              </button>
            </div>
            @if (quantityError()) {
              <span id="quantity-error" class="item-form__error-message" role="alert">
                {{ quantityError() }}
              </span>
            }
          </div>

          <!-- Submit error -->
          @if (submitError()) {
            <div class="item-form__submit-error" role="alert">
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
              class="btn btn-primary item-form__submit"
              [disabled]="!isValid() || isSaving()"
            >
              @if (isSaving()) {
                <span class="item-form__submit-spinner"></span>
                Saving...
              } @else {
                {{ isEditMode() ? 'Update Item' : 'Create Item' }}
              }
            </button>
          }
        </form>
      }
    </div>
  `,
  styles: [`
    .item-form {
      display: flex;
      flex-direction: column;
      min-height: 100%;
      padding: var(--spacing-md);
      padding-bottom: calc(var(--spacing-xl) + 60px);
    }

    /* Loading state */
    .item-form__loading {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: var(--spacing-2xl) var(--spacing-md);
      gap: var(--spacing-md);
      flex: 1;
    }

    .item-form__spinner {
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

    .item-form__loading-text {
      font-size: 0.875rem;
      color: var(--tg-theme-hint-color);
    }

    /* Error state */
    .item-form__error {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: var(--spacing-2xl) var(--spacing-md);
      gap: var(--spacing-md);
      text-align: center;
      flex: 1;
    }

    .item-form__error-icon {
      width: 48px;
      height: 48px;
      color: var(--tg-theme-destructive-text-color);
    }

    .item-form__error-text {
      font-size: 0.875rem;
      color: var(--tg-theme-hint-color);
      max-width: 280px;
    }

    /* Header with back button */
    .item-form__header {
      display: flex;
      align-items: flex-start;
      gap: var(--spacing-md);
      padding: var(--spacing-lg) 0;
    }

    .item-form__back-button {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: var(--spacing-xs);
      padding: var(--spacing-sm) var(--spacing-md);
      min-height: 44px;
      min-width: 44px;
      background: none;
      border: none;
      color: var(--tg-theme-link-color);
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      border-radius: var(--radius-md);
      transition: background-color var(--transition-fast);
      flex-shrink: 0;

      svg {
        width: 20px;
        height: 20px;
      }

      &:active {
        background-color: var(--tg-theme-secondary-bg-color);
      }

      &:focus {
        outline: 2px solid var(--tg-theme-link-color);
        outline-offset: 2px;
      }

      &:focus:not(:focus-visible) {
        outline: none;
      }
    }

    .item-form__header-content {
      flex: 1;
      min-width: 0;
    }

    .item-form__title {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--tg-theme-text-color);
    }

    .item-form__subtitle {
      font-size: 0.875rem;
      color: var(--tg-theme-hint-color);
      margin-top: var(--spacing-xs);
    }

    /* Form */
    .item-form__form {
      display: flex;
      flex-direction: column;
      gap: var(--spacing-lg);
    }

    .item-form__field {
      display: flex;
      flex-direction: column;
      gap: var(--spacing-xs);
    }

    .item-form__label {
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--tg-theme-text-color);
    }

    .item-form__input,
    .item-form__textarea {
      width: 100%;
      padding: var(--spacing-md);
      font-size: 1rem;
      color: var(--tg-theme-text-color);
      background-color: var(--tg-theme-secondary-bg-color);
      border: 2px solid transparent;
      border-radius: var(--radius-md);
      transition: border-color var(--transition-fast), background-color var(--transition-fast);
      font-family: inherit;

      &::placeholder {
        color: var(--tg-theme-hint-color);
      }

      &:focus {
        outline: none;
        border-color: var(--tg-theme-button-color);
        background-color: var(--tg-theme-bg-color);
      }
    }

    .item-form__input {
      min-height: 52px;
    }

    .item-form__textarea {
      min-height: 100px;
      resize: vertical;
    }

    .item-form__input--error {
      border-color: var(--tg-theme-destructive-text-color);

      &:focus {
        border-color: var(--tg-theme-destructive-text-color);
      }
    }

    .item-form__field-footer {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: var(--spacing-sm);
      min-height: 20px;
    }

    .item-form__error-message {
      font-size: 0.75rem;
      color: var(--tg-theme-destructive-text-color);
      flex: 1;
    }

    .item-form__char-count {
      font-size: 0.75rem;
      color: var(--tg-theme-hint-color);
      flex-shrink: 0;
    }

    .item-form__char-count--warning {
      color: var(--tg-theme-destructive-text-color);
    }

    /* Quantity field */
    .item-form__quantity-wrapper {
      display: flex;
      align-items: center;
      gap: var(--spacing-sm);
    }

    .item-form__quantity-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 52px;
      height: 52px;
      background-color: var(--tg-theme-button-color);
      color: var(--tg-theme-button-text-color);
      border: none;
      border-radius: var(--radius-md);
      cursor: pointer;
      transition: opacity var(--transition-fast), transform var(--transition-fast);
      flex-shrink: 0;

      svg {
        width: 24px;
        height: 24px;
      }

      &:active {
        transform: scale(0.95);
      }

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }

    .item-form__quantity-input {
      flex: 1;
      min-width: 80px;
      text-align: center;
      font-size: 1.125rem;
      font-weight: 600;
      -moz-appearance: textfield;

      &::-webkit-outer-spin-button,
      &::-webkit-inner-spin-button {
        -webkit-appearance: none;
        margin: 0;
      }
    }

    /* Submit error */
    .item-form__submit-error {
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
    .item-form__submit {
      width: 100%;
      padding: var(--spacing-md);
      font-size: 1rem;
      min-height: 52px;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: var(--spacing-sm);
    }

    .item-form__submit-spinner {
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
export class ItemFormComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly itemApiService = inject(ItemApiService);
  private readonly locationApiService = inject(LocationApiService);
  private readonly telegramService = inject(TelegramService);
  private readonly destroyRef = inject(DestroyRef);

  /** Item ID for edit mode (from route param) */
  readonly id = input<string>();

  /** Location ID for create mode (from route param) */
  readonly locationId = input<string>();

  /** Form field: item name */
  readonly name = signal('');

  /** Form field: item description */
  readonly description = signal('');

  /** Form field: item quantity */
  readonly quantity = signal(1);

  /** Name validation error */
  readonly nameError = signal<string | null>(null);

  /** Quantity validation error */
  readonly quantityError = signal<string | null>(null);

  /** Loading state for fetching existing item */
  readonly isLoadingItem = signal(false);

  /** Error loading existing item */
  readonly loadError = signal<string | null>(null);

  /** Saving state */
  readonly isSaving = signal(false);

  /** Error during save */
  readonly submitError = signal<string | null>(null);

  /** Location name for display */
  readonly locationName = signal<string | null>(null);

  /** Existing item data for edit mode */
  private existingItem: ItemResponse | null = null;

  /** Location ID for create (stored after resolving) */
  private resolvedLocationId: string | null = null;

  /** Check if in edit mode */
  readonly isEditMode = computed(() => !!this.id());

  /** Form title based on mode */
  readonly formTitle = computed(() => {
    if (this.isEditMode()) {
      return 'Edit Item';
    }
    return 'Add Item';
  });

  /** Check if form is valid */
  readonly isValid = computed(() => {
    const n = this.name().trim();
    const q = this.quantity();
    return n.length > 0 && n.length <= 200 && !this.nameError() && q >= 1 && !this.quantityError();
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
      this.loadItem();
    } else if (this.locationId()) {
      this.resolvedLocationId = this.locationId()!;
      this.loadLocationInfo();
    }
  }

  ngOnDestroy(): void {
    this.cleanupMainButton();
  }

  /**
   * Load existing item for edit mode
   */
  loadItem(): void {
    const itemId = this.id();
    if (!itemId) return;

    this.isLoadingItem.set(true);
    this.loadError.set(null);

    this.itemApiService
      .getItem(itemId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (item) => {
          this.existingItem = item;
          this.name.set(item.name);
          this.description.set(item.description || '');
          this.quantity.set(item.quantity);
          this.locationName.set(item.locationName);
          this.resolvedLocationId = item.locationId;
          this.isLoadingItem.set(false);
          this.updateMainButton();
        },
        error: (err) => {
          this.loadError.set(err.message || 'Failed to load item');
          this.isLoadingItem.set(false);
        }
      });
  }

  /**
   * Load location info for create mode
   */
  private loadLocationInfo(): void {
    const locId = this.locationId();
    if (!locId) return;

    this.locationApiService
      .getLocation(locId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (location) => {
          this.locationName.set(location.name);
        },
        error: () => {
          // Silently ignore - location name is just for display
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
   * Validate quantity field
   */
  validateQuantity(): void {
    const q = this.quantity();

    if (q < 1) {
      this.quantityError.set('Quantity must be at least 1');
    } else if (q > 999999) {
      this.quantityError.set('Quantity must be 999999 or less');
    } else if (!Number.isInteger(q)) {
      this.quantityError.set('Quantity must be a whole number');
    } else {
      this.quantityError.set(null);
    }

    this.updateMainButton();
  }

  /**
   * Increment quantity
   */
  incrementQuantity(): void {
    this.triggerHapticFeedback();
    if (this.quantity() < 999999) {
      this.quantity.update(q => q + 1);
      this.validateQuantity();
    }
  }

  /**
   * Decrement quantity
   */
  decrementQuantity(): void {
    this.triggerHapticFeedback();
    if (this.quantity() > 1) {
      this.quantity.update(q => q - 1);
      this.validateQuantity();
    }
  }

  /**
   * Handle back button click - navigate to location without saving
   */
  onBackClick(): void {
    this.triggerHapticFeedback();
    const locId = this.locationId() || this.resolvedLocationId;

    if (locId) {
      this.router.navigate(['/location', locId]);
    } else {
      this.router.navigate(['/']);
    }
  }

  /**
   * Handle form submission
   */
  onSubmit(): void {
    // Trigger validation
    this.validateName();
    this.validateQuantity();

    if (!this.isValid() || this.isSaving()) {
      return;
    }

    this.triggerHapticFeedback();
    this.isSaving.set(true);
    this.submitError.set(null);
    this.updateMainButton();

    if (this.isEditMode()) {
      this.updateItem();
    } else {
      this.createItem();
    }
  }

  private createItem(): void {
    if (!this.resolvedLocationId) {
      this.submitError.set('Location is required');
      this.isSaving.set(false);
      this.updateMainButton();
      return;
    }

    const request: CreateItemRequest = {
      name: this.name().trim(),
      description: this.description().trim() || undefined,
      quantity: this.quantity(),
      locationId: this.resolvedLocationId
    };

    this.itemApiService
      .createItem(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (item) => {
          this.triggerHapticFeedback('success');
          this.isSaving.set(false);
          this.router.navigate(['/item', item.id]);
        },
        error: (err) => {
          this.triggerHapticFeedback('error');
          this.submitError.set(err.message || 'Failed to create item');
          this.isSaving.set(false);
          this.updateMainButton();
        }
      });
  }

  private updateItem(): void {
    const itemId = this.id();
    if (!itemId) return;

    const request: UpdateItemRequest = {
      name: this.name().trim(),
      description: this.description().trim() || undefined,
      quantity: this.quantity()
    };

    this.itemApiService
      .updateItem(itemId, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.triggerHapticFeedback('success');
          this.isSaving.set(false);
          this.router.navigate(['/item', itemId]);
        },
        error: (err) => {
          this.triggerHapticFeedback('error');
          this.submitError.set(err.message || 'Failed to update item');
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
    const text = this.isEditMode() ? 'Update Item' : 'Create Item';
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
