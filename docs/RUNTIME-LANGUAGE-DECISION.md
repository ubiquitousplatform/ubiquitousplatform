# Runtime Language Decision: C# with Extism

> Decision record for the Ubiquitous execution host implementation language and WASM framework.
> **Status: DECIDED** — March 1, 2026 (validated by benchmark-backed evidence from the wasm-runtime POC)

---

## Decision

**C# (.NET 8) with Extism.NET as the WASM execution framework.**

This decision was validated on March 1, 2026 by running quantified benchmarks across C# and Rust runtimes
using both Extism and raw Wasmtime. See [2026-03-01-runtime-decision-day.md](project-plan/2026-03-01-runtime-decision-day.md)
for the full analysis, benchmark data, and decision rationale.

---

## Decision Criteria (with Outcomes)

| Criteria | Weight | Outcome |
|----------|--------|---------|
| Warm invocation latency | High | C# Extism.NET warm calls within acceptable range (<100μs) |
| Host function ecosystem | High | **C# wins decisively** — Garnet, Aspire, SignalR, HangFire |
| Multi-language PDK support | High | **Extism wins** — JS, Rust, Go, Python, C#, Zig, Haskell, C, AssemblyScript |
| Cold start speed | Medium | Acceptable at current scale; revisit at 100K+ instances |
| Cross-platform binary | Medium | .NET 8 self-contained publish; NativeAOT for CLI tooling |
| Compile-from-source time | Medium | <10s build; accepted over Rust compile times |
| Team familiarity | Medium | Existing codebase is C# |

---

## Why C#

### Host Function Ecosystem — The Deciding Factor

The C# ecosystem provides capabilities critical to Ubiquitous that have no viable Rust equivalent today:

| Capability | C# Option | Rust Equivalent | Assessment |
|-----------|-----------|----------------|------------|
| Embedded Redis-compatible KV | **Garnet** (Microsoft, battle-tested, full RESP protocol) | None — redb/sled provide KV but no Redis protocol | **No Rust equivalent** |
| Observability dashboard | **Aspire Dashboard** (OTLP-native, zero config) | Jaeger/Grafana as external containers only | **C# wins** |
| Real-time WebSocket | **SignalR** (hub abstractions, typed proxies) | socketioxide (different protocol, different client ecosystem) | C# is stronger |
| Background jobs | **HangFire** (production-grade, battle-tested) | apalis (v1.0.0-rc, close but pre-1.0) | C# is stronger |
| HTTP framework | **ASP.NET Core / Kestrel** (10+ years, LTS, Microsoft-backed) | axum + tower (excellent but pre-1.0 era) | C# is stronger |
| Distributed consensus | No dominant .NET library | openraft (production, used by Databend) | Rust is stronger here |

Garnet alone makes C# the correct choice: an in-process Redis RESP-compatible KV store that every Extism PDK
language can access through a standard KV host function. Building this in Rust would take months.

### Extism FFI Overhead — Not a Problem in Practice

Benchmarks showed that the managed→native FFI overhead for Extism.NET over the raw Rust extism crate is in the
single-digit microsecond range per call. For warm function invocations with host function round-trips, C# remains
well within the <100μs target.

The extra level of indirection (managed → P/Invoke → Extism C lib → Wasmtime → WASM) adds negligible overhead
relative to the actual work done by WASM functions.

### Existing Codebase

The C# codebase already has:
- Working Extism-based execution engine with object pool and auto-scaling
- ASP.NET Core host with request routing
- Function lifecycle state machine
- Unit tests and test harness

Rebuilding this in Rust would represent months of lost progress with no measurable user-facing benefit at current scale.

---

## Why Extism (not raw Wasmtime)

Extism provides a complete plugin execution framework built on top of Wasmtime:

| Feature | Extism | Raw Wasmtime |
|---------|--------|-------------|
| Multi-language PDK ecosystem | **9+ languages, maintained by Dylibso** | Build your own for each language |
| Memory management (I/O) | **Handled automatically** | Manual guest_malloc/free coordination |
| Host function registration | **Clean, idiomatic API** | Lower-level, manual setup per import |
| Plugin lifecycle | **Built-in** (load, configure, call, reset, destroy) | Build your own |
| Module-scoped variables | **Built-in** | Build your own |
| Performance vs raw Wasmtime | **<5μs overhead per call** | Baseline |
| XTP Bindgen | **Schema-driven code gen for interfaces** | N/A |

