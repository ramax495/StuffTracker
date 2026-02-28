[← API Reference](api.md) · [Back to README](../README.md)

# Configuration

## Backend

Configuration is loaded from `appsettings.json`, overridden by `appsettings.{Environment}.json`, and further overridden by environment variables.

### Connection String

| Key | Description | Default (dev) |
|-----|-------------|---------------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=localhost;Port=5432;Database=stufftracker;Username=stufftracker;Password=stufftracker_dev_password` |

**Format:** `Host=<host>;Port=<port>;Database=<db>;Username=<user>;Password=<pass>`

---

### Telegram Settings

| Key | Description | Required |
|-----|-------------|----------|
| `Telegram__BotToken` | Telegram Bot API token from @BotFather | Yes (prod) |
| `Telegram__WebAppUrl` | Frontend URL (used for bot configuration) | No |
| `Telegram__DevMode` | Bypass initData validation — **never enable in production** | No |
| `Telegram__DevUserId` | Fake Telegram user ID injected in dev mode | No |
| `Telegram__DevUserFirstName` | Fake user first name for dev mode | No |

**Example `appsettings.Development.json`:**
```json
{
  "Telegram": {
    "BotToken": "123456:ABC-DEF...",
    "WebAppUrl": "http://localhost:4200",
    "DevMode": true,
    "DevUserId": 123456789,
    "DevUserFirstName": "Developer"
  }
}
```

---

### Logging

| Key | Description | Default |
|-----|-------------|---------|
| `Logging__LogLevel__Default` | Root log level | `Information` |
| `Logging__LogLevel__Microsoft.AspNetCore` | ASP.NET Core log level | `Warning` |
| `Logging__LogLevel__Microsoft.EntityFrameworkCore` | EF Core log level | `Warning` |

Set `Microsoft.EntityFrameworkCore.Database.Command` to `Information` to log SQL queries in development.

---

## Frontend

Environment configuration lives in `frontend/src/environments/`:

| File | Used when |
|------|-----------|
| `environment.ts` | Production build (`ng build`) |
| `environment.development.ts` | Dev server (`ng serve`) |

### Environment Variables

| Key | Description | Default |
|-----|-------------|---------|
| `apiUrl` | Backend API base URL | `http://localhost:5000/api` |
| `telegram.botUsername` | Telegram bot username (for deep links) | `StuffTrackerDevBot` |

**Example `environment.ts` (production):**
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://api.yourdomain.com/api',
  telegram: {
    botUsername: 'YourBotUsername'
  }
};
```

---

## Docker Compose

Environment variables for the `backend` service in `docker-compose.yml`:

| Variable | Description |
|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Development` or `Production` |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Telegram__BotToken` | Bot token (from `.env` file or shell) |
| `Telegram__DevMode` | `true`/`false` |
| `Telegram__DevUserId` | Dev user Telegram ID |
| `Telegram__DevUserFirstName` | Dev user first name |

**Using a `.env` file:**
```env
Telegram__BotToken=123456:ABC-DEF...
```

Then in `docker-compose.yml`, variables like `${Telegram__BotToken:-}` will pick it up.

---

## Security Notes

- `Telegram__DevMode: true` disables authentication entirely — **never use in production**
- `BotToken` is the secret used for HMAC validation — treat it like a private key
- Do not commit real bot tokens to version control — use `.env` files or secrets management

---

## See Also

- [Getting Started](getting-started.md) — how to run locally
- [API Reference](api.md) — authentication header details
