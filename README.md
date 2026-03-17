# MemoApi
Memmo Api

A REST API for the Angular Memmo frontend application.

## Getting Started

### Prerequisites

- Node.js 18+
- npm

### Installation

```bash
npm install
```

### Development

```bash
npm run dev
```

### Build

```bash
npm run build
```

### Start (production)

```bash
npm start
```

The server listens on port `3000` by default. Set the `PORT` environment variable to override.

## API Endpoints

### Health

| Method | Path      | Description      |
|--------|-----------|------------------|
| GET    | /health   | Health check     |

### Memos

| Method | Path         | Description           |
|--------|--------------|-----------------------|
| GET    | /memos       | List all memos        |
| GET    | /memos/:id   | Get a single memo     |
| POST   | /memos       | Create a new memo     |
| PUT    | /memos/:id   | Update an existing memo |
| DELETE | /memos/:id   | Delete a memo         |

### Memo object

```json
{
  "id": "uuid",
  "title": "string",
  "content": "string",
  "createdAt": "ISO 8601 date string",
  "updatedAt": "ISO 8601 date string"
}
```

### Create / Update request body

```json
{
  "title": "string",
  "content": "string"
}
```

Both fields are required when creating a memo. Either field may be omitted when updating (only provided fields are changed).