The PDK ecosystem is the critical feature. Building idiomatic SDKs for TypeScript, Rust, Go, Python, C#, Zig, and
AssemblyScript against a custom protocol would take 6–12 months. Extism provides this today.

**Extism still uses Wasmtime internally** — we get the same execution performance with a dramatically better
developer experience for plugin authors.

---

## Architecture

```
┌──────────────────────────────────────────────────────┐
│                C# Host Process                        │
│                                                      │
│  ASP.NET Core (HTTP + middleware pipeline)           │
│  Garnet (embedded Redis-compatible KV store)         │
│  HangFire (background job execution)                 │
│  .NET Aspire (OTLP dashboard, service discovery)    │
│  Entity Framework / SQLite (metadata)                │
│                                                      │
│  ┌──────────────────────────────────────────────┐    │
│  │        Extism.NET WASM Execution Layer        │    │
│  │  Extism.NET SDK → Extism C lib → Wasmtime     │    │
│  │  Function pool, instance management           │    │
│  │  Host function registration                   │    │
│  │  Plugin lifecycle (load/call/reset/destroy)   │    │
│  └──────────────────────────────────────────────┘    │
│                │                                     │
│                ▼                                     │
│  ┌──────────────────────────────────────────────┐    │
│  │  WASM Guest (Extism PDK)                      │    │
│  │  Any of: TypeScript, Rust, Go, Python, C#,   │    │
│  │          Zig, AssemblyScript, Haskell, C      │    │
│  └──────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────┘
```

---

## What Was Evaluated and Rejected

### Rust Host Runtime

**Rejected** because:
- No Garnet equivalent (no in-process Redis-compatible KV store)
- No Aspire Dashboard equivalent (embeddable OTLP UI)
- No SignalR equivalent (typed WebSocket hub framework)
- Months of work to rebuild what the C# codebase already provides
- Rust prototype was non-compiling scaffolding; C# prototype is near MVP-ready

The Rust `wasmtime` and `extism` crates are excellent and yielded better raw benchmark numbers, but the
*host function implementations* — Garnet, Aspire, SignalR — are the competitive differentiators, not WASM
invocation speed.

### Raw Wasmtime via C# (Wasmtime.NET)

**Rejected** in favor of Extism because:
- No PDK ecosystem — each target language requires a custom SDK
- Manual guest memory management (guest_malloc/free wrangling)
- Manual host function registration boilerplate
- Plugin lifecycle must be built from scratch
- Extism wraps Wasmtime with <5μs overhead per call — not a meaningful difference in practice

### Go Runtime (wazero)

**Not pursued** because:
- wazero is ~2–3× slower than Wasmtime for WASM execution
- Would require building the same host function infrastructure from scratch
- No Garnet, Aspire, or SignalR equivalent
- Not competitive with C# on any dimension that matters for Ubiquitous

### WebAssembly Component Model

**Deferred** — not used for MVP because:
- Language support is uneven (good for Rust, experimental for Go/Python/C#)
- Extra compilation step (module → component via wasm-tools)
- Extism's model is pragmatic, already working with all target languages today
- Can be adopted incrementally — our host function namespace/version scheme maps cleanly to WIT interfaces

---

## Revisit Criteria

This decision should be revisited if:

1. **Warm invocations exceed 500μs under load** — at that point, a Rust WASM sidecar via gRPC/Unix Domain Socket should be prototyped
2. **Binary size becomes a distribution blocker** — NativeAOT with tree-shaking is the first mitigation; Rust CLI is a secondary option
3. **Garnet gains a Rust equivalent** — an in-process Redis-compatible KV store in Rust would change the calculus
4. **Component Model reaches broad language parity** — at that point, WIT-defined host function interfaces are worth evaluating

---

## References

- [Benchmark day analysis](project-plan/2026-03-01-runtime-decision-day.md) — Full POC benchmarks and decision rationale
- [Microsoft Garnet](https://github.com/microsoft/Garnet) — Embedded Redis-compatible KV store in C#
- [Extism](https://extism.org) — Multi-language WASM plugin framework
- [Extism.NET](https://github.com/extism/dotnet-sdk) — C# host SDK for Extism
- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) — OTLP dashboard and cloud-native dev tooling
- [Wasmtime](https://wasmtime.dev) — The underlying WASM runtime used by Extism
