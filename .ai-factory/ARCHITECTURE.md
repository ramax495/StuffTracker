# Architecture: Vertical Slice Architecture

## Overview
StuffTracker uses **Vertical Slice Architecture** — each feature owns its complete vertical stack from HTTP endpoint through to database access. Rather than horizontal layers (controller → service → repository), code is organised by feature (e.g., `CreateLocation`, `SearchItems`), with each slice containing everything it needs: endpoint, request/response DTOs, validator, and repository.

This was chosen because:
- FastEndpoints naturally encourages per-endpoint classes
- The domain is feature-clear (Items, Locations, Search, Auth) with minimal cross-feature coupling
- Small team size benefits from the high locality of change: touching a feature means touching one folder
- Domain models and shared infrastructure live in support layers, keeping slices lean

## Decision Rationale
- **Project type:** Telegram Mini App — CRUD-heavy with clear feature boundaries
- **Tech stack:** .NET 8 / FastEndpoints (backend), Angular 20 (frontend)
- **Key factor:** FastEndpoints + small team = vertical slices are the natural fit

---

## Backend Folder Structure

```
backend/src/StuffTracker.Api/
├── Domain/                         # Core entities — NO framework dependencies
│   ├── Item.cs
│   ├── StorageLocation.cs
│   └── User.cs
│
├── Application/                    # Cross-cutting application services
│   └── (e.g., UserContext, mapping helpers)
│
├── Infrastructure/                 # Shared infrastructure implementations
│   ├── Persistence/
│   │   ├── AppDbContext.cs         # EF Core DbContext
│   │   ├── Migrations/             # EF Core migrations (never edit manually)
│   │   └── Repositories/          # Repository implementations (ILocationRepository, etc.)
│   └── Telegram/                  # Telegram Bot client, initData validation
│
├── Common/                        # Framework conveniences (not business logic)
│   ├── BaseEndpoint.cs            # FastEndpoints base with GetUserId() helper
│   ├── ApiErrorResponse.cs        # Shared error shape
│   └── Middleware/                # Auth middleware, exception handler
│
└── Features/                      # ONE folder per use-case (vertical slices)
    ├── Auth/
    │   └── ValidateUser/
    │       ├── ValidateUserEndpoint.cs
    │       ├── ValidateUserRequest.cs
    │       └── ValidateUserValidator.cs
    ├── Items/
    │   ├── CreateItem/
    │   ├── GetItem/
    │   ├── UpdateItem/
    │   └── DeleteItem/
    ├── Locations/
    │   ├── CreateLocation/
    │   │   ├── CreateLocationEndpoint.cs
    │   │   ├── CreateLocationRequest.cs
    │   │   ├── CreateLocationValidator.cs
    │   │   └── Persistence/       # Optional: slice-local queries if simple enough
    │   ├── GetLocation/
    │   ├── UpdateLocation/
    │   ├── DeleteLocation/
    │   ├── MoveLocation/
    │   ├── GetLocationTree/
    │   ├── GetTopLevelLocations/
    │   └── Shared/                # Shared DTOs used by multiple Location slices
    │       └── LocationResponse.cs
    └── Search/
        └── SearchAll/
```

## Frontend Folder Structure

```
frontend/src/app/
├── core/                          # Singleton services & global concerns
│   ├── api/                       # HTTP service layer (Angular services)
│   ├── auth/                      # Auth state, guards
│   └── navigation.service.ts
│
├── telegram/                      # TWA SDK wrapper (isolates @twa-dev/sdk)
│
├── shared/                        # Reusable, feature-agnostic UI components
│   └── components/                # e.g., loading spinner, confirm dialog
│
└── features/                      # One folder per feature (mirrors backend slices)
    ├── home/                      # Root location list view
    ├── item/                      # Item detail & form
    ├── location/                  # Location detail, form, move modal
    └── search/                    # Search results
```

---

## Dependency Rules

### Backend

```
Domain        ──────────────────────────────► (nothing)
Application   ──────────────────────────────► Domain
Infrastructure ─────────────────────────────► Domain, Application
Features (slices) ──────────────────────────► Domain, Infrastructure, Common
Common        ──────────────────────────────► Domain (minimal)
```

- ✅ Feature slices may use Domain entities directly
- ✅ Feature slices may inject repository interfaces from Infrastructure
- ✅ Feature slices may use shared DTOs from sibling `Shared/` folders
- ❌ Feature slices must NOT import from other feature slices
- ❌ Domain must NOT reference Infrastructure, Features, or Common
- ❌ Never import from `Features/X/` into `Features/Y/` — use shared DTOs or Domain instead

### Frontend

- ✅ Features may inject services from `core/`
- ✅ Features may use components from `shared/`
- ✅ Features may use `telegram/` wrapper
- ❌ Features must NOT import from other features
- ❌ `shared/` must NOT import from `features/`
- ❌ `core/` must NOT import from `features/`

---

## Layer/Module Communication

