# Implementation Plan: Dark Theme Toggle

Branch: feature/dark-theme
Created: 2026-02-28

## Settings
- Testing: no
- Logging: verbose
- Docs: no

## Overview

Add a dark theme and a theme toggle button to the Angular frontend.

**Scope:** Frontend only. No backend changes.

**Strategy:**
- `ThemeService` owns theme state as a signal; persists to `localStorage`; falls back to Telegram's `colorScheme` or system `prefers-color-scheme`
- Dark theme CSS defined via `[data-theme="dark"]` selector overriding all `--tg-theme-*` variables
- `ThemeToggleComponent` renders in the fixed app header (stacked below the search button)
- Applied by setting `data-theme` attribute on `document.documentElement`

## Key Files

| File | Change |
|------|--------|
| `frontend/src/app/core/theme/theme.service.ts` | Create |
| `frontend/src/styles.scss` | Add dark theme block |
| `frontend/src/app/shared/components/theme-toggle/theme-toggle.component.ts` | Create |
| `frontend/src/app/shared/components/theme-toggle/theme-toggle.component.scss` | Create |
| `frontend/src/app/app.ts` | Inject ThemeService, add ThemeToggleComponent |
| `frontend/src/app/app.html` | Add `<app-theme-toggle />` to header |
| `frontend/src/app/app.scss` | Flex-column header layout |

## Tasks

### Phase 1: Core Service

- [x] Task 1: Create `ThemeService` in `core/theme/theme.service.ts`
  - Signal `theme: Signal<'light' | 'dark'>`
  - Computed `isDark = computed(() => theme() === 'dark')`
  - `initialize()`: reads localStorage → TelegramService.getColorScheme() → applies
  - `toggleTheme()`: toggles, saves to localStorage, calls `applyTheme()`
  - `applyTheme()`: sets `document.documentElement.setAttribute('data-theme', value)`
  - Guard all DOM/storage access with `isPlatformBrowser(platformId)`

### Phase 2: Styling & Component (independent of each other, depend on Task 1)

- [x] Task 2: Add dark theme CSS to `styles.scss` (depends on 1)
  - Add `[data-theme="dark"]` block after `:root` block
  - Override all `--tg-theme-*` variables with iOS-style dark values
  - Add smooth color transition on `html` element

- [x] Task 3: Create `ThemeToggleComponent` in `shared/components/theme-toggle/` (depends on 1)
  - Standalone, OnPush
  - Sun/moon SVG icons toggled via `@if (isDark())`
  - Styled to match `.app-header__search-btn` (44×44px circle)
  - Dynamic `aria-label`
  - Haptic feedback via TelegramService

### Phase 3: Integration

- [x] Task 4: Wire up in `App` root component (depends on 1, 2, 3)
  - Inject `ThemeService`, call `initialize()` in `ngOnInit`
  - Add `ThemeToggleComponent` to imports
  - Add `<app-theme-toggle />` to `app.html` header
  - Update `app.scss` header to flex-column for stacked buttons

## Commit Plan

Less than 5 tasks — single commit at the end:

```
feat(frontend): add dark theme toggle and dark CSS theme
```
