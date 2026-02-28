[Back to README](../README.md) · [Architecture →](architecture.md)

# Getting Started

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | 8.0+ | [dotnet.microsoft.com](https://dotnet.microsoft.com/download) |
| Node.js | 20+ | For Angular frontend |
| Docker | Latest | For PostgreSQL |
| Angular CLI | 20+ | `npm install -g @angular/cli` |

## Local Development Setup

### With Make (recommended)

```bash
make setup   # Restore deps, start DB, run migrations (first time only)
make dev     # Start DB + backend watcher + frontend (Ctrl+C stops all)
```

Run `make help` for a full list of targets.

---

### Manual Steps

### 1. Start PostgreSQL

```bash
docker compose up postgres -d
```

This starts PostgreSQL 16 on port `5432` with:
- Database: `stufftracker`
- User: `stufftracker`
- Password: `stufftracker_dev_password`

### 2. Run the Backend

```bash
cd backend
dotnet run --project src/StuffTracker.Api
```

The API starts at `http://localhost:5000`. Swagger UI is available at `http://localhost:5000/swagger`.

**Apply database migrations** (first run):

```bash
cd backend
dotnet ef database update --project src/StuffTracker.Api
```

### 3. Run the Frontend

```bash
cd frontend
npm install
npm start
```

The app opens at `http://localhost:4200`.

> **Dev mode**: Set `Telegram__DevMode: true` in `appsettings.Development.json` to bypass Telegram auth. A fake user will be injected automatically — no Telegram bot required for local development.

---

## Full Stack with Docker

To run the complete stack (postgres + backend + frontend) in containers:

```bash
docker compose --profile full up
```

| Service | URL |
|---------|-----|
| Frontend | http://localhost:4200 |
| Backend API | http://localhost:5000 |
| Swagger | http://localhost:5000/swagger |
| PostgreSQL | localhost:5432 |

---

## Telegram Mini App Setup (Production)

To run as a real Telegram Mini App:

1. Create a bot with [@BotFather](https://t.me/BotFather) → get the `BotToken`
2. Set up a Mini App on your bot (`/newapp` in BotFather)
3. Deploy frontend to HTTPS (required by Telegram)
4. Configure backend env vars (see [Configuration](configuration.md))
5. Set `Telegram__WebAppUrl` to your frontend HTTPS URL

---

## Verify It Works

After startup, hit the API:

```bash
# Health check (no auth required)
curl http://localhost:5000/swagger

# With dev mode enabled — validate fake user
curl http://localhost:5000/api/auth/me \
  -H "X-Telegram-Init-Data: devmode"
```

Expected: `200 OK` with user JSON.

---

## Next Steps

- See [Architecture](architecture.md) to understand the codebase layout
- See [API Reference](api.md) for all available endpoints
- See [Configuration](configuration.md) for all environment variables

## See Also

- [Configuration](configuration.md) — all environment variables
- [Architecture](architecture.md) — project structure and patterns
