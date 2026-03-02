# Runtime Language Decision: C# vs Rust vs Go

> Analysis of implementation language for the Ubiquitous runtime.

---

## Decision Criteria

| Criteria | Weight | Why It Matters |
|----------|--------|----------------|
| Cold start speed | High | Serverless = lots of cold starts |
| Warm invocation latency | High | User-facing request latency |
| Memory per instance | High | Want 100K+ concurrent instances |
| Cross-platform static binary | High | "Zero dependencies" promise |
| WASM runtime ecosystem | High | Quality of Wasmtime/Extism bindings |
| Compile-from-source time | Medium | Developer iteration speed |
| Ecosystem maturity | Medium | Libraries for networking, crypto, etc. |
| Team familiarity | Medium | Existing codebase is C# |

---

## C# / .NET 8

### Strengths
- **Existing codebase**: Two working execution engines already built
- **Aspire**: Best-in-class observability dashboard, OTLP visualization, service discovery
- **Extism.NET + Wasmtime.NET**: Mature, tested bindings (currently in use)
- **High performance**: AOT compilation, sub-ms allocations, System.Text.Json
- **Rich ecosystem**: ASP.NET Core, Entity Framework, SignalR
- **Team familiarity**: Luke knows C# well

### Weaknesses
- **Binary size**: ~80MB+ self-contained binary (with runtime)
- **Not truly static**: Requires .NET runtime (NativeAOT helps but has limitations)
- **NativeAOT limitations**: Reflection-heavy code breaks, Extism SDK may have issues
- **Memory overhead**: ~30MB base memory for runtime
- **Startup time**: ~50-100ms even with NativeAOT (vs ~1ms for Rust)
- **WASM integration**: One level of indirection (managed → native → WASM)

### Cross-Platform Story
- NativeAOT produces native binaries but requires build toolchain per platform
- Self-contained deployment includes .NET runtime (~80MB)
- Not a true "single static binary" without NativeAOT

### Verdict
Best option if Aspire dashboard is a priority and team wants to ship fast. Worst option for binary distribution and memory efficiency.

---

## Rust

### Strengths
- **Wasmtime is Rust-native**: Zero FFI overhead, first-class API, most features
- **Extism SDK**: Excellent Rust support, maintained by same team as Wasmtime
- **Binary size**: 5-15MB static binary with everything
- **Memory efficiency**: ~1-2MB base overhead, WASM instances are the main cost
- **Performance**: Native speed, zero-cost abstractions, no GC pauses
- **Static binary**: Single `musl` binary, truly zero dependencies
- **Cross-compilation**: `cross` tool makes multi-platform builds trivial
- **Cold start**: Sub-millisecond process startup
- **WASI support**: Best-in-class, Wasmtime team drives WASI spec

### Weaknesses
- **Learning curve**: Borrow checker, lifetimes, more complex than Go/C#
- **Compile times**: Debug builds 10-30s, release builds 1-5 minutes
- **Ecosystem gaps**: HTTP server ecosystem (axum/actix) is good but less batteries-included than ASP.NET
- **Team familiarity**: Less experience (though Rust prototype exists)
- **Dashboard UI**: Would need to build or embed a web UI (no Aspire equivalent)

### Cross-Platform Story
- `cargo build --target x86_64-unknown-linux-musl` = static binary
- CI matrix for: linux-x86_64, linux-aarch64, macos-x86_64, macos-aarch64, windows-x86_64
- Single binary, no runtime needed, runs on bare metal

### Verdict
Best option for production runtime: smallest binary, lowest memory, best WASM integration. Higher initial investment.

---

## Go

### Strengths
- **wazero**: Pure-Go WASM runtime, zero CGO dependencies, truly portable
- **extism-go**: Using wazero backend, no native dependencies
- **Static binary**: Single binary, cross-compiles trivially with `GOOS`/`GOARCH`
- **Fast compilation**: `go build` in 1-5 seconds
- **Goroutines**: Excellent concurrency model for request handling
- **Simple language**: Fast onboarding, easy to contribute to
- **Good ecosystem**: net/http, cobra (CLI), viper (config)

