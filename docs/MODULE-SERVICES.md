# Platform Services

> Built-in services that functions can access via host functions.

---

## KV Store

### Purpose
Durable key-value storage for function state, configuration, and data.

### Interface (from inside a function)

```typescript
import { kv } from '@ubiq/sdk';

// Set a value
await kv.set('user:123', JSON.stringify({ name: 'Alice' }));

// Get a value
const user = JSON.parse(await kv.get('user:123'));

// Delete a value
await kv.delete('user:123');

// List keys by prefix
const keys = await kv.list('user:');

// Atomic operations
await kv.setIfNotExists('lock:resource', 'owner-1', { ttl: 30000 });
```

### Namespacing
- By default, each function has its own KV namespace
- Cross-function access requires explicit permission in manifest
- Namespace format: `{project}:{function}:{key}`

### Backends

| Environment | Backend | Notes |
|-------------|---------|-------|
| Development | SQLite | Embedded, zero config |
| Production (C#) | Garnet | Microsoft's Redis-compatible cache |
| Production (Rust) | sled / RocksDB | Embedded high-performance |
| Production (Distributed) | Redis / DynamoDB | Pluggable via config |

### Limits

| Limit | Default |
|-------|---------|
| Key size | 256 bytes |
| Value size | 1 MB |
| Total storage per function | 100 MB |
| Keys per function | 100,000 |

---

## File Storage

### Purpose
Sandboxed object/file storage. Each function gets an isolated storage area.

### Interface

```typescript
import { storage } from '@ubiq/sdk';

// Write a file
await storage.write('images/photo.jpg', imageData);

// Read a file
const data = await storage.read('images/photo.jpg');

// List files
const files = await storage.list('images/');
// → [{ path: 'images/photo.jpg', size: 12345, modified: '2025-...' }]

// Delete a file
await storage.delete('images/photo.jpg');

// Get metadata
const meta = await storage.stat('images/photo.jpg');
// → { size: 12345, mime: 'image/jpeg', modified: '2025-...' }
```

### Backends

| Environment | Backend |
|-------------|---------|
| Development | Local filesystem (`./data/{function}/`) |
| Production | S3-compatible (MinIO, AWS S3, R2) |

### Security
- Each function can only access its own storage namespace
- No path traversal allowed (enforced at host function level)
- MIME type detection for uploaded files
- Virus scanning via plugin (optional)

---

## Event System

### Purpose
Decouple functions via publish/subscribe events.

### Publishing

```typescript
import { events } from '@ubiq/sdk';

await events.emit('user.created', {
  id: '123',
  name: 'Alice',
  email: 'alice@example.com'
});
```

### Subscribing

In `ubiq.toml`:
```toml
[triggers]
"user.created" = "functions/on-user-created.ts"
"order.completed" = "functions/send-receipt.ts"
```

The subscriber function receives the event:
```typescript
// functions/on-user-created.ts
export default function(event) {
  // event.topic = "user.created"
  // event.payload = { id: "123", name: "Alice", ... }
  // event.timestamp = 1234567890
  // event.source = "create-user"
}
```

### Guarantees
- At-least-once delivery
- Dead letter queue for failed handlers (after N retries)
- Event ordering within a topic (best effort)

### Backends

| Environment | Backend |
|-------------|---------|
| Development | In-process pub/sub |
| Production | NATS, Kafka, or SQS (pluggable) |

---

## HTTP Client (Outbound)

### Purpose
Allow functions to make HTTP requests to external services.

### Interface

```typescript
import { http } from '@ubiq/sdk';

const response = await http.get('https://api.example.com/users');
// → { status: 200, headers: {...}, body: '...' }

const response = await http.post('https://api.example.com/users', {
  body: JSON.stringify({ name: 'Alice' }),
  headers: { 'Content-Type': 'application/json' }
});
```

### Permission Gating
Functions must declare allowed URLs:

```toml
[functions.my-function]
permissions = [
  "http:get:api.example.com/*",
  "http:post:hooks.slack.com/services/*"
]
```

Requests to undeclared URLs are rejected immediately.

### Limits

| Limit | Default |
|-------|---------|
| Request body size | 10 MB |
| Response body size | 10 MB |
| Timeout per request | 30 seconds |
| Concurrent requests per invocation | 5 |
| Total requests per invocation | 20 |

---

## Configuration

### Purpose
Inject configuration and secrets into functions at runtime.

### Interface

```typescript
import { config } from '@ubiq/sdk';

const apiKey = config.get('API_KEY');
const dbUrl = config.get('DATABASE_URL');
```

### Sources (in priority order)
1. Function-level env in `ubiq.toml`
2. Environment-specific overrides (`[env.production]`)
3. System environment variables
4. `.env` file (dev only)
5. Secrets manager (production)

### Secrets
- Secrets are never logged or exposed in error messages
- In production, fetched from a secrets manager (Vault, AWS Secrets Manager)
- In dev, stored in `.env.local` (gitignored)

---

## Logging

### Purpose
Structured logging from functions, captured and routed to OTLP.

### Interface

```typescript
import { log } from '@ubiq/sdk';

log.debug('Processing request', { userId: '123' });
log.info('User created', { userId: '123', name: 'Alice' });
log.warn('Rate limit approaching', { current: 55, max: 60 });
log.error('Failed to send email', { error: err.message });
```

### Behavior
- In dev: printed to terminal with color and timestamp
- In production: emitted as OTLP log records
- Each log entry includes: function name, invocation ID, timestamp, level, message, attributes
- `console.log()` is automatically captured (mapped to `log.info`)

---

## Scheduled Functions (Cron)

### Purpose
Run functions on a schedule.

### Configuration

```toml
[triggers]
"*/5 * * * *" = "functions/cleanup.ts"           # Every 5 minutes
"0 0 * * *" = "functions/daily-report.ts"         # Daily at midnight
"0 */6 * * *" = "functions/sync-data.ts"          # Every 6 hours
```

### Behavior
- In dev: scheduler runs locally (for testing schedules)
- In production: distributed scheduler with leader election (only one node executes)
- No input payload (function receives `{ trigger: "cron", schedule: "*/5 * * * *" }`)
- Execution timeout applies like any other function
- Failed cron executions are logged and retried (configurable)
