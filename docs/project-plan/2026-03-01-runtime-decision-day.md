# Runtime Decision Day — March 1, 2026

> One-day deep dive to make three critical architectural decisions with benchmark-backed evidence.

---

## Executive Summary

Today we resolve three blocking questions that everything else hinges on:

1. **Rust or C# for the execution host?** (driven by invocation latency, not binary size)
2. **Wasmtime or Extism?** (and does Extism's abstraction layer cost us meaningful overhead?)
3. **Component Model or not?** (and do we need it, or does a custom host function contract serve us better?)

We'll also prototype compiler output for each target language and test host function round-trips. By end of day, we'll have a decision document backed by real numbers from our own hardware.

---

## Context & Current State

### What Exists

- **C# prototype** (most mature): Two parallel WASM execution engines in `ubiquitous.functions` — one using Extism SDK, one using raw Wasmtime. Object pool, lifecycle state machine, host function IPC, unit tests.
- **Rust prototype**: Non-compiling sketch in `rust/ubiquitous-functions/`. Uses axum + extism crate. Never got past scaffolding.
- **Benchmark project**: `BenchmarkWasmRuntimes` — has compile errors, only partially implemented for Extism, Wasmtime path is stubbed.
- **Test harness**: Working TS→WASM compilation via esbuild + extism-js (QuickJS). Binary IPC envelope protocol for host function calls.
- **Host functions API**: TypeSpec-defined KV store contract. JSON-based dispatch protocol.

### What We Need to Know

The pitch says "sub-millisecond warm starts" and "100K+ concurrent instances." The question isn't which approach is *theoretically* faster — it's whether the gap between Rust-native Wasmtime and C#-via-FFI is large enough to matter at scale, and whether Extism's abstraction adds measurable overhead.

---

## The Three Decisions

### Decision 1: Rust vs C# for Execution Host

**The real question**: Does the C#→native FFI boundary for WASM invocation add enough latency to matter?

| Factor | C# (.NET 8 NativeAOT) | Rust |
|--------|----------------------|------|
| Wasmtime binding | Wasmtime.NET (P/Invoke FFI) | wasmtime crate (native, zero FFI) |
| Extism binding | Extism.NET (wraps C lib via FFI) | extism crate (native Rust) |
| Estimated warm invocation | ~50μs | ~20μs |
| Memory per instance | ~2MB + .NET overhead | ~1MB |
| Process startup | ~50ms (NativeAOT) | ~1ms |
| Host function call overhead | Managed→Native→WASM→Native→Managed | Native→WASM→Native |
| Rich ecosystem | ASP.NET, Garnet, SignalR, HangFire, Aspire | axum, redb, socketioxide, apalis |

**The nuance you raised**: Even if Rust wins on raw WASM invocation speed, C# gives us Garnet (embedded Redis-compatible KV), Aspire (observability dashboard), SignalR (WebSocket hubs), HangFire (job execution), and a rich framework ecosystem. Can we get these in Rust?

**Rust ecosystem reality check:**

| Capability | C# Option | Rust Option | Gap |
|-----------|-----------|-------------|-----|
| Embedded Redis-compatible KV | **Garnet** (Microsoft, battle-tested, full RESP protocol) | **No equivalent.** redb/sled provide KV but no Redis protocol. You'd have to build a RESP layer yourself. | **Large gap** |
| Distributed consensus (Raft) | No dominant .NET library | **openraft** (production, used by Databend) | Rust is stronger here |
| Real-time WebSocket (SignalR-like) | **SignalR** (hub abstractions, auto-transport fallback, typed proxies) | **socketioxide** (Socket.IO server, rooms/namespaces, but different protocol than SignalR) | **Medium gap** — different ecosystem |
| Background jobs (HangFire-like) | **HangFire** (Postgres/SQLite/Redis backends, dashboard, battle-tested) | **apalis** (v1.0.0-rc, Postgres/SQLite/Redis, Tower-based, has dashboard) | Small gap — apalis is close but less mature |
| Observability dashboard | **Aspire Dashboard** (OTLP-native, traces/metrics/logs, zero config) | **Nothing embeddable.** Must spin up Jaeger/Grafana externally. (But Aspire Dashboard itself is a standalone container usable from any language.) | **Large gap** — but solvable by using Aspire Dashboard as a sidecar |
| HTTP framework | **ASP.NET Core** (Kestrel, middleware, DI, identity) | **axum + tower** (production-ready, composable middleware, manual DI) | Small gap — axum is excellent |
| Web framework maturity | 10+ years, LTS, Microsoft backing | axum v0.8, pre-1.0 | Cultural gap more than technical |

### Decision 2: Wasmtime vs Extism

**What Extism gives us over raw Wasmtime:**

1. **Multi-language PDK ecosystem** — JS, Rust, Go, Python, C#, Zig, C, Haskell, AssemblyScript all have PDKs. This is the *primary* value. Without Extism, we'd need to write our own PDK for each language.
2. **Memory management** — Extism manages a separate memory region for plugin I/O, avoiding the complexity of guest `malloc`/`free` coordination.
3. **Host function registration** — Cleaner API than raw Wasmtime host function linking.
4. **Plugin lifecycle** — Load, configure, call, reset, destroy — all managed.
5. **Built-in HTTP** — Plugins can make host-controlled outbound HTTP without WASI networking.
6. **Variables** — Module-scoped persistent state between calls.
7. **XTP Bindgen** — Schema-driven code generation for plugin interfaces.
8. **Fuel/limiting** — Recently added CompiledPlugin with fuel limits.

**What Extism costs us:**

1. **Abstraction penalty** — Extism wraps Wasmtime. There's a layer of indirection. The question is: how much does this cost per invocation? Likely single-digit microseconds — Extism is Rust calling Rust when used natively.
2. **Locked to Extism's choices** — Their memory model, their IPC protocol, their lifecycle. If we want custom behavior, we're fighting the framework.
3. **Dependency on Dylibso** — Extism is maintained by a small company. 38 contributors, last release Nov 2025.
4. **WASI story** — Extism intentionally provides its own alternatives to WASI (HTTP, variables). This conflicts with the Component Model direction.

**What raw Wasmtime gives us:**

1. **Component Model support** — First-class. Wasmtime is the reference implementation.
2. **Full control** — Custom memory layout, module linking, custom IPC.
3. **No third-party dependency** — Wasmtime is Bytecode Alliance, widely backed.
4. **WASI-native** — Direct WASI Preview 1 and Preview 2 support.

**What raw Wasmtime costs us:**

1. **We build everything Extism gives us for free** — PDKs, memory management, host function registration patterns, lifecycle management. That's months of work.
2. **No existing PDK ecosystem** — Each target language needs its own SDK for our custom protocol.

### Decision 3: Component Model or Custom Contract

**What the Component Model gives us:**

1. **Typed interfaces via WIT** — Strong contracts between host and guest. Type-safe across languages.
2. **Composition** — Components can be assembled from sub-components without shared memory.
3. **WASI 0.2** — The standardized system interface uses Component Model. This is the future direction of the entire Wasm ecosystem.
4. **Tooling ecosystem** — `wasm-tools`, `wit-bindgen`, `cargo-component`, `componentize-py`, `jco` (JS).

**What the Component Model costs us:**

1. **Tooling immaturity** — Not all languages have mature component model support:

| Language | Component Model Tooling | Maturity |
|----------|------------------------|----------|
| **Rust** | `cargo-component` + `wit-bindgen` | Production-ready |
| **JavaScript** | `jco` (ComponentizeJS) | Usable, actively developed |
| **Python** | `componentize-py` | Works, but Python-in-WASM is slow |
| **Go** | `wit-bindgen-go` (via TinyGo) | Experimental |
| **C#** | `componentize-dotnet` (preview) | Early stage |

2. **Conceptual complexity** — Worlds, interfaces, packages, WIT syntax. Your instinct is right — the naming conventions are confusing. "Worlds" are essentially "host environment contracts," "interfaces" are "function group contracts," and "packages" are "namespaced interface bundles." The mental model isn't hard once you strip away the naming.
3. **Extra compilation step** — Source → core WASM module → component (via `wasm-tools component new`). Adds build complexity.
4. **Larger binaries** — Component wrapping adds adapter code.
5. **Lock to bytecode alliance cadence** — If they're slow, we're slow.

**What a custom contract gives us:**

1. **Full control** over the host↔guest boundary.
2. **Simpler toolchain** — No WIT, no component wrapping, just plain WASM modules with known imports/exports.
3. **Extism compatibility** — Extism's PDK model is essentially a "custom contract" that's already built and working.
4. **Speed** — We ship when we want, not when the component model ecosystem is ready.

**My recommendation**: **Don't use the Component Model for MVP. Use Extism's model (or a thin custom contract inspired by it).** Here's why:

- The Component Model is the *right* direction long-term, but we don't need it yet.
- Extism's approach is pragmatic: it provides the same sandboxing guarantees via host functions, works with plain WASM modules (not components), and has PDKs for every language we care about *today*.
- We can design our host function interface so that migrating to Component Model later is straightforward (our namespace/function/version envelope already looks like a WIT interface).
- The Component Model's value proposition (composing components from sub-components) isn't relevant to our use case — our functions are leaf nodes that call host functions, not component graphs that wire together.

---

## The Hybrid Architecture Proposal

Based on the ecosystem gaps identified above, here's the architecture I recommend evaluating today:

```
┌──────────────────────────────────────────────────────┐
│                C# Host Process                        │
│                                                      │
│  ASP.NET Core (HTTP + SignalR + Middleware)           │
│  Garnet (embedded KV store)                          │
│  HangFire (job execution)                            │
│  Aspire (OTLP dashboard)                             │
│  Entity Framework / SQLite (metadata store)          │
│                                                      │
│  ┌──────────────────────────────────────────────┐    │
│  │        WASM Execution Bridge                  │    │
│  │  Option A: Extism.NET (C# → Extism C lib)    │    │
│  │  Option B: gRPC/UDS → Rust sidecar            │    │
│  │  Option C: csbindgen FFI → Rust .so/.dylib    │    │
│  └──────────────────────────────────────────────┘    │
│         │                                            │
│         ▼                                            │
│  ┌──────────────────────────────────────────────┐    │
│  │  Rust WASM Executor (if Option B/C)           │    │
│  │  wasmtime or extism crate (native)            │    │
│  │  Function pool, instance management           │    │
│  │  Communicates host calls back to C# host      │    │
│  └──────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────┘
```

**Option A: Pure C# (Extism.NET / Wasmtime.NET)**
- Simplest. One process. No bridge.
- Benchmark to see if the FFI overhead is acceptable.
- If warm invocations are <100μs, this is good enough.

**Option B: C# host + Rust sidecar (gRPC/Unix Domain Socket)**
- C# does HTTP, KV, jobs, observability.
- Rust sidecar owns WASM execution. Communicates via UDS + protobuf.
- Pro: Best of both ecosystems.
- Con: IPC serialization overhead on every host function call. A function that calls kv.get() needs: Guest→Rust→UDS→C#→Garnet→C#→UDS→Rust→Guest. That's a lot of hops.

**Option C: C# host + Rust shared library (FFI via csbindgen)**
- Rust compiled as a `.dylib`/`.so`, loaded directly into C# process.
- Near-zero overhead for WASM invocations (native call into Rust).
- Host function callbacks go Rust→C# via function pointers (csbindgen supports this).
- Pro: Fast, single process.
- Con: Complex build, debugging across FFI boundary is painful.

**Today's benchmark will tell us if Option A is fast enough.** If it is, we skip the complexity of B and C entirely.

---

## Day Plan: 8 Hours, 6 Blocks

### Block 1 (8:00–9:30): Fix & Run C# Benchmarks

**Goal**: Get real BenchmarkDotNet numbers for the existing C# codebase.

**Tasks:**
1. Fix compile errors in `BenchmarkWasmRuntimes/Extism.cs` (dangling dot on line 46)
2. Implement the stubbed Wasmtime.NET benchmark using the existing `WasmRuntime` from `ubiquitous.functions`
3. Benchmark these scenarios:
   - **Extism.NET**: cold start (create plugin), warm call (count_vowels), warm call with host function
   - **Wasmtime.NET**: cold start (compile module), warm call, warm call with host function
   - **Include host function round-trip**: The benchmark must call a host function (like kv.get) so we measure the full managed→native→WASM→native→managed path
4. Run with BenchmarkDotNet (proper statistical rigor: warmup, iterations, percentiles)

**Deliverable**: Baseline numbers table:
```
| Scenario                         | Mean    | P50    | P99    | Allocs |
|----------------------------------|---------|--------|--------|--------|
| Extism cold start                |         |        |        |        |
| Extism warm invocation           |         |        |        |        |
| Extism warm + host fn call       |         |        |        |        |
| Wasmtime cold start              |         |        |        |        |
| Wasmtime warm invocation         |         |        |        |        |
| Wasmtime warm + host fn call     |         |        |        |        |
```

### Block 2 (9:30–11:00): Build & Benchmark Rust Equivalents

**Goal**: Get equivalent Rust numbers using `criterion` (the Rust BenchmarkDotNet).

**Tasks:**
1. Create a new Rust project: `benchmarks/rust-wasm-bench/`
2. Add dependencies: `criterion`, `wasmtime`, `extism`
3. Use the **same WASM modules** as the C# benchmarks (same `.wasm` files)
4. Benchmark identical scenarios:
   - **extism crate**: cold start, warm call, warm call with host function
   - **wasmtime crate**: cold start, warm call, warm call with host function
5. For the host function, implement a simple in-memory KV get/set (HashMap) — same logic as the C# version
6. Run criterion benchmarks

**Deliverable**: Equivalent numbers table for Rust, directly comparable to Block 1.

### Block 3 (11:00–12:00): Compile Target Languages → WASM

**Goal**: For each target language, compile a function that (a) takes JSON input, (b) calls a host function, and (c) returns JSON output.

The function to implement in each language:

```
Input:  { "name": "Alice" }
Host call: kv.set("greeting:" + name, "Hello, " + name)
Host call: value = kv.get("greeting:" + name)
Output: { "message": value }
```

**Language matrix:**

| Language | Compiler | PDK | Notes |
|----------|----------|-----|-------|
| TypeScript | esbuild + extism-js | Extism JS PDK | Already working in test harness |
| Rust | cargo build --target wasm32-wasi | Extism Rust PDK | Straightforward |
| Go | TinyGo --target wasi | Extism Go PDK | TinyGo required |
| Python | extism-py PDK (or componentize-py) | Extism Python PDK | Experimental |
| C# | componentize-dotnet (or Extism .NET PDK) | Extism .NET PDK | Experimental |

**Tasks:**
1. For TypeScript: modify existing test harness function to do KV get/set via host function
2. For Rust: create a small crate with `extism-pdk`, compile to wasm32-unknown-unknown
3. For Go: create a small Go module with extism-pdk-go, compile with TinyGo
4. For Python: attempt with extism-python-pdk; note if it works or fails
5. Collect resulting `.wasm` sizes and note compilation steps

**Deliverable**: A table of `.wasm` files and their sizes, one per language, all implementing the same function.

### Block 4 (12:00–1:00): Lunch + Analyze Morning Results

Compare C# vs Rust benchmark numbers. Key questions:
- Is the Extism.NET FFI overhead >2x the native Rust extism crate?
- Is the Wasmtime.NET FFI overhead >2x the native Rust wasmtime crate?
- Is the host function round-trip the bottleneck, or is it the WASM invocation itself?
- Does Extism add measurable overhead over raw Wasmtime (in either language)?

**Decision checkpoint**: If C# warm invocations with host functions are <100μs, Option A (pure C#) is viable. If they're >200μs, we need to consider the hybrid architecture.

### Block 5 (1:00–3:00): Cross-Language Function Benchmarks

**Goal**: Using the `.wasm` files from Block 3, benchmark them all through both the C# and Rust runtimes.

**Tasks:**
1. Run each language's `.wasm` through the C# benchmark harness
2. Run each language's `.wasm` through the Rust benchmark harness
3. For each, measure: cold start, warm invocation, warm invocation with 2 host function calls (kv.set + kv.get)

**Deliverable**: Full matrix:
```
| Guest Language | Runtime | Cold Start | Warm Call | Warm + Host Fn |
|---------------|---------|------------|-----------|----------------|
| TypeScript    | C#      |            |           |                |
| TypeScript    | Rust    |            |           |                |
| Rust          | C#      |            |           |                |
| Rust          | Rust    |            |           |                |
| Go            | C#      |            |           |                |
| Go            | Rust    |            |           |                |
| Python        | C#      |            |           |                |
| Python        | Rust    |            |           |                |
```

### Block 6 (3:00–5:00): Hybrid Bridge Prototype + Final Decision

**Goal**: If the numbers show C# FFI overhead matters, prototype the Rust shared library approach (Option C). If not, skip this and spend the time on architecture documentation.

**If hybrid needed:**
1. Create a Rust library crate (`cdylib`) that exposes:
   - `fn create_pool(wasm_bytes, pool_size) -> PoolHandle`
   - `fn call_function(pool, input_json) -> output_json`
   - `fn register_host_callback(pool, callback_fn_ptr)`
2. Use `csbindgen` to generate C# P/Invoke bindings
3. Build a minimal C# console app that loads the Rust `.dylib`, registers a host callback, and invokes a WASM function
4. Benchmark the round-trip: C# → Rust FFI → WASM → host callback → C# callback → Rust → WASM → Rust → C#

**If hybrid NOT needed:**
1. Document the "Pure C# with Extism.NET" architecture decision
2. Outline the migration path to Component Model (when ready)
3. Clean up and standardize the C# codebase (pick Extism or Wasmtime, remove the other)
4. Create a `DECISIONS.md` with all benchmark results and rationale

### End of Day (5:00–5:30): Write Decision Document

Produce a `DECISIONS.md` that records:

1. **Runtime Language**: Rust / C# / Hybrid — with benchmark evidence
2. **WASM Engine**: Extism / Wasmtime / Both — with overhead measurements
3. **Component Model**: Yes / No / Later — with rationale
4. **Per-language compiler matrix**: Compiler + PDK + viability for each target language
5. **Architecture diagram**: Final chosen architecture
6. **Next steps**: What to build in the next sprint based on these decisions

---

## Pre-Day Setup Checklist

Run these before the morning starts so we're not waiting on installs:

```bash
# Rust toolchain
rustup update stable
rustup target add wasm32-unknown-unknown wasm32-wasi
cargo install cargo-criterion

# Extism CLI (for testing wasm modules)
curl -fsSL https://get.extism.org/cli | sh

# TinyGo (for Go → WASM)
brew install tinygo

# Extism JS PDK compiler
curl -fsSL https://get.extism.org/js | sh

# Python WASM (optional, may be hard to set up)
pip install extism-cli

# .NET 8 SDK (should already be installed)
dotnet --version

# BenchmarkDotNet (should be in project already)
cd src/BenchmarkWasmRuntimes && dotnet restore

# Criterion (will be added to Rust project)
# (done when we create the project in Block 2)
```

---

## My Pre-Day Recommendations (To Be Validated by Benchmarks)

### Runtime Language: Start with C#, prepare for hybrid if needed

**Rationale**: The C# codebase is 80% of the way to an MVP execution engine. Rewriting in Rust costs months. The FFI overhead of Extism.NET/Wasmtime.NET is likely in the 10-30μs range — acceptable for most workloads. If we later find hot paths where this matters, we can surgically replace the WASM execution layer with a Rust shared library (Option C) without rewriting the entire host.

The bigger argument for C# isn't the runtime — it's the *host function implementations*:
- **Garnet** gives us a production-grade Redis-compatible KV store embeddable in-process. There is no Rust equivalent. We'd have to build one or layer a RESP parser on top of redb.
- **ASP.NET Core** gives us a production HTTP server, middleware pipeline, and model binding. axum is good but less batteries-included.
- **SignalR** gives us WebSocket hubs with transport fallback. socketioxide exists in Rust but uses Socket.IO protocol (different client ecosystem).
- **HangFire** gives us persistent job queues with Postgres/SQLite backends and a monitoring dashboard. apalis is close but pre-1.0.
- **Aspire Dashboard** gives us OTLP visualization with zero setup. Nothing embeddable exists for Rust — though we can *use* Aspire Dashboard as a standalone container with a Rust app too.

The "single binary" story takes a hit with C# (80MB vs 10MB), but binary size was explicitly not your priority — execution speed is.

### WASM Engine: Extism

**Rationale**: Extism's PDK ecosystem is the killer feature. Building our own PDK for 7+ languages is prohibitively expensive. The overhead of Extism over raw Wasmtime is likely <5μs per call (it's a thin Rust wrapper around Wasmtime). We get:
- Host function registration with a clean API
- Memory management for I/O (no manual guest_malloc)
- Module-scoped variables
- Host-controlled HTTP for plugins
- XTP bindgen for schema-driven code generation