### Backend
- Slices communicate with the database through **repository interfaces** injected via DI
- Cross-slice data needs go through the **database** (never slice-to-slice method calls)
- Auth context flows via `BaseEndpoint.GetUserId()` — reads the validated Telegram user ID from the HTTP context
- Validation is done in the slice's `*Validator.cs` (FastEndpoints AbstractValidator)

### Frontend
- Feature components call `core/api/` services for HTTP
- Route params drive feature navigation (no cross-feature service calls)
- Telegram SDK access is always via the `telegram/` wrapper — never `window.Telegram.WebApp` directly

---

## Key Principles

1. **Locality of change**: A new feature = a new folder under `Features/`. Modifying a feature touches only that folder.
2. **No cross-slice imports**: If two slices need the same data shape, extract a `Shared/` DTO in the parent feature folder or use the Domain entity.
3. **Domain is pure**: `Domain/` entities have no EF Core annotations, no framework attributes, no API concerns.
4. **NoTracking by default**: All EF Core queries use `.AsNoTracking()` unless explicitly performing an update.
5. **Migrations never edited manually**: Always use `dotnet ef migrations add` — never hand-edit migration files.
6. **Angular standalone only**: No NgModules. Every Angular component is standalone with explicit `imports: []`.

---

## Code Examples

### Backend — Slice endpoint (FastEndpoints)

```csharp
// Features/Locations/CreateLocation/CreateLocationEndpoint.cs
namespace StuffTracker.Api.Features.Locations.CreateLocation;

public class CreateLocationEndpoint : BaseEndpoint<CreateLocationRequest, LocationResponse>
{
    private readonly ILocationRepository _locationRepository;

    public CreateLocationEndpoint(ILocationRepository locationRepository)
    {
        _locationRepository = locationRepository;
    }

    public override void Configure()
    {
        Post("/locations");
        // Auth is applied globally via middleware; no [Authorize] needed here
    }

    public override async Task HandleAsync(CreateLocationRequest req, CancellationToken ct)
    {
        var userId = GetUserId(); // from BaseEndpoint — never pass userId via request body

        var location = new StorageLocation
        {
            Name = req.Name,
            UserId = userId,
            ParentId = req.ParentId
        };

        await _locationRepository.CreateAsync(location, ct);
        await SendCreatedAtAsync<GetLocationEndpoint>(new { id = location.Id },
            LocationResponse.From(location), cancellation: ct);
    }
}
```

### Backend — Repository query (NoTracking)

```csharp
// Infrastructure/Persistence/Repositories/LocationRepository.cs
public async Task<StorageLocation?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct)
{
    return await _db.Locations
        .AsNoTracking()                         // ✅ always NoTracking for reads
        .Where(l => l.Id == id && l.UserId == userId)
        .FirstOrDefaultAsync(ct);
}
```

### Backend — Domain entity (pure C#, no EF attributes)

```csharp
// Domain/StorageLocation.cs
public class StorageLocation
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid? ParentId { get; set; }
    public List<string> BreadcrumbIds { get; set; } = [];
    // No [Key], no [ForeignKey], no EF annotations here
}
```

### Frontend — Feature component (Angular 20 standalone)

```typescript
// features/location/location-detail/location-detail.component.ts
@Component({
  selector: 'app-location-detail',
  standalone: true,              // always — never use NgModules
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, AsyncPipe],
  templateUrl: './location-detail.component.html',
})
export class LocationDetailComponent {
  private locationService = inject(LocationService); // from core/api/
  private route = inject(ActivatedRoute);

  location$ = this.route.paramMap.pipe(
    switchMap(params => this.locationService.getById(params.get('id')!))
  );
}
```

---

## Anti-Patterns

- ❌ **Cross-slice imports**: `import { SomethingFromCreateLocation } from '../CreateLocation/...'` inside `UpdateLocation` — use `Shared/` or Domain instead
- ❌ **Business logic in endpoints**: Endpoints orchestrate; complex logic belongs in Domain or a service class
- ❌ **Tracking queries for reads**: Always `.AsNoTracking()` unless you intend to `.SaveChanges()` on the result
- ❌ **Editing migrations manually**: Always `dotnet ef migrations add <Name>` — hand-edited migrations cause drift
- ❌ **Direct `window.Telegram.WebApp` access**: Always use the `telegram/` service wrapper
- ❌ **NgModules**: StuffTracker is fully standalone — never introduce `@NgModule`
- ❌ **Passing userId in request body**: The authenticated user ID comes from `BaseEndpoint.GetUserId()`, never from client-supplied data
- ❌ **`ChangeDetectionStrategy.Default` in zoneless mode**: All components MUST use `OnPush` — the app runs zoneless (`provideZonelessChangeDetection()`), so Default will not detect changes
- ❌ **`effect(() => { observable.subscribe() })`**: Creates overlapping subscriptions on signal change — use `toObservable(signal).pipe(switchMap(...))` instead
- ❌ **Manual `Subject<void>` + `ngOnDestroy` teardown**: Use `takeUntilDestroyed(destroyRef)` for all Observable subscriptions
