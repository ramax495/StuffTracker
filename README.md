# StuffTracker

> Never forget where you put things.

StuffTracker is a **Telegram Mini App** for tracking physical items and their storage locations. Add items, assign them to nested locations (rooms → shelves → boxes), and search for anything — all within Telegram.

## Quick Start

```bash
make setup   # First-time: restore deps, start DB, run migrations
make dev     # Start DB + backend watcher + frontend (Ctrl+C stops all)
```

Open the app at `http://localhost:4200` — in dev mode, Telegram auth is bypassed automatically.

Run `make help` for all available targets. See [Getting Started](docs/getting-started.md) for manual setup steps.

## Key Features

- **Item tracking** — Add items with name, description, and quantity
- **Hierarchical locations** — Nested storage locations (rooms, shelves, boxes, etc.)
- **Breadcrumb navigation** — Always know where you are in the hierarchy
- **Search** — Find items by name or filter by location
- **Telegram-native** — Authenticates via Telegram `initData`; works as a Mini App

## Example

```
📦 Kitchen
  └── 🗄 Cabinet
        ├── Coffee beans (1)
        └── Tea bags (3)
🏠 Living Room
  └── 📦 Bookshelf
        └── HDMI cable (2)
```

---

## Documentation

| Guide | Description |
|-------|-------------|
| [Getting Started](docs/getting-started.md) | Prerequisites, setup, running locally |
| [Architecture](docs/architecture.md) | Project structure and patterns |
| [API Reference](docs/api.md) | All REST endpoints with request/response |
| [Configuration](docs/configuration.md) | Environment variables and config options |

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | .NET 8, FastEndpoints, EF Core 8 |
| Frontend | Angular 20, TypeScript 5.9 |
| Database | PostgreSQL 16 |
| Platform | Telegram Mini App (`@twa-dev/sdk`) |

## License

MIT
