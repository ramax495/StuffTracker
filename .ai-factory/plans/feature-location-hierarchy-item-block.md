# Feature Plan: Location Hierarchy Display in Item Detail Block

**Branch:** `feature/location-hierarchy-item-block`
**Created:** 2026-03-01
**Type:** Feature Enhancement

---

## Overview

Enhance the Location block on the item detail page to display the full location hierarchy (breadcrumbs) instead of just the current location. The display should be visually appealing, responsive across different screen sizes, and provide easy navigation through the hierarchy.

### Current State
- Item detail page shows only the current location name in the Location block
- Full location hierarchy is displayed in breadcrumbs at the top of the page
- Breadcrumb hierarchy data (names + IDs) is already fetched from the API

### Desired State
- Location block displays the full path from root to current location
- Hierarchical display is visually distinct and easy to understand
- Navigation is possible by clicking on parent locations
- Responsive design works on small screens (mobile), tablets, and desktops
- Styling integrates with Telegram Mini App theme

---

## Implementation Settings

| Setting | Value | Rationale |
|---------|-------|-----------|
| **Testing** | Yes, component tests | Signal-based UI components benefit from unit tests for state & interaction |
| **Logging** | Verbose (DEBUG level) | Component lifecycle + user interactions help debug responsive behavior |
| **Documentation** | Yes, update docs | Component pattern + responsive strategy should be documented |

---

## Architecture Decisions

### 1. **Component Design: New `LocationHierarchyComponent` vs Enhance Existing**
   - **Decision:** Create new standalone component `LocationHierarchyComponent`
   - **Rationale:**
     - Separates concerns (breadcrumb nav in header vs hierarchy display in item block)
     - Reusable for other contexts (location detail page, search results, etc.)
     - Follows vertical slice architecture — feature-specific component lives in `features/item/`
     - Avoids bloating existing components

### 2. **Display Strategy: Hierarchical vs Inline**
   - **Options Evaluated:**
     - Inline path: `Home > Garage > Shelf` (space-constrained on mobile)
     - Vertical list: Each level as a separate row with indentation (better for small screens)
     - Breadcrumb-like chips: Visual buttons for each level (interactive & responsive)
   - **Decision:** Breadcrumb-like chips with responsive wrapping
   - **Rationale:**
     - Matches existing breadcrumbs component (UX consistency)
     - Clickable = navigable (matches breadcrumbs behavior)
     - Responsive wrapping handles mobile screens naturally
     - Each chip shows location name + optional icons/counts

### 3. **Responsiveness Strategy**
   - Mobile (< 480px): Show simplified path (hide intermediate levels if needed, or use collapsible)
   - Tablet (480px - 768px): Show full path with wrapping
   - Desktop (> 768px): Show full path with horizontal layout
   - Strategy: Use CSS Grid/Flexbox with wrap + `@media` queries for label adjustments

### 4. **Integration with Item Detail Page**
   - Item detail component already fetches location hierarchy
   - LocationHierarchyComponent receives:
     - `breadcrumbs: Signal<string[]>` — location names
     - `breadcrumbIds: Signal<string[]>` — location IDs for navigation
     - `currentLocationId: Signal<string>` — highlight current location
   - Component emits: `locationSelected(id: string)` → routes to location detail

---

## Task Breakdown

### Phase 1: Component Creation & Core Logic (Tasks 1-3)

**Task 1: Create LocationHierarchyComponent (Skeleton)** ✅
- File: `frontend/src/app/features/item/components/location-hierarchy/location-hierarchy.component.ts`
- File: `frontend/src/app/features/item/components/location-hierarchy/location-hierarchy.component.html`
- File: `frontend/src/app/features/item/components/location-hierarchy/location-hierarchy.component.scss`
- Create standalone component with:
  - Inputs: `breadcrumbs`, `breadcrumbIds`, `currentLocationId` (all as Signals)
  - Output: `locationSelected` event emitter
  - Change detection: `OnPush`
  - Template: Basic structure with `@for` loop over breadcrumbs
- Add DEBUG logging for input changes
- **Deliverable:** Component renders flat list of breadcrumbs with separators

