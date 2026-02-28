[← Architecture](architecture.md) · [Back to README](../README.md) · [Configuration →](configuration.md)

# API Reference

## Base URL

| Environment | URL |
|-------------|-----|
| Local dev | `http://localhost:5000/api` |
| Docker full | `http://localhost:5000/api` |

Swagger UI: `http://localhost:5000/swagger`

## Authentication

All endpoints require a `X-Telegram-Init-Data` header containing the Telegram WebApp `initData` string (HMAC-validated against the bot token).

```
X-Telegram-Init-Data: query_id=...&user=...&hash=...
```

In **dev mode** (`Telegram__DevMode: true`), any value is accepted and a fake user is injected.

**Error responses:**

| Status | Meaning |
|--------|---------|
| `401 Unauthorized` | Missing or invalid initData |
| `400 Bad Request` | Validation error |
| `404 Not Found` | Resource not found or doesn't belong to user |

All error responses follow:
```json
{ "error": "message", "details": {} }
```

---

## Auth

### GET /auth/me

Get (or create) the current authenticated user.

**Response `200`:**
```json
{
  "telegramId": 123456789,
  "firstName": "Alice",
  "lastName": "Smith",
  "username": "alice",
  "languageCode": "en",
  "createdAt": "2024-01-15T10:00:00Z"
}
```

---

## Locations

### GET /locations

List all top-level locations (no parent) for the current user.

**Response `200`:** Array of `LocationListItem`:
```json
[
  { "id": "uuid", "name": "Kitchen", "childCount": 2 }
]
```

---

### POST /locations

Create a new location.

**Request body:**
```json
{
  "name": "Kitchen",       // required, 1–200 chars
  "parentId": "uuid"       // optional, null = top-level
}
```

**Response `201`:** `LocationResponse` (see below)

---

### GET /locations/{id}

Get a single location with breadcrumbs.

**Response `200`:** `LocationResponse`:
```json
{
  "id": "uuid",
  "name": "Shelf",
  "parentId": "uuid",
  "breadcrumbs": ["Kitchen", "Cabinet"],
  "breadcrumbIds": ["uuid-a", "uuid-b"],
  "depth": 2,
  "createdAt": "2024-01-15T10:00:00Z",
  "updatedAt": "2024-01-15T10:00:00Z"
}
```

---

### PATCH /locations/{id}

Rename a location.

**Request body:**
```json
{ "name": "New Name" }   // required, 1–200 chars
```

**Response `200`:** `LocationResponse`

---

### DELETE /locations/{id}

Delete a location. Fails if the location has children or contains items.

**Response `204`:** No content

---

### PATCH /locations/{id}/move

Move a location to a new parent (or to root).

**Request body:**
```json
{ "parentId": "uuid" }   // null to move to root
```

**Response `200`:** `LocationResponse`

---

### GET /locations/{id}/tree

Get a location and its entire sub-tree.

**Response `200`:** Nested tree structure:
```json
{
  "id": "uuid",
  "name": "Kitchen",
  "children": [
    {
      "id": "uuid",
      "name": "Cabinet",
      "children": []
    }
  ]
}
```

---

## Items

### POST /items

Create a new item.

**Request body:**
```json
{
  "name": "Coffee beans",       // required, 1–200 chars
  "description": "Dark roast",  // optional
  "quantity": 2,                // optional, default 1, must be ≥ 1
  "locationId": "uuid"          // required
}
```

**Response `201`:** `ItemResponse`:
```json
{
  "id": "uuid",
  "name": "Coffee beans",
  "description": "Dark roast",
  "quantity": 2,
  "locationId": "uuid",
  "createdAt": "2024-01-15T10:00:00Z",
  "updatedAt": "2024-01-15T10:00:00Z"
}
```

---

### GET /items/{id}

Get a single item with location path.

**Response `200`:** `ItemDetailResponse` (extends `ItemResponse`):
```json
{
  "id": "uuid",
  "name": "Coffee beans",
  "description": "Dark roast",
  "quantity": 2,
  "locationId": "uuid",
  "locationName": "Cabinet",
  "locationPath": ["Kitchen", "Cabinet"],
  "createdAt": "2024-01-15T10:00:00Z",
  "updatedAt": "2024-01-15T10:00:00Z"
}
```

---

### PATCH /items/{id}

Update an item's name, description, or quantity. All fields optional — only provided fields are updated.

**Request body:**
```json
{
  "name": "Coffee beans",       // optional, 1–200 chars
  "description": "Light roast", // optional
  "quantity": 3                 // optional, must be ≥ 1
}
```

**Response `200`:** `ItemResponse`

---

### DELETE /items/{id}

Delete an item.

**Response `204`:** No content

---

### PATCH /items/{id}/move

Move an item to a different location.

**Request body:**
```json
{ "locationId": "uuid" }   // required
```

**Response `200`:** `ItemResponse`

---

## Search

### GET /search/items

Search for items by name or location.

**Query parameters:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `q` | string | — | Search query (1–100 chars). Matches item name. |
| `locationId` | UUID | — | Filter to items in this location (includes nested). |
| `limit` | int | 50 | Max results (1–100). |
| `offset` | int | 0 | Skip N results (pagination). |

**Example:**
```
GET /search/items?q=coffee&locationId=uuid&limit=20&offset=0
```

**Response `200`:**
```json
{
  "items": [
    { "id": "uuid", "name": "Coffee beans", "quantity": 2 }
  ],
  "total": 1,
  "limit": 20,
  "offset": 0
}
```

---

## See Also

- [Configuration](configuration.md) — Telegram bot token and database settings
- [Architecture](architecture.md) — how endpoints are structured
