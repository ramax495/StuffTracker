# Migrate Angular Frontend to Zoneless Mode

**Branch:** `chore/angular-zoneless`
**Created:** 2026-02-28

## Settings

- **Tests:** No
- **Logging:** Verbose (DEBUG logs for key operations)
- **Docs:** Yes (update after implementation)

---

## Context

Angular 18+ introduced experimental zoneless change detection; Angular 19+ stabilised it as
`provideZonelessChangeDetection()`. The app already uses signals throughout and OnPush on all
feature components — it is essentially zoneless-ready. This migration removes the last zone.js
dependency and fixes two patterns that are technically incorrect in zoneless mode.

### What changes

| # | Change | File |
|---|--------|------|
| 1 | Replace `provideZoneChangeDetection` → `provideZonelessChangeDetection` + add `withFetch()` | `app.config.ts` |
| 2 | Remove `zone.js` / `zone.js/testing` from polyfills | `angular.json` |
| 3 | Add `OnPush` to root `App` component (only component missing it) | `app.ts` |
| 4 | Fix `effect()+subscribe` → `toObservable+switchMap` in `LocationDetailComponent` | `location-detail.component.ts` |
| 5 | Unify subscription teardown: replace `takeUntil(destroy$)` → `takeUntilDestroyed` in `SearchComponent` | `search.component.ts` |
| 6 | Verify build + manual smoke test | — |
| 7 | Update docs (architecture.md, ARCHITECTURE.md, MEMORY.md) | docs/ |

---

## Tasks

### Phase 1 — Core Config (Tasks 1–3)

**[x] Task 1 — `app.config.ts`: Switch to zoneless provider**
- File: `frontend/src/app/app.config.ts`
- Replace `provideZoneChangeDetection({ eventCoalescing: true })` → `provideZonelessChangeDetection()`
- Add `withFetch()` to `provideHttpClient(...)`
- Update imports accordingly
- Log: Add comment `// Zoneless mode — no zone.js` above the provider

**[x] Task 2 — `angular.json`: Remove zone.js polyfills**
- File: `frontend/angular.json`
- Remove `"zone.js"` from `build.options.polyfills`
- Remove `"zone.js"` and `"zone.js/testing"` from `test.options.polyfills`

**[x] Task 3 — `app.ts`: Add OnPush to root component**
- File: `frontend/src/app/app.ts`
- Add `changeDetection: ChangeDetectionStrategy.OnPush`

---

### Phase 2 — Pattern Fixes (Tasks 4–5)

**[x] Task 4 — `LocationDetailComponent`: Fix effect+subscribe pattern**
- File: `frontend/src/app/features/location/location-detail/location-detail.component.ts`
- Replace `effect(() => { this.loadLocation() })` with `toObservable(this.id).pipe(switchMap(...))` pattern
- Uses proper `switchMap` cancellation — no duplicate in-flight requests on route change
- Log: `console.debug('[LocationDetail] Loading location id=%s', id)` / `console.debug('[LocationDetail] Location loaded: %o', location)`

**[x] Task 5 — `SearchComponent`: Unify destroy pattern**
- File: `frontend/src/app/features/search/search.component.ts`
- Remove `private destroy$ = new Subject<void>()` and `ngOnDestroy`
- Replace `takeUntil(this.destroy$)` → `takeUntilDestroyed(this.destroyRef)` in debounce pipeline
- Log: `console.debug('[Search] Debounce pipeline initialized')` / `console.debug('[Search] Query changed: %s', query)`

---

### Phase 3 — Verification & Docs (Tasks 6–7)

**Task 6 — Build & smoke test** _(blocked by Tasks 1–5)_
- Run `cd frontend && npm run build`
- Verify: no TypeScript errors, no zone.js warnings in browser console
- Smoke test: home → location detail → back → another location → search

**Task 7 — Update documentation** _(blocked by Task 6)_
- `docs/architecture.md` — add zoneless mode note
- `.ai-factory/ARCHITECTURE.md` — add to anti-patterns: `❌ ChangeDetectionStrategy.Default in zoneless mode`
- `MEMORY.md` — add "Angular zoneless mode"

---

## Commit Plan

| After tasks | Commit message |
|---|---|
| 1, 2, 3 | `chore(frontend): migrate Angular to zoneless mode — remove zone.js, switch provider, add OnPush to App` |
| 4, 5 | `refactor(frontend): fix zoneless-incompatible patterns — toObservable+switchMap, takeUntilDestroyed` |
| 7 | `docs: update architecture docs to reflect Angular zoneless mode` |
