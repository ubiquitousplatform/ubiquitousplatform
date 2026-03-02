# SDK & Standard Library

> The developer-facing API that functions use to interact with the platform.

---

## Overview

The SDK (`@ubiq/sdk`) is a lightweight set of functions that WASM guest code calls to access platform services. Internally, these are thin wrappers around host function calls.

For every supported language, there's a PDK (Plugin Development Kit) that provides idiomatic access to these capabilities.

---

## TypeScript / JavaScript SDK

### Installation

No installation needed. `@ubiq/sdk` is auto-available in every function.

```typescript
import { kv, storage, events, http, config, log, trace } from '@ubiq/sdk';
```

### API Reference

#### `kv` — Key-Value Store

```typescript
kv.get(key: string): Promise<string | null>
kv.set(key: string, value: string, opts?: { ttl?: number }): Promise<void>
kv.delete(key: string): Promise<boolean>
kv.list(prefix: string): Promise<string[]>
kv.exists(key: string): Promise<boolean>
kv.setIfNotExists(key: string, value: string, opts?: { ttl?: number }): Promise<boolean>
```

#### `storage` — File/Object Storage

```typescript
storage.read(path: string): Promise<Uint8Array>
storage.readText(path: string): Promise<string>
storage.write(path: string, data: Uint8Array | string): Promise<void>
storage.delete(path: string): Promise<boolean>
storage.list(prefix?: string): Promise<FileInfo[]>
storage.stat(path: string): Promise<FileInfo | null>
storage.exists(path: string): Promise<boolean>

interface FileInfo {
  path: string;
  size: number;
  mime: string;
  modified: string;  // ISO 8601
}
```

#### `events` — Event System

```typescript
events.emit(topic: string, payload: any): Promise<void>
```

#### `http` — Outbound HTTP

```typescript
http.get(url: string, opts?: RequestOpts): Promise<HttpResponse>
http.post(url: string, opts?: RequestOpts): Promise<HttpResponse>
http.put(url: string, opts?: RequestOpts): Promise<HttpResponse>
http.delete(url: string, opts?: RequestOpts): Promise<HttpResponse>
http.request(method: string, url: string, opts?: RequestOpts): Promise<HttpResponse>

interface RequestOpts {
  body?: string | Uint8Array;
  headers?: Record<string, string>;
  timeout?: number;  // ms
}

interface HttpResponse {
  status: number;
  headers: Record<string, string>;
  body: string;
  bodyBytes: Uint8Array;
}
```

#### `config` — Configuration

```typescript
config.get(key: string): string | undefined
config.require(key: string): string  // Throws if missing
```

#### `log` — Structured Logging

```typescript
log.debug(message: string, attrs?: Record<string, any>): void
log.info(message: string, attrs?: Record<string, any>): void
log.warn(message: string, attrs?: Record<string, any>): void
log.error(message: string, attrs?: Record<string, any>): void
```

#### `trace` — Custom Telemetry

```typescript
trace.setAttribute(key: string, value: string | number | boolean): void
trace.addEvent(name: string, attrs?: Record<string, any>): void
```

---

## Rust SDK

```rust
use ubiq_sdk::{kv, storage, events, http, config, log};

#[ubiq::function]
fn create_user(input: CreateUserInput) -> Result<User, UbiqError> {
    log::info!("Creating user: {}", input.email);

    let user = User {
        id: ubiq::uuid(),
        name: input.name,
        email: input.email,
    };

    kv::set(&format!("user:{}", user.id), &serde_json::to_string(&user)?)?;
    events::emit("user.created", &user)?;

    Ok(user)
}
```

---

## Go SDK

```go
package main

import (
    "github.com/ubiquitous/sdk-go"
)

//export create_user
func createUser(input CreateUserInput) (*User, error) {
    ubiq.Log.Info("Creating user", "email", input.Email)

    user := &User{
        ID:    ubiq.UUID(),
        Name:  input.Name,
        Email: input.Email,
    }

    if err := ubiq.KV.Set("user:"+user.ID, user); err != nil {
        return nil, err
    }

    ubiq.Events.Emit("user.created", user)
    return user, nil
}
```

---

## Test SDK (`@ubiq/test`)

```typescript
import { test, expect, describe, beforeEach, afterEach, mock } from '@ubiq/test';

// Test lifecycle
describe(name: string, fn: () => void): void
test(name: string, fn: () => void): void
beforeEach(fn: () => void): void
afterEach(fn: () => void): void

// Assertions
expect(value).toBe(expected)
expect(value).toEqual(expected)        // Deep equality
expect(value).toBeTruthy()
expect(value).toBeFalsy()
expect(value).toBeNull()
expect(value).toBeUndefined()
expect(value).toContain(item)
expect(value).toHaveLength(n)
expect(value).toBeGreaterThan(n)
expect(value).toBeLessThan(n)
expect(value).toMatch(pattern)
expect(fn).toThrow(message?)

// Mocking
mock.kv(overrides?: Partial<KVStore>): MockedKV
mock.storage(overrides?: Partial<Storage>): MockedStorage
mock.events(): MockedEvents
mock.http(handlers: Record<string, MockHandler>): MockedHttp
mock.config(values: Record<string, string>): MockedConfig
```

---

## Implementation Strategy

### How the SDK Works Internally

```
User Function Code
    │
    │ import { kv } from '@ubiq/sdk'
    │ await kv.get('my-key')
    │
    ▼
SDK Wrapper (@ubiq/sdk)
    │
    │ Serialize: { namespace: 'kv', action: 'get', args: ['my-key'] }
    │ Call host function: ubiqDispatch(ptr, len)
    │
    ▼
Host Function (in the runtime)
    │
    │ Deserialize request
    │ Check permissions
    │ Execute against backend (SQLite, Redis, filesystem, etc.)
    │ Serialize response
    │ Write to guest memory
    │
    ▼
SDK Wrapper
    │
    │ Read response from memory
    │ Deserialize result
    │ Return to user code
    │
    ▼
User Function Code
    │
    │ const value = await kv.get('my-key')  // ← resolved
```

The SDK is compiled into the WASM module alongside the user's function code. It's a thin serialization layer, not a runtime.