We should benchmark Extism vs raw Wasmtime today to confirm the overhead hypothesis. If it's <10μs, the decision is clear.

### Component Model: Not now. Design for compatibility.

**Rationale**: 
- Component Model support varies wildly by language (great for Rust, ok for JS, experimental for Go/Python/C#)
- The extra compilation step (module → component) adds build complexity
- The key benefit (composing components) doesn't apply to our use case (isolated leaf functions)
- WIT interfaces are conceptually equivalent to our existing TypeSpec-defined host function contract
- We can migrate when the ecosystem matures — our JSON-based host function protocol maps cleanly to WIT interfaces

**Compatibility hedge**: Define host functions with the same granularity as WIT interfaces. When Component Model is ready, we can generate WIT from our existing contracts and support both paths.

---

## Success Criteria for Today

By 5:30 PM, we should have:

- [ ] BenchmarkDotNet results for C# (Extism.NET + Wasmtime.NET) with host function calls
- [ ] Criterion results for Rust (extism crate + wasmtime crate) with host function calls
- [ ] A compiled `.wasm` for at least 3 target languages (TS, Rust, Go) implementing the same function
- [ ] Cross-runtime benchmark matrix (each `.wasm` run through both C# and Rust runtimes)
- [ ] A written decision on: Runtime language, WASM engine, Component Model stance
- [ ] If hybrid: a working prototype of Rust dylib loaded from C# with host function callback
- [ ] DECISIONS.md committed to the repo

---

## Appendix: Key References

- [Extism GitHub](https://github.com/extism/extism) — WASM plugin framework, Rust core, multi-language SDKs/PDKs
- [Extism Concepts](https://extism.org/docs/category/concepts) — Plug-in system, host functions, PDKs, memory model
- [Wasmtime](https://github.com/bytecodealliance/wasmtime) — Reference WASM runtime by Bytecode Alliance
- [Component Model Book](https://component-model.bytecodealliance.org) — WIT, interfaces, worlds, packages
- [Microsoft Garnet](https://github.com/microsoft/Garnet) — Redis-compatible cache-store in C#, MIT licensed
- [ASP.NET Core SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction) — Real-time WebSocket hub abstraction
- [BenchmarkDotNet](https://benchmarkdotnet.org/) — .NET micro-benchmarking framework
- [Criterion.rs](https://github.com/bheisler/criterion.rs) — Rust micro-benchmarking framework
- [csbindgen](https://github.com/Cysharp/csbindgen) — Auto-generate C# DllImport from Rust extern fns

### Rust Ecosystem Libraries (Evaluated)

| Need | Library | Status |
|------|---------|--------|
| Embedded KV | `redb` | Stable, ACID, pure Rust |
| Embedded KV | `rust-rocksdb` | Battle-tested via TiKV |
| Raft consensus | `openraft` | Production, used by Databend |
| WebSocket hubs | `socketioxide` | v0.18, Socket.IO protocol |
| Background jobs | `apalis` | v1.0.0-rc, multi-backend |
| HTTP framework | `axum` + `tower` | v0.8, de-facto standard |
| Observability | `tracing` + `opentelemetry-rust` | Stable SDK, no embeddable dashboard |
| Rust↔C# FFI | `csbindgen` | Production, by Cysharp |
