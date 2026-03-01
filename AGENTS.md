# StuffTracker Project Map

This document maps the project structure for AI agents and developers navigating the codebase.

## Frontend Components

### Shared Components (`frontend/src/app/shared/components/`)

Reusable UI components used across multiple features.

| Component | Path | Purpose | Inputs | Outputs |
|-----------|------|---------|--------|---------|
| **Breadcrumbs** | `breadcrumbs/` | Location path navigation | `breadcrumbs[]`, `locationIds[]` | `navigateToHome()`, `navigateToLocation(id)` |
| **LocationHierarchy** | `item/components/location-hierarchy/` | Item location hierarchy display | `breadcrumbs[]`, `breadcrumbIds[]`, `currentLocationId` | `locationSelected(id)` |
| **LocationPicker** | `location-picker/` | Tree picker for location selection | `locations: LocationTreeNode[]` | `locationSelected(id)`, `closed()` |
| **LocationCard** | `location-card/` | Compact location display | `location: LocationListItem` | Click navigation |
| **ItemCard** | `item-card/` | Compact item display | `item: ItemListItem` | Click navigation |
| **ItemList** | `item-list/` | Item list container | `items: ItemListItem[]` | Item selection |
| **ErrorToast** | `error-toast/` | Toast notifications | Service-driven | Auto-dismiss |
| **DeleteConfirmation** | `delete-confirmation/` | Confirmation dialog | `title`, `message` | `confirmed()`, `cancelled()` |
| **SearchResultItem** | `search-result-item/` | Search result display | `result: SearchResult` | Click navigation |
| **LocationAutocomplete** | `location-autocomplete/` | Text input with location dropdown | `placeholder` | `locationSelected(id)` |

### Feature Components

#### Home Feature (`features/home/`)
- **HomeComponent** — Root location list view

#### Item Feature (`features/item/`)
- **ItemDetailComponent** — Item detail page with location hierarchy
  - Uses: **LocationHierarchyComponent** (for location path display)
  - Uses: **BreadcrumbsComponent** (for page navigation)
- **ItemFormComponent** — Add/Edit item form
- **MoveItemModalComponent** — Modal to move item to new location
  - Uses: **LocationPickerComponent**

#### Location Feature (`features/location/`)
- **LocationDetailComponent** — Location detail page with child items and locations
  - Uses: **BreadcrumbsComponent**
  - Uses: **LocationCardComponent** (for child locations)
  - Uses: **ItemListComponent** (for items in location)
- **LocationFormComponent** — Add/Edit location form
- **MoveLocationModalComponent** — Modal to move location to new parent
  - Uses: **LocationPickerComponent**

#### Search Feature (`features/search/`)
- **SearchComponent** — Search results view
  - Uses: **SearchResultItemComponent**

## Key Services

### Core API Services (`core/api/`)
- **ItemApiService** — GET/POST/PUT/DELETE items
- **LocationApiService** — GET/POST/PUT/DELETE locations
- **SearchApiService** — Search items

### Core Auth (`core/auth/`)
- **AuthService** — User authentication state
- **AuthGuard** — Route protection

### Telegram Integration (`telegram/`)
- **TelegramService** — Wrapper for @twa-dev/sdk
  - Methods: `getInitData()`, `isInTelegram()`, `showBackButton()`, `HapticFeedback()`

## Key Data Models

### LocationResponse (from API)
```typescript
{
  id: string;
  name: string;
  breadcrumbs: string[];      // Location names from root
  breadcrumbIds: string[];    // Location IDs from root (parallel array)
  children: LocationListItem[];
  items: ItemListItem[];
}
```

### ItemDetail (from API)
```typescript
{
  id: string;
  name: string;
  quantity: number;
  description: string;
  locationId: string;
  locationName: string;
  locationPath: string[];     // Location names (without item name)
  createdAt: string;
  updatedAt: string;
}
```

## Responsive Design Patterns

### Mobile-First Approach
- Base styles for mobile (< 480px)
- `@media (min-width: 480px)` for tablet
- `@media (min-width: 768px)` for desktop

### Touch-Friendly
- Minimum touch target: 44px × 44px
- Buttons use `min-height: 44px`
- Adequate spacing between interactive elements

### Telegram Theme Integration
- CSS custom properties: `--tg-text-color`, `--tg-link-color`, `--tg-bg-color`
- Support for light and dark themes
- Haptic feedback on user actions (when in Telegram)

## See Also

- [Architecture](docs/architecture.md) — Vertical slices, dependency rules, design patterns
- [API Reference](docs/api.md) — REST endpoints
- [Getting Started](docs/getting-started.md) — Local setup