**Task 2: Implement Click Navigation & Responsive Styling** ✅
- Enhance `location-hierarchy.component.ts`:
  - Add `navigateToLocation(id: string)` method
  - Track last item (current location) separately
  - Make non-last items clickable
  - Add haptic feedback (Telegram SDK integration for mini app)
- Enhance `location-hierarchy.component.scss`:
  - Flexbox layout with wrap
  - Mobile breakpoint (< 480px): Smaller font, tighter spacing
  - Tablet/Desktop (≥ 480px): Larger layout
  - Current location: Different styling (bold/highlight)
  - Separator styling (> symbol or similar)
  - Padding/margin adjust for cramped spaces
- Add DEBUG logs for navigation events
- **Deliverable:** Clickable, responsive breadcrumb display with proper styling

**Task 3: Integrate LocationHierarchyComponent into Item Detail Page** ✅
- File: `frontend/src/app/features/item/item-detail/item-detail.component.ts`
- File: `frontend/src/app/features/item/item-detail/item-detail.component.html`
- Replace existing "Location: [name]" display with `<app-location-hierarchy>`
- Pass signals: `location()?.breadcrumbs`, `location()?.breadcrumbIds`, `location()?.id`
- Wire `locationSelected` output → `router.navigate(['/location', id])`
- Add DEBUG log when breadcrumb navigation occurs
- **Deliverable:** Item detail page shows full location hierarchy with working navigation

---

### Phase 2: Styling & Polish (Task 4)

**Task 4: Responsive Design Polish & Telegram Theme Integration** ✅
- Enhance `location-hierarchy.component.scss`:
  - Test on mobile (360px), tablet (768px), desktop (1200px+)
  - Ensure text truncation or abbreviation on very small screens
  - Dark theme support: Colors adapt to Telegram's theme
  - Hover/active states for clickable items
  - Touch target size ≥ 44px for mobile tap
- Add DEBUG logs for viewport changes (if responsive behavior needs debugging)
- **Deliverable:** Component works well on all screen sizes with proper theme support

---

### Phase 3: Testing (Task 5-6)

**Task 5: Write Component Tests for LocationHierarchyComponent** ✅
- File: `frontend/src/app/features/item/components/location-hierarchy/location-hierarchy.component.spec.ts`
- Tests:
  - Renders breadcrumbs when inputs provided
  - Last breadcrumb non-clickable, others clickable
  - `navigateToLocation` called on breadcrumb click
  - Correct styling (current location highlighted)
  - Edge cases: Empty breadcrumbs, single item, null locationIds
  - Responsive behavior (media query testing via `matchMedia` mock)
- Use `TestBed.overrideComponent` for component testing
- Mock `Router` for navigation tests
- **Deliverable:** Tests pass, coverage ≥ 80% for component logic

**Task 6: Test Item Detail Page Integration** ✅
- File: `frontend/src/app/features/item/item-detail/item-detail.component.spec.ts`
- Tests:
  - LocationHierarchyComponent rendered when location data available
  - Navigation to location works when breadcrumb clicked
  - Component signals update correctly when item changes
  - Loading states handled properly
- Update existing item-detail tests if needed
- **Deliverable:** Integration tests pass, navigation works end-to-end

---

### Phase 4: Documentation (Task 7)

**Task 7: Update Documentation** ✅
- File: `docs/architecture.md` ✅
  - Add section: "Responsive Components - LocationHierarchyComponent Example"
  - Document: Signal inputs, outputs, responsive strategy, CSS patterns
- File: `AGENTS.md` ✅
  - Created frontend component map with all shared and feature components
  - Added LocationHierarchyComponent to features/item section
- Code comments: ✅
  - Added JSDoc comments in `location-hierarchy.component.ts`
  - Added detailed comments explaining responsive breakpoints
  - Documented Telegram theme integration
- **Deliverable:** Documentation updated, clear examples for future component development

---

## Commit Checkpoints

Since we have 7 tasks, use these checkpoints for meaningful commits:

1. **After Task 3:**
   ```
   feat(frontend): add location hierarchy display component

   - Create standalone LocationHierarchyComponent
   - Implement responsive breadcrumb-style hierarchy display
   - Integrate into item detail page
   - Add clickable navigation to parent locations
   ```

