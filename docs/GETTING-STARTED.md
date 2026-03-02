# Getting Started with Ubiquitous

> From zero to running function in 30 seconds.

---

## Install

```bash
curl -fsSL https://ubiq.run/install | sh
```

Verify:
```bash
ubiq --version
# Ubiquitous v0.1.0
```

---

## Create Your First Project

```bash
ubiq new hello-app
cd hello-app
```

This creates:
```
hello-app/
├── ubiq.toml
├── functions/
│   ├── hello.ts
│   └── hello.test.ts
└── .gitignore
```

---

## Look at the Code

### `ubiq.toml`
```toml
[project]
name = "hello-app"
version = "0.1.0"

[runtime]
language = "typescript"
```

### `functions/hello.ts`
```typescript
export default function(input: { name?: string }) {
  return {
    message: `Hello, ${input.name || 'World'}!`
  };
}
```

### `functions/hello.test.ts`
```typescript
import { test, expect } from '@ubiq/test';

test('greets by name', () => {
  const result = hello({ name: 'Alice' });
  expect(result.message).toBe('Hello, Alice!');
});

test('default greeting', () => {
  const result = hello({});
  expect(result.message).toBe('Hello, World!');
});
```

---

## Run It

```bash
ubiq dev
```

```
Ubiquitous v0.1.0 — Dev Server

  Compiling functions...
  ✓ hello.ts → hello.wasm (147ms)

  Listening on http://localhost:3000
  Dashboard at http://localhost:3000/_dashboard

  Watching for changes...
```

---

## Call It

```bash
curl http://localhost:3000/hello
# {"message": "Hello, World!"}

curl -X POST http://localhost:3000/hello -d '{"name": "Alice"}'
# {"message": "Hello, Alice!"}
```

---

## Test It

```bash
ubiq test
```

```
  functions/hello.test.ts
    ✓ greets by name (0.4ms)
    ✓ default greeting (0.2ms)

Tests: 2 passed, 0 failed
Time:  0.31s
```

---

## Add Another Function

```bash
ubiq new function users/list
```

Creates:
```
functions/users/
  list.ts
  list.test.ts
```

Edit `functions/users/list.ts`:
```typescript
import { kv } from '@ubiq/sdk';

export async function GET() {
  const keys = await kv.list('user:');
  const users = [];
  for (const key of keys) {
    users.push(JSON.parse(await kv.get(key)));
  }
  return { users };
}

export async function POST(input: { name: string; email: string }) {
  const id = crypto.randomUUID();
  const user = { id, name: input.name, email: input.email };
  await kv.set(`user:${id}`, JSON.stringify(user));
  return user;
}
```

Save — it auto-recompiles:
```
  ↻ users/list.ts changed
  ✓ users/list.ts → list.wasm (95ms, hot-swapped)
```

Test:
```bash
# Create a user
curl -X POST http://localhost:3000/users/list -d '{"name":"Alice","email":"alice@example.com"}'
# {"id":"abc-123","name":"Alice","email":"alice@example.com"}

# List users
curl http://localhost:3000/users/list
# {"users":[{"id":"abc-123","name":"Alice","email":"alice@example.com"}]}
```

---

## Deploy (When Ready)

```bash
ubiq deploy
```

That's it. Your functions are live.

---

## Next Steps

- Read the [Architecture Overview](ARCHITECTURE.md) to understand how it all fits together
- Check [MODULE-MANIFEST.md](MODULE-MANIFEST.md) for all configuration options
- See [MODULE-SERVICES.md](MODULE-SERVICES.md) for KV, storage, events, and more
- Look at [MODULE-PLUGINS.md](MODULE-PLUGINS.md) to extend the platform
- Explore the [LIBRA-MEDIA-APP.md](LIBRA-MEDIA-APP.md) showcase for a real-world example