### Weaknesses
- **WASM performance**: wazero is ~2-3x slower than Wasmtime for WASM execution
- **Memory**: Go garbage collector + wazero overhead, ~10-15MB per goroutine stack
- **Binary size**: 15-30MB (Go runtime + wazero)
- **WASI support**: wazero implements WASI but lags behind Wasmtime on newest features
- **Extism-Go**: Less mature than Rust/C# SDKs
- **No generics history**: Ecosystem still catching up on generic patterns

### Cross-Platform Story
- `GOOS=linux GOARCH=amd64 go build` = done
- Static binary, no runtime
- CGO_ENABLED=0 (no C dependencies with wazero)

### Verdict
Best option for developer velocity and simplest cross-platform story. Worst option for raw WASM performance.

---

## Benchmark Comparison (Estimated)

| Metric | C# (NativeAOT) | Rust | Go |
|--------|----------------|------|-----|
| Process cold start | ~50ms | ~1ms | ~5ms |
| WASM cold start (compile module) | ~5ms | ~3ms | ~8ms |
| WASM warm invocation | ~50μs | ~20μs | ~100μs |
| Memory (runtime base) | ~30MB | ~2MB | ~10MB |
| Memory (per WASM instance) | ~2MB | ~1MB | ~3MB |
| Max instances (8GB RAM) | ~3,500 | ~7,500 | ~2,500 |
| Binary size (release) | ~80MB | ~10MB | ~20MB |
| Build time (debug) | ~5s | ~15s | ~2s |
| Build time (release) | ~30s | ~120s | ~10s |

*These are estimates based on publicly available benchmarks and the existing codebase's observed performance.*

---

## Recommendation

### Option A: Rust for Everything (Recommended for Production)
- Runtime, CLI, dashboard — all Rust
- Best performance, smallest binary, best WASM integration
- Higher initial investment, but pays off at scale
- Use: axum (HTTP), clap (CLI), Wasmtime (WASM), sled (KV)

### Option B: Hybrid — C# Runtime + Rust CLI (Pragmatic)
- Keep C# runtime (existing code, Aspire dashboard)
- Build CLI in Rust (small binary, fast, ships compilers)
- CLI invokes the C# runtime or includes it
- Downside: two language stacks, more complex build

### Option C: Go for Everything (Fastest to Ship)
- Runtime, CLI, dashboard — all Go
- Fastest development velocity
- wazero means zero native dependencies
- Acceptable performance for MVP
- Risk: may need to rewrite for performance later

### My Recommendation
**Start with Rust** for the core runtime and CLI. The existing C# code has validated the architecture and proven the concept. Now build the "real" version optimized for production:

1. **Phase 0-1**: Port the execution engine to Rust (Wasmtime native + Extism Rust SDK)
2. **Phase 1**: Build CLI in Rust (clap)
3. **Phase 2**: Build services in Rust (KV with sled, storage with filesystem/S3)
4. **Phase 3**: Build dashboard as an embedded web UI (htmx or simple React SPA bundled into the binary)
5. **Keep C# prototypes** as reference implementations and for Aspire-based development dashboard

The ~10MB static binary story is a massive competitive advantage for marketing and adoption.

---

## Alternative: Keep C# and Optimize Later

If shipping speed is the priority over binary size/memory, keep C#:
- Ship faster with existing code
- Use Aspire for observability (huge DX win)
- Accept the 80MB binary and higher memory footprint
- Optimize or rewrite hot paths later if needed
- Consider NativeAOT for the CLI portion

The beauty of the architecture is that the **user-facing API (functions, manifest, CLI commands)** is the same regardless of implementation language. A rewrite later doesn't affect users.
