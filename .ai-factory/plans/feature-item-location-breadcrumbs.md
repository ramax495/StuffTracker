# Feature: Clickable Location Hierarchy Breadcrumbs on Item Detail Page

**Branch:** `feature/item-location-breadcrumbs`
**Created:** 2026-03-01 (refined)
**Route:** `/item/{id}`

## Problem

The item detail page shows the location path as breadcrumbs (e.g. `Home > Apartment > Bedroom > Closet > ItemName`) but the intermediate location segments are **not clickable**. The `BreadcrumbsComponent` supports navigation via `locationIds`, but the item detail currently passes `[locationIds]="[]"` — a hardcoded empty array.

## Solution (Frontend-only, no backend changes)

`GET /locations/{id}` already returns `breadcrumbs: string[]` AND `breadcrumbIds: string[]` in `LocationResponse`. After loading the item, make a second call to `getLocation(item.locationId)` to retrieve `breadcrumbIds` and wire them to the breadcrumbs.

This avoids all backend changes. Trade-off: 2 HTTP requests per item page load (item + location), with the second being non-blocking (item renders immediately).

## Settings

- **Testing:** No
- **Logging:** Verbose (DEBUG/WARN logs)
- **Docs update:** No

---

## Tasks

### [x] Task 5 — Wire clickable breadcrumbs in item-detail using `getLocation()`

**File:** `frontend/src/app/features/item/item-detail/item-detail.component.ts`

**Changes:**

1. Import `LocationApiService`:
```typescript
import { LocationApiService } from '../../../core/api/location-api.service';
```

2. Inject the service:
```typescript
private readonly locationApiService = inject(LocationApiService);
```

3. Add `breadcrumbIds` signal:
```typescript
readonly breadcrumbIds = signal<string[]>([]);
```

4. In `loadItem()`, after item loads, fire a non-blocking second call:
```typescript
next: (item) => {
  this.item.set(item);
  this.isLoading.set(false);

  // Non-blocking: load breadcrumb IDs for clickable navigation
  this.locationApiService
    .getLocation(item.locationId)
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe({
      next: (loc) => {
        console.debug('[ItemDetail] Breadcrumb IDs loaded for', item.locationId, loc.breadcrumbIds);
        this.breadcrumbIds.set(loc.breadcrumbIds);
      },
      error: (err) => {
        console.warn('[ItemDetail] Breadcrumb IDs unavailable, falling back to non-clickable', err);
        // Graceful degradation: breadcrumbs still show as text
      }
    });
},
```

5. Update template:
```html
<app-breadcrumbs
  [breadcrumbs]="getBreadcrumbs()"
  [locationIds]="breadcrumbIds()"
/>
```

---

## Data Flow (after fix)

```
GET /api/items/{id}
  → locationPath: ["Apartment", "Bedroom", "Closet"]
  → locationId: "clos_id"

GET /api/locations/clos_id
  → breadcrumbs:   ["Apartment", "Bedroom", "Closet"]
  → breadcrumbIds: ["apt_id", "bed_id", "clos_id"]

getBreadcrumbs()  → ["Apartment", "Bedroom", "Closet", "ItemName"]
breadcrumbIds()   → ["apt_id", "bed_id", "clos_id"]

BreadcrumbsComponent renders:
  Home > [Apartment] > [Bedroom] > [Closet] > ItemName
          ↑ clickable   ↑ clickable  ↑ clickable  ↑ static (last)
```

## Commit Plan

Single commit after Task 5:
`feat(frontend): wire clickable location breadcrumbs on item detail page`
