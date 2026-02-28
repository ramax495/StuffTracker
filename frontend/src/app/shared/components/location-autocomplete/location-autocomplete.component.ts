import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  computed,
  inject,
  ElementRef,
  DestroyRef,
  OnInit
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { fromEvent } from 'rxjs';
import { filter } from 'rxjs/operators';
import { LocationTreeNode } from '../../../core/api/location-api.service';
import { TelegramService } from '../../../telegram/telegram.service';

/**
 * Flattened location with pre-computed path for display
 */
interface FlatLocation {
  id: string;
  name: string;
  path: string;
}

/**
 * Selection event emitted when user picks a location
 */
export interface LocationSelection {
  id: string;
  name: string;
}

/**
 * LocationAutocompleteComponent provides a text input with filtered dropdown
 * for selecting a location from the hierarchical tree.
 *
 * Features:
 * - Flattens LocationTreeNode[] into a flat searchable list
 * - Each item shows name + path hint ("Root > ... > Parent")
 * - Filters by name.includes(query) (case-insensitive)
 * - Pre-selects location by ID
 * - Keyboard accessible, touch-friendly (48px min targets)
 * - Telegram theme styling
 */
@Component({
  selector: 'app-location-autocomplete',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="loc-autocomplete">
      <div class="loc-autocomplete__input-wrapper">
        <input
          type="text"
          class="loc-autocomplete__input"
          [class.loc-autocomplete__input--error]="required() && !selectedLocation() && touched()"
          [placeholder]="placeholder()"
          [value]="displayValue()"
          (input)="onInput($event)"
          (focus)="onFocus()"
          (keydown)="onKeydown($event)"
          autocomplete="off"
          role="combobox"
          aria-autocomplete="list"
          [attr.aria-expanded]="isOpen()"
          aria-haspopup="listbox"
          aria-controls="location-listbox"
          [attr.aria-activedescendant]="activeDescendant()"
          [attr.aria-invalid]="required() && !selectedLocation() && touched()"
        />
        @if (selectedLocation()) {
          <button
            type="button"
            class="loc-autocomplete__clear"
            (click)="clearSelection()"
            aria-label="Clear location selection"
          >
            <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
            </svg>
          </button>
        }
      </div>

      @if (isOpen()) {
        <ul
          id="location-listbox"
          class="loc-autocomplete__dropdown"
          role="listbox"
          aria-label="Location suggestions"
        >
          @for (item of filteredLocations(); track item.id; let i = $index) {
            <li
              [id]="'loc-option-' + item.id"
              class="loc-autocomplete__option"
              [class.loc-autocomplete__option--highlighted]="highlightedIndex() === i"
              role="option"
              [attr.aria-selected]="selectedLocation()?.id === item.id"
              (click)="selectItem(item)"
              (mouseenter)="highlightedIndex.set(i)"
            >
              <div class="loc-autocomplete__option-icon">
                <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                  <path d="M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z"/>
                </svg>
              </div>
              <div class="loc-autocomplete__option-content">
                <span class="loc-autocomplete__option-name">{{ item.name }}</span>
                @if (item.path) {
                  <span class="loc-autocomplete__option-path">{{ item.path }}</span>
                }
              </div>
            </li>
          } @empty {
            <li class="loc-autocomplete__no-results" role="option" aria-disabled="true">
              No locations found
            </li>
          }
        </ul>
      }

      @if (required() && !selectedLocation() && touched()) {
        <span class="loc-autocomplete__error" role="alert">
          Location is required
        </span>
      }
    </div>
  `,
  styles: [`
    .loc-autocomplete {
      position: relative;
    }

    .loc-autocomplete__input-wrapper {
      position: relative;
      display: flex;
      align-items: center;
    }

    .loc-autocomplete__input {
      width: 100%;
      min-height: 52px;
      padding: var(--spacing-md);
      padding-right: 44px;
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

    .loc-autocomplete__input--error {
      border-color: var(--tg-theme-destructive-text-color);

      &:focus {
        border-color: var(--tg-theme-destructive-text-color);
      }
    }

    .loc-autocomplete__clear {
      position: absolute;
      right: 4px;
      display: flex;
      align-items: center;
      justify-content: center;
      width: 40px;
      height: 40px;
      background: none;
      border: none;
      border-radius: var(--radius-full);
      cursor: pointer;
      color: var(--tg-theme-hint-color);
      transition: background-color var(--transition-fast);

      &:active {
        background-color: var(--tg-theme-secondary-bg-color);
      }

      svg {
        width: 20px;
        height: 20px;
      }
    }

    .loc-autocomplete__dropdown {
      position: absolute;
      top: calc(100% + 4px);
      left: 0;
      right: 0;
      max-height: 240px;
      overflow-y: auto;
      background-color: var(--tg-theme-bg-color);
      border: 1px solid var(--tg-theme-secondary-bg-color);
      border-radius: var(--radius-md);
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      z-index: var(--z-dropdown, 10);
      list-style: none;
      margin: 0;
      padding: var(--spacing-xs) 0;
    }

    .loc-autocomplete__option {
      display: flex;
      align-items: center;
      gap: var(--spacing-sm);
      min-height: 48px;
      padding: var(--spacing-sm) var(--spacing-md);
      cursor: pointer;
      transition: background-color var(--transition-fast);

      &:active {
        background-color: var(--tg-theme-secondary-bg-color);
      }
    }

    .loc-autocomplete__option--highlighted {
      background-color: var(--tg-theme-secondary-bg-color);
    }

    .loc-autocomplete__option-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 32px;
      height: 32px;
      background-color: var(--tg-theme-button-color);
      border-radius: var(--radius-sm);
      flex-shrink: 0;

      svg {
        width: 18px;
        height: 18px;
        color: var(--tg-theme-button-text-color);
      }
    }

    .loc-autocomplete__option-content {
      flex: 1;
      min-width: 0;
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .loc-autocomplete__option-name {
      font-size: 0.9375rem;
      color: var(--tg-theme-text-color);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .loc-autocomplete__option-path {
      font-size: 0.75rem;
      color: var(--tg-theme-hint-color);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .loc-autocomplete__no-results {
      padding: var(--spacing-md);
      text-align: center;
      font-size: 0.875rem;
      color: var(--tg-theme-hint-color);
    }

    .loc-autocomplete__error {
      display: block;
      margin-top: var(--spacing-xs);
      font-size: 0.75rem;
      color: var(--tg-theme-destructive-text-color);
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LocationAutocompleteComponent implements OnInit {
  private readonly elementRef = inject(ElementRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly telegramService = inject(TelegramService);

  /** Tree of locations to flatten and search */
  readonly locations = input<LocationTreeNode[]>([]);

  /** Pre-selected location ID */
  readonly selectedLocationId = input<string | undefined>(undefined);

  /** Placeholder text for the input */
  readonly placeholder = input<string>('Search locations...');

  /** Whether a selection is required */
  readonly required = input<boolean>(false);

  /** Emitted when user selects or clears a location */
  readonly locationSelected = output<LocationSelection | null>();

  /** Current search query typed by user */
  readonly query = signal('');

  /** Whether the dropdown is open */
  readonly isOpen = signal(false);

  /** Currently highlighted item index for keyboard navigation */
  readonly highlightedIndex = signal(-1);

  /** Whether the input has been touched (for validation) */
  readonly touched = signal(false);

  /** Currently selected location */
  readonly selectedLocation = signal<FlatLocation | null>(null);

  /** Flattened list of all locations with computed paths */
  readonly flatLocations = computed(() => {
    const result: FlatLocation[] = [];

    const flatten = (nodes: LocationTreeNode[], ancestors: string[]): void => {
      for (const node of nodes) {
        result.push({
          id: node.id,
          name: node.name,
          path: ancestors.length > 0 ? ancestors.join(' > ') : ''
        });

        if (node.children?.length) {
          flatten(node.children, [...ancestors, node.name]);
        }
      }
    };

    flatten(this.locations(), []);
    return result;
  });

  /** Filtered locations based on query */
  readonly filteredLocations = computed(() => {
    const q = this.query().toLowerCase().trim();
    if (!q) {
      return this.flatLocations();
    }
    return this.flatLocations().filter(loc =>
      loc.name.toLowerCase().includes(q)
    );
  });

  /** Display value for the input field */
  readonly displayValue = computed(() => {
    if (this.isOpen()) {
      return this.query();
    }
    return this.selectedLocation()?.name ?? '';
  });

  /** Active descendant ID for aria */
  readonly activeDescendant = computed(() => {
    const idx = this.highlightedIndex();
    const filtered = this.filteredLocations();
    if (idx >= 0 && idx < filtered.length) {
      return 'loc-option-' + filtered[idx].id;
    }
    return null;
  });

  ngOnInit(): void {
    this.initPreselection();
    this.listenForOutsideClicks();
  }

  /**
   * Pre-select location if selectedLocationId is provided
   */
  private initPreselection(): void {
    const preselectedId = this.selectedLocationId();
    if (!preselectedId) return;

    const match = this.flatLocations().find(loc => loc.id === preselectedId);
    if (match) {
      this.selectedLocation.set(match);
    }
  }

  /**
   * Close dropdown when clicking outside the component
   */
  private listenForOutsideClicks(): void {
    fromEvent<MouseEvent>(document, 'click')
      .pipe(
        filter(event => !this.elementRef.nativeElement.contains(event.target as Node)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        if (this.isOpen()) {
          this.closeDropdown();
        }
      });
  }

  onInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.query.set(value);
    this.highlightedIndex.set(-1);

    if (!this.isOpen()) {
      this.isOpen.set(true);
    }

    // Clear selection when user types (they're searching for a new one)
    if (this.selectedLocation()) {
      this.selectedLocation.set(null);
      this.locationSelected.emit(null);
    }
  }

  onFocus(): void {
    this.touched.set(true);
    this.isOpen.set(true);

    // If there's a selected location, populate query to allow refinement
    const selected = this.selectedLocation();
    if (selected) {
      this.query.set(selected.name);
    }
  }

  onKeydown(event: KeyboardEvent): void {
    const filtered = this.filteredLocations();

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        if (!this.isOpen()) {
          this.isOpen.set(true);
        }
        this.highlightedIndex.update(i =>
          i < filtered.length - 1 ? i + 1 : 0
        );
        break;

      case 'ArrowUp':
        event.preventDefault();
        if (!this.isOpen()) {
          this.isOpen.set(true);
        }
        this.highlightedIndex.update(i =>
          i > 0 ? i - 1 : filtered.length - 1
        );
        break;

      case 'Enter':
        event.preventDefault();
        if (this.isOpen() && this.highlightedIndex() >= 0 && this.highlightedIndex() < filtered.length) {
          this.selectItem(filtered[this.highlightedIndex()]);
        }
        break;

      case 'Escape':
        event.preventDefault();
        this.closeDropdown();
        break;
    }
  }

  selectItem(item: FlatLocation): void {
    this.triggerHapticFeedback();
    this.selectedLocation.set(item);
    this.query.set('');
    this.closeDropdown();
    this.locationSelected.emit({ id: item.id, name: item.name });
  }

  clearSelection(): void {
    this.triggerHapticFeedback();
    this.selectedLocation.set(null);
    this.query.set('');
    this.locationSelected.emit(null);
  }

  private closeDropdown(): void {
    this.isOpen.set(false);
    this.highlightedIndex.set(-1);
    this.query.set('');
  }

  private triggerHapticFeedback(): void {
    if (this.telegramService.isInTelegram()) {
      try {
        // @ts-expect-error - HapticFeedback may not be typed in SDK
        window.Telegram?.WebApp?.HapticFeedback?.selectionChanged();
      } catch {
        // Silently ignore if haptic feedback is not available
      }
    }
  }
}