2. **After Task 4:**
   ```
   feat(frontend): polish location hierarchy styling and responsiveness

   - Responsive design for mobile/tablet/desktop
   - Dark theme support
   - Touch targets optimized for mobile
   ```

3. **After Task 6:**
   ```
   test(frontend): add comprehensive tests for location hierarchy

   - Component unit tests (80%+ coverage)
   - Integration tests with item detail page
   ```

4. **After Task 7:**
   ```
   docs(frontend): document location hierarchy component pattern

   - Update architecture guide
   - Add responsive component examples
   ```

---

## Key Files to Create/Modify

### Create (New)
- `frontend/src/app/features/item/components/location-hierarchy/location-hierarchy.component.ts`
- `frontend/src/app/features/item/components/location-hierarchy/location-hierarchy.component.html`
- `frontend/src/app/features/item/components/location-hierarchy/location-hierarchy.component.scss`
- `frontend/src/app/features/item/components/location-hierarchy/location-hierarchy.component.spec.ts`

### Modify (Existing)
- `frontend/src/app/features/item/item-detail/item-detail.component.ts`
- `frontend/src/app/features/item/item-detail/item-detail.component.html`
- `frontend/src/app/features/item/item-detail/item-detail.component.spec.ts`
- `docs/architecture.md`
- `AGENTS.md`

---

## Design Reference

### LocationHierarchyComponent Behavior

**Input Example:**
```typescript
breadcrumbs: Signal<['Home', 'Garage', 'Shelf']>
breadcrumbIds: Signal<['loc-1', 'loc-2', 'loc-3']>
currentLocationId: Signal<'loc-3'>
```

**Template Output (Desktop):**
```
🏠 > Garage > Shelf
     ↑ clickable        ↑ clickable   ↑ current (bold)
```

**Template Output (Mobile < 480px):**
```
🏠 > ...
    Garage > Shelf
```
(or collapsible version if needed)

### Responsive Breakpoints
- **Mobile:** < 480px
  - Single line, smaller text
  - Abbreviate long names if needed (e.g., "Garag..." for "Garage")
- **Tablet:** 480px - 768px
  - Multiline wrap, readable text
- **Desktop:** > 768px
  - Full text, horizontal layout

### Current Location Styling
- Bold font weight
- Slightly different color (lighter or muted)
- Not clickable (no link cursor)

---

## Testing Strategy

### Unit Tests (LocationHierarchyComponent)
- Input/output signal handling
- Navigation logic
- Responsive CSS class application
- Telegram haptic feedback trigger
- Edge cases (empty, single item, null IDs)

### Integration Tests (Item Detail)
- Component renders when location data loads
- Navigation works end-to-end
- Signal lifecycle and cleanup

### Manual Testing Checklist
- [ ] Mobile (360px width) - text fits, no overflow
- [ ] Tablet (768px width) - full path visible
- [ ] Desktop (1200px+ width) - optimal spacing
- [ ] Dark theme - colors contrast properly
- [ ] Click on parent location - navigates correctly
- [ ] Haptic feedback triggers on click (Telegram app)

---

## Risk & Mitigation

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Responsive overflow on mobile | Poor UX on small screens | Use abbreviation + CSS wrapping (Task 4 testing) |
| Breadcrumb data becomes stale | Stale navigation links | Use Signals, computed properties auto-update |
| Cross-slice import violation | Architecture violation | Component lives in `features/item/`, uses `core/api/` only |
| Haptic feedback not available | User doesn't get feedback on old Telegram version | Graceful fallback (check `window.Telegram.WebApp.HapticFeedback` exists) |

---

## Next Steps

1. **Review this plan** — Confirm approach before implementation
2. **Run `/aif-implement`** — Execute all 7 tasks sequentially
3. **Test on device** — Verify responsive behavior on actual mobile device or simulator
4. **Merge to main** — Create PR, request review, merge when approved

---

## Success Criteria

✅ Location hierarchy fully visible in item detail page
✅ Clickable parent location navigation working
✅ Responsive design passes all breakpoints
✅ Component tests pass (80%+ coverage)
✅ Dark theme integration complete
✅ Documentation updated
✅ No architecture rule violations

