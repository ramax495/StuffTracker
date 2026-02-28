# Feature: Item Form Location Autocomplete

**Branch:** `feature/item-form-location-autocomplete`
**Created:** 2026-02-28
**Status:** Planning

## Settings

| Setting | Value |
|---------|-------|
| Testing | Yes |
| Logging | Verbose (DEBUG) |
| Docs    | Yes |

## Summary

Add a location autocomplete field to the item form (create & edit modes). Currently the location is fixed from the route param and not changeable. After this feature:
- Create mode: location is pre-filled from route param but user can change it
- Edit mode: location is pre-filled from existing item but user can change it (triggers moveItem API)
- Global add-item route: user can add items from home screen without pre-selected location
- Each autocomplete dropdown item shows name + path hint ("Root > ... > Parent")

**Backend:** No changes needed — `POST /items/{id}/move` endpoint already exists.

## Tasks

### Phase 1: Core Component

**Task 1 — Create LocationAutocompleteComponent** [DONE]
- New file: `shared/components/location-autocomplete/location-autocomplete.component.ts`
- New file: `shared/components/location-autocomplete/index.ts`
- Inline text input with filtered dropdown
- Flattens `LocationTreeNode[]` into flat list with computed path
- Inputs: `locations`, `selectedLocationId`, `placeholder`, `required`
- Output: `locationSelected` emitting `{ id, name } | null`
- Filter by `name.includes(query)`, show path as hint per item

### Phase 2: Integration

**Task 2 — Integrate autocomplete into ItemFormComponent** [DONE]
- Edit: `features/item/item-form/item-form.component.ts`
- Replace static subtitle with `<app-location-autocomplete>` field
- Load location tree via `locationApiService.getLocationTree()` on init
- Create mode: pre-fill from `locationId` route param
- Edit mode: pre-fill from `item.locationId`, call `moveItem` on submit if changed
- Add location validation (required)

**Task 3 — Add global /add-item route and navigation** [DONE]
- Edit: `features/home/home.routes.ts` — add `/add-item` route
- Edit: `features/home/home.component.ts` — add "Add Item" quick action button
- Form works with empty `locationId` — user must select via autocomplete

### Phase 3: Quality

**Task 4 — Write tests** [DONE]
- New file: `shared/components/location-autocomplete/location-autocomplete.component.spec.ts`
- New file: `features/item/item-form/item-form.component.spec.ts`
- Autocomplete: flatten, filter, select, clear, pre-select, no-results
- Form: tree load, pre-select (create/edit), validation, moveItem on edit, global route

**Task 5 — Update documentation** [DONE]
- Update `docs/architecture.md` if shared components listed
- Verify `docs/api.md` accuracy (no API changes)

## Commit Plan

| Checkpoint | After Tasks | Message |
|------------|-------------|---------|
| Commit 1 | 1 | `feat(frontend): add LocationAutocompleteComponent` |
| Commit 2 | 2, 3 | `feat(frontend): integrate location autocomplete into item form` |
| Commit 3 | 4 | `test(frontend): add tests for location autocomplete and item form` |
| Commit 4 | 5 | `docs: update docs for location autocomplete feature` |
