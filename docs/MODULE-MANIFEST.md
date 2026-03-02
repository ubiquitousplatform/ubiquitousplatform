# Function Manifest & Project Convention

> How Ubiquitous projects are structured and configured.

---

## Project Structure

```
myapp/
├── ubiq.toml              # Project manifest
├── functions/
│   ├── hello.ts           # Function: GET /hello
│   ├── hello.test.ts      # Tests for hello
│   ├── users/
│   │   ├── get.ts         # Function: GET /users
│   │   ├── get.test.ts
│   │   ├── create.ts      # Function: POST /users
│   │   └── create.test.ts
│   └── webhooks/
│       └── stripe.ts      # Function: POST /webhooks/stripe
├── plugins/
│   └── auth.ts            # Local plugin
├── shared/
│   └── utils.ts           # Shared code (bundled into functions that import it)
└── .ubiq/
    ├── cache/             # Compiled WASM cache
    └── toolchain/         # Downloaded compilers
```

---

## Manifest: `ubiq.toml`

### Minimal Example

```toml
[project]
name = "myapp"
version = "0.1.0"

[runtime]
language = "typescript"
```

That's it. Everything else has sensible defaults.

### Full Example

```toml
[project]
name = "myapp"
version = "1.2.0"
description = "My serverless application"
authors = ["Luke P <luke@example.com>"]

[runtime]
language = "typescript"       # or "javascript", "rust", "go", "python"
runtime_version = "1.0"       # Ubiquitous runtime version
entry_dir = "functions"       # Where functions live (default: "functions")

[dev]
port = 3000                   # Dev server port
hot_reload = true             # File watcher enabled (default: true)
open_browser = false          # Open browser on startup
dashboard = true              # Enable dev dashboard

[build]
debug_optimize = "speed"      # "speed" or "size" for debug builds
release_optimize = "size"     # "speed" or "size" for release builds

[limits]
timeout_ms = 30000            # Max execution time per invocation
memory_mb = 64                # Max memory per instance
max_request_body_kb = 1024    # Max inbound request size
max_response_body_kb = 10240  # Max response size

# Per-function overrides
[functions.heavy-computation]
timeout_ms = 120000
memory_mb = 256

[functions.hello]
route = "GET /api/hello"      # Override convention-based routing

[functions.create-user]
route = "POST /api/users"
permissions = ["kv:write", "events:emit:user.created"]

# Middleware pipeline (applied in order)
[middleware]
global = ["@ubiq/cors", "@ubiq/rate-limit"]

[middleware.config."@ubiq/cors"]
origins = ["https://myapp.com"]

[middleware.config."@ubiq/rate-limit"]
requests_per_minute = 60

# Environment variables (dev)
[env]
API_KEY = "dev-key-123"
DATABASE_URL = "sqlite://local.db"

# Environment overrides
[env.staging]
API_KEY = "${STAGING_API_KEY}"

[env.production]
API_KEY = "${PROD_API_KEY}"

# Deployment targets
[deploy.production]
nodes = ["node1.example.com", "node2.example.com"]
strategy = "blue-green"       # "blue-green", "canary", "rolling"

[deploy.staging]
nodes = ["staging.example.com"]
strategy = "rolling"

# Event subscriptions
[triggers]
"user.created" = "functions/on-user-created.ts"
"*/5 * * * *" = "functions/cleanup-cron.ts"
```

---

## Routing Convention

### File-System Routing (Default)

| File Path | HTTP Route | Method |
|-----------|-----------|--------|
| `functions/hello.ts` | `GET /hello` | GET (default) |
| `functions/users/list.ts` | `GET /users/list` | GET |
| `functions/users/create.ts` | `POST /users/create` | POST |
| `functions/health.ts` | `GET /health` | GET |

### Method Detection

By default, functions are `GET`. To specify the method:

**Option A: Export name convention**
```typescript
// functions/users.ts
export function GET(input) { /* list users */ }
export function POST(input) { /* create user */ }
```

**Option B: Manifest override**
```toml
[functions.users]
route = "POST /api/users"
```

### Route Parameters

```
functions/users/[id].ts → GET /users/:id
functions/posts/[slug]/comments.ts → GET /posts/:slug/comments
```

---

## Function File Convention

### Minimal Function (TypeScript)

```typescript
// functions/hello.ts
export default function(input: any) {
  return { message: `Hello, ${input.name || 'World'}!` };
}
```

### Full-Featured Function

```typescript
// functions/create-user.ts
import { kv, events, log } from '@ubiq/sdk';

interface CreateUserInput {
  name: string;
  email: string;
}

interface User {
  id: string;
  name: string;
  email: string;
  createdAt: number;
}

export async function POST(input: CreateUserInput): Promise<User> {
  log.info('Creating user', { email: input.email });

  const user: User = {
    id: crypto.randomUUID(),
    name: input.name,
    email: input.email,
    createdAt: Date.now(),
  };

  await kv.set(`user:${user.id}`, JSON.stringify(user));
  await events.emit('user.created', user);

  return user;
}
```

---

## Test File Convention

### Test Placement
Tests live next to functions:
```
functions/
  hello.ts
  hello.test.ts      ← tests for hello.ts
```

### Test File Format

```typescript
// functions/hello.test.ts
import { test, expect } from '@ubiq/test';

test('returns greeting with name', () => {
  const result = hello({ name: 'Alice' });
  expect(result.message).toBe('Hello, Alice!');
});

test('returns default greeting without name', () => {
  const result = hello({});
  expect(result.message).toBe('Hello, World!');
});

test('handles empty input', () => {
  const result = hello(null);
  expect(result.message).toBe('Hello, World!');
});
```

### Test Execution
Tests run **inside the WASM sandbox**, guaranteeing identical behavior to production.

---

## Monorepo Support

For projects with multiple independent services:

```
my-platform/
├── ubiq.toml              # Root manifest
├── services/
│   ├── api/
│   │   ├── ubiq.toml      # Service manifest
│   │   └── functions/
│   ├── webhooks/
│   │   ├── ubiq.toml
│   │   └── functions/
│   └── cron-jobs/
│       ├── ubiq.toml
│       └── functions/
└── shared/
    └── utils.ts
```

Root `ubiq.toml`:
```toml
[workspace]
members = ["services/*"]

[shared]
paths = ["shared"]
```

`ubiq dev` starts all services. `ubiq deploy` deploys all services as a single atomic bundle.

---

## Version Tracking

### Debug Builds
- Version is computed from source file content hash
- No explicit version bump needed
- Hash changes on any code change → automatic recompile

### Release Builds
- Version from `ubiq.toml` `[project].version`
- Semantic versioning enforced
- `ubiq release patch|minor|major` bumps version and creates bundle
- Bundle includes: version, function hashes, compiled WASM, manifest snapshot
