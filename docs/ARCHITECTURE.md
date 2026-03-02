# Architecture Overview

> How Ubiquitous fits together, from HTTP request to WASM execution.

---

## System Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         ubiq CLI                                        │
│  new | dev | watch | test | build | deploy | dashboard                  │
└─────────┬───────────────────────────────────┬───────────────────────────┘
          │                                   │
          ▼                                   ▼
┌───────────────────┐              ┌─────────────────────┐
│   Compiler Chain   │              │   Release Manager    │
│  TS/JS → WASM      │              │  Bundle | Version    │
│  Rust → WASM       │              │  2-Phase Commit      │
│  Go → WASM         │              │  Rollback            │
│  Python → WASM     │              └──────────┬──────────┘
└────────┬──────────┘                          │
         │                                     │
         ▼                                     ▼
┌──────────────────────────────────────────────────────────────────┐
│                      Ubiquitous Runtime                          │
│                                                                  │
│  ┌──────────┐  ┌──────────────┐  ┌───────────┐  ┌───────────┐  │
│  │  Router   │→│  Middleware   │→│  Function  │→│  Response  │  │
│  │          │  │  Pipeline    │  │  Pool      │  │  Builder   │  │
│  └──────────┘  └──────────────┘  └─────┬─────┘  └───────────┘  │
│                                        │                         │
│                                        ▼                         │
│                           ┌────────────────────┐                 │
│                           │   WASM Sandbox     │                 │
│                           │                    │                 │
│                           │  ┌──────────────┐  │                 │
│                           │  │ User Function │  │                 │
│                           │  └──────┬───────┘  │                 │
│                           │         │          │                 │
│                           │    Host Functions  │                 │
│                           └─────────┬──────────┘                 │
│                                     │                            │
│         ┌───────────────────────────┼─────────────────────┐      │
│         ▼              ▼            ▼          ▼          ▼      │
│  ┌──────────┐  ┌──────────┐ ┌──────────┐ ┌────────┐ ┌───────┐  │
│  │ KV Store │  │ Storage  │ │  Events  │ │  HTTP  │ │ Logs  │  │
│  └──────────┘  └──────────┘ └──────────┘ └────────┘ └───────┘  │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │                    OTLP Telemetry                        │    │
│  │   Traces | Metrics | Logs → Dashboard / External         │    │
│  └──────────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────────┘
```

---

## Request Lifecycle

### 1. Incoming Request
An HTTP request arrives at the runtime server (local dev or production).

### 2. Routing
The router maps the request to a function based on:
- File-system convention: `functions/get-users.ts` → `GET /get-users`
- Explicit routes in `ubiq.toml`
- Plugin-provided routes

### 3. Middleware Pipeline
Request passes through configured middleware (in order):
- CORS
- Authentication
- Rate limiting
- Request validation
- Custom middleware plugins

Any middleware can short-circuit (return early without calling the function).

### 4. Function Pool Checkout
The runtime checks out a pre-warmed WASM instance from the function pool:
- Pool maintains N pre-initialized instances per function
- Auto-scales between min and max capacity
- If no instance available, one is created on-demand

### 5. WASM Execution
The function executes inside the WASM sandbox:
- Input is serialized (JSON) and passed to the function
- Function can call host functions (KV, storage, HTTP, etc.)
- Each host function call crosses the WASM boundary via IPC
- Resource limits are enforced (CPU time, memory, timeout)

### 6. Response
- Function output is serialized back to HTTP response
- Middleware pipeline runs in reverse (response middleware)
- WASM instance is checked back into the pool
- Telemetry span is completed and exported

---

## Execution Modes

### Development (`ubiq dev`)
- File watcher active, recompiles on save
- Debug-optimized compilation (fast compile, slower execution)
- Console output displayed in terminal
- Dashboard available at `localhost:PORT/dashboard`
- Relaxed resource limits for debugging

### Test (`ubiq test`)
- Functions and tests both run inside WASM
- Identical sandbox as production
- Assertion library injected via host functions
- Results reported in terminal (and optionally JUnit XML)

### Production (`ubiq deploy`)
- Release-optimized compilation (slow compile, fast execution)
- Strict resource limits
- Telemetry exported to configured OTLP backend
- 2-phase commit deployment across cluster nodes

---

## Module Dependency Graph

```
ubiq CLI
  ├── Compiler Chain
  ├── File Watcher
  ├── Release Manager
  └── Runtime
        ├── Router
        ├── Middleware Engine
        ├── Function Pool
        │     └── WASM Engine (Extism.NET → Wasmtime)
        ├── Host Functions
        │     ├── KV Store
        │     ├── File Storage
        │     ├── Event Bus
        │     ├── HTTP Client
        │     ├── Config
        │     └── Logging
        ├── Telemetry (OTLP)
        └── Dashboard (Web UI)
```

---

## Key Design Principles

### 1. Convention Over Configuration
Sensible defaults for everything. A function file is all you need.

### 2. Same Runtime Everywhere
`ubiq dev` and production use the identical WASM engine. No "works on my machine."

### 3. Sandbox by Default
Every function runs in an isolated WASM sandbox. No access to the host system unless explicitly granted.

### 4. Zero Dependencies
Single binary includes everything: runtime, compilers, dev server, dashboard.

### 5. Functions are Files
One file = one function. Directory structure reflects API structure.

### 6. Tests are Siblings
Test files live next to function files and run in the same sandbox.

### 7. Observable by Default
Every invocation produces traces, metrics, and logs without any user configuration.
