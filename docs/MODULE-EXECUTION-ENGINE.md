# WASM Execution Engine

> The core of Ubiquitous: loading, executing, and managing WASM sandboxes.

---

## Overview

The execution engine is responsible for:
1. Compiling user code to WASM
2. Loading WASM modules into sandboxed runtimes
3. Managing a pool of pre-warmed instances
4. Routing host function calls from guest to platform services
5. Enforcing resource limits (CPU, memory, timeout)

---

## Current State

**Extism SDK (`FunctionExecutor`)** is the single execution engine:
- Higher-level plugin lifecycle state machine
- Built-in host function registration
- Plugin management abstraction
- Multi-language PDK support (TypeScript/JS, Rust, Go, Python, C#, C, Haskell, Zig, AssemblyScript)
- Memory management (input/output) handled by the framework
- Module-scoped variable storage between calls
- Extism wraps Wasmtime internally — same execution performance, dramatically better DX

Decision rationale: [RUNTIME-LANGUAGE-DECISION.md](RUNTIME-LANGUAGE-DECISION.md)

---

## Function Pool

### Purpose
Pre-warm WASM instances to avoid cold start latency on every request.

### Design
```
FunctionPool
  │
  ├── functions: Map<FunctionName, PoolEntry>
  │     │
  │     └── PoolEntry
  │           ├── available: Queue<WasmInstance>
  │           ├── checked_out: Map<InstanceId, WasmInstance>
  │           ├── config: CapacityConfig
  │           └── compiled_module: CompiledModule (shared)
  │
  └── scaling_loop (background)
        └── Every 1s: adjust pool sizes based on demand
```

### Checkout/Checkin Lifecycle
```
1. Request arrives
2. pool.checkout("my-function") → WasmInstance
3. instance.call("handler", input) → output
4. pool.checkin(instance) → instance reset and returned to pool
```

### Auto-Scaling
- **Min capacity**: Always keep N instances warm (configurable, default 2)
- **Max capacity**: Hard ceiling (configurable, default 1000)
- **Overprovision**: Keep 10% more instances than current demand
- **Scale down**: If idle for >30s, reduce toward min capacity

### Instance Reset
On checkin, the instance is reset:
- WASM linear memory is reset to initial state
- KV variable state is cleared
- File handles are closed
- Environment re-injected
- If reset fails, instance is destroyed and a new one created

---

## Host Function IPC Protocol

Guest WASM functions communicate with the host via host functions. The protocol:

### Extism Host Function Protocol
```
Guest calls: Host.getFunctions() → registered host functions
Guest reads input: Host.inputString() / Host.inputBytes()
Guest writes output: Host.outputString(data) / Host.outputBytes(data)
Guest calls host function: hostFnName(inputPtr, inputLen) → response
```

Host functions are registered on the C# side using `HostFunction` objects with the Extism.NET SDK.
The guest invokes them via the Extism PDK for whichever language it is written in.
Serialization between host and guest uses MessagePack (via the Ubiquitous SDK shim — see SDK versioning docs).

### Planned Host Functions

| Namespace | Function | Description |
|-----------|----------|-------------|
| `kv` | `get(key)` | Read from KV store |
| `kv` | `set(key, value)` | Write to KV store |
| `kv` | `delete(key)` | Delete from KV store |
| `kv` | `list(prefix)` | List keys by prefix |
| `storage` | `read(path)` | Read file from sandboxed storage |
| `storage` | `write(path, data)` | Write file |
| `storage` | `delete(path)` | Delete file |
| `storage` | `list(prefix)` | List files |
| `http` | `request(method, url, body, headers)` | Make outbound HTTP request |
| `events` | `emit(topic, payload)` | Publish event |
| `config` | `get(key)` | Read config value |
| `log` | `log(level, message)` | Structured logging |
| `log` | `debug(message)` | Debug log |

---

## Resource Limits

Every WASM execution is bounded:

| Resource | Default | Configurable |
|----------|---------|-------------|
| **CPU time** | 5 seconds | Per-function in manifest |
| **Memory** | 64 MB | Per-function in manifest |
| **Timeout** | 30 seconds | Per-function in manifest |
| **Request body size** | 1 MB | Global config |
| **Response body size** | 10 MB | Global config |
| **Outbound HTTP size** | 10 MB | Per-function |
| **KV value size** | 1 MB | Global config |
| **File storage quota** | 100 MB | Per-function |

### Enforcement
- **CPU time**: Extism `CompiledPlugin` fuel limits (backed by Wasmtime's epoch/fuel mechanism)
- **Memory**: WASM linear memory max pages configured on the Extism manifest
- **Timeout**: Host-side timer via `CancellationToken`, kills execution on expiry
- **I/O sizes**: Validated in host functions before processing

---

## Compilation Pipeline

### JavaScript / TypeScript

```
source.ts
  → esbuild (bundle, CJS, es2020)
  → output.js
  → extism-js (compile with QuickJS engine)
  → output.wasm
```

**Debug mode**: Skip optimizations, target compile speed (<500ms).
**Release mode**: Full optimizations, wasm-opt pass.

### Rust

```
source.rs
  → cargo build --target wasm32-wasi --release
  → output.wasm
  → wasm-opt (optional)
```

### Go

```
source.go
  → tinygo build -target wasi -o output.wasm
  → wasm-opt (optional)
```

### Python

```
source.py
  → componentize-py (or Extism Python PDK)
  → output.wasm
```

---

## Module Caching

Compiled WASM modules are cached:
- **Key**: SHA-256 hash of source files
- **Location**: `.ubiq/cache/` in project directory
- **Invalidation**: Source file hash changes
- **Sharing**: Compiled Wasmtime `Module` is shared across all pool instances (safe because `Module` is immutable; each instance gets its own `Store`)

---

## Security Model

### WASM Sandbox Guarantees
- No access to host filesystem (unless explicitly permitted via WASI)
- No access to host network (unless via `http` host function with URL allowlist)
- No access to host memory (WASM linear memory is isolated)
- No access to other functions' state
- Deterministic execution (no randomness unless via host function)

### Permission Declaration
Functions declare required permissions in `ubiq.toml`:

```toml
[functions.my-function]
permissions = [
  "kv:read",
  "kv:write",
  "storage:read:images/*",
  "http:get:api.example.com",
  "http:post:api.example.com/webhook"
]
```

Undeclared host function calls are denied at runtime.
