# Ubiquitous Platform — Project Plan

> A phased plan to take Ubiquitous from prototype to startup-ready product.

---

## Vision Statement

Ubiquitous is a **serverless framework and runtime** that makes building, testing, and deploying sandboxed applications as simple as `ubiq dev`. Write functions in any language, test them locally, deploy them atomically — with zero system dependencies.

---

## Current State Assessment

### What Exists Today
- **Extism-based execution engine** (single, chosen) with object pool, checkout/checkin, auto-scaling (10–1000 instances)
- ASP.NET Core API host with routing
- TypeScript/JavaScript → WASM compilation pipeline (esbuild → extism-js)
- Test harness executing inside WASM
- Lifecycle state machine for function execution
- Performance benchmarks (C# Extism.NET validated against Rust baselines)
- Bruno API collection for testing
- Host functions API defined in TypeSpec (KV store)

### What Needs Work
- No CLI tool
- No file watcher / recompiler
- No release/bundle system
- No distributed consensus / 2-phase commits
- Supporting modules (auth, events, storage, config, etc.) are empty stubs
- No plugin system
- No OTLP integration
- Two execution engines need to be unified
- No manifest/config format for functions
- No deployment pipeline

---

## Phase 0: Foundation & Decision Making ~~(Weeks 1–2)~~ ✅ COMPLETE

### Goal
Lock in key architectural decisions before building.

### ~~0.1 — Runtime Language Decision~~ ✅ DECIDED: C# with Extism.NET
- [x] Benchmarked C# (Extism.NET) vs Rust (extism crate + wasmtime crate)
- [x] Measured warm invocation latency and host function round-trip overhead
- [x] Evaluated host function ecosystem (Garnet, Aspire, SignalR, HangFire)
- [x] **Decision**: C# — FFI overhead acceptable; host ecosystem (Garnet, Aspire) has no Rust equivalent
- [x] **Deliverable**: [RUNTIME-LANGUAGE-DECISION.md](RUNTIME-LANGUAGE-DECISION.md)

### ~~0.2 — WASM Execution Engine Unification~~ ✅ DECIDED: Extism SDK only
- [x] Extism chosen: multi-language PDK, managed lifecycle, <5μs overhead over raw Wasmtime
- [x] Wasmtime-direct path removed from codebase
- [x] **Deliverable**: Single Extism execution path; [MODULE-EXECUTION-ENGINE.md](MODULE-EXECUTION-ENGINE.md) updated

### 0.3 — Function Manifest Format
- [ ] Design the `ubiq.toml` / `ubiq.yaml` manifest format
- [ ] Fields: `name`, `version`, `language`, `runtime_version`, `entry_point`, `permissions`, `triggers`
- [ ] Monorepo support: root manifest + per-function manifests
- [ ] **Deliverable**: Manifest spec document with examples

### 0.4 — Project Directory Convention
- [ ] Define canonical directory structure for a Ubiquitous project
- [ ] Convention: `functions/`, `tests/`, `plugins/`, `ubiq.toml`
- [ ] **Deliverable**: Directory convention document

---

## Phase 1: Core Developer Experience (Weeks 3–8)

### Goal
A developer can create, write, test, and run a function locally with a single CLI. This is the **make-or-break phase** — if this doesn't feel magical, nothing else matters.

### 1.1 — CLI Tool (`ubiq`)
- [ ] Scaffold CLI (Rust with `clap`, or Go with `cobra` — matches runtime language decision)
- [ ] Commands:
  - `ubiq new <project-name>` — scaffold a new project
  - `ubiq new function <name>` — scaffold a new function with test
  - `ubiq dev` / `ubiq watch` — start dev server with file watcher
  - `ubiq test` — run all tests
  - `ubiq build` — compile all functions to WASM
  - `ubiq run <function> [input]` — invoke a single function
  - `ubiq deploy` — push to production (later phases)
- [ ] Single static binary, no dependencies
- [ ] **Target**: `ubiq new myapp && cd myapp && ubiq dev` in under 5 seconds

### 1.2 — Compiler Pipeline
- [ ] JavaScript/TypeScript → WASM (via QuickJS/Javy or Extism JS PDK)
- [ ] Rust → WASM (via `cargo build --target wasm32-wasi`)
- [ ] Go → WASM (via TinyGo `--target wasi`)
- [ ] Python → WASM (via componentize-py or Extism Python PDK)
- [ ] Debug mode: optimize for compilation speed (<1 second)
- [ ] Release mode: optimize for runtime performance
- [ ] **All compilers bundled in the binary or downloaded on first use**

### 1.3 — File Watcher & Hot Reload
- [ ] Watch function source files for changes
- [ ] On change: recompile only the changed function (<1 second)
- [ ] Hot-swap the WASM module in the runtime pool (no server restart)
- [ ] Hash-based change detection (avoid unnecessary recompiles)
- [ ] Display compilation results in terminal with clear success/error output
- [ ] **Target**: Save file → function reloaded in <1 second

### 1.4 — Local Runtime Server
- [ ] HTTP server that routes requests to WASM functions
- [ ] Convention-based routing: `functions/get-users.ts` → `GET /get-users`
- [ ] Or explicit route manifest in `ubiq.toml`
- [ ] Request/response serialization (JSON by default)
- [ ] Resource limits enforced even in dev (CPU, memory, timeout)
- [ ] Function pool with checkout/checkin (reuse existing pattern)
- [ ] Console output from functions displayed in dev server terminal

### 1.5 — Test Harness
- [ ] Test files live next to functions: `functions/get-users.test.ts`
- [ ] Tests execute inside WASM (same sandbox as production)
- [ ] Built-in assertion library (minimal, like `expect()`)
- [ ] `ubiq test` runs all tests, `ubiq test <function>` runs specific
- [ ] Test output: passed/failed count, execution time, output
- [ ] Watch mode: `ubiq test --watch` re-runs on file changes
- [ ] **Key insight**: Tests run in the same WASM runtime = identical behavior to production

### 1.6 — Logging & Console Output
- [ ] `console.log()` / `println!()` captured and displayed in dev terminal
- [ ] Structured logging support (JSON output)
- [ ] Log levels: debug, info, warn, error
- [ ] Each function invocation gets a correlation ID
- [ ] **Deliverable**: Logging host functions in the stdlib

---

## Phase 2: Platform Services (Weeks 9–14)

### Goal
Built-in services that functions can use: KV store, file storage, events, config.

### 2.1 — Key-Value Store
- [ ] Durable KV store accessible from functions via host functions
- [ ] Interface: `kv.get(key)`, `kv.set(key, value)`, `kv.delete(key)`, `kv.list(prefix)`
- [ ] Local backend: embedded store (SQLite, Garnet if C#, or sled if Rust)
- [ ] Production backend: pluggable (Redis, DynamoDB, etc.)
- [ ] Namespaced per function by default, cross-function access opt-in
- [ ] Size limits per key and per function namespace

### 2.2 — File/Object Storage
- [ ] Sandboxed filesystem per function
- [ ] Interface: `storage.read(path)`, `storage.write(path, data)`, `storage.list(prefix)`, `storage.delete(path)`
- [ ] Local backend: filesystem directory per function
- [ ] Production backend: S3-compatible
- [ ] Size limits and quota enforcement
- [ ] MIME type detection

### 2.3 — Configuration System
- [ ] Environment variables injected per function
- [ ] Secrets management (encrypted at rest, injected at runtime)
- [ ] Config files: `ubiq.toml` for static config, env-specific overrides
- [ ] `config.get(key)` host function for runtime access

### 2.4 — Event System
- [ ] Functions can emit events: `events.emit(topic, payload)`
- [ ] Functions can subscribe to events: `triggers: [{ event: "user.created" }]`
- [ ] Local: in-process pub/sub
- [ ] Production: pluggable (NATS, Kafka, SQS)
- [ ] Dead letter queue for failed handlers

### 2.5 — HTTP Client (Outbound)
- [ ] Functions can make HTTP requests: `http.get(url)`, `http.post(url, body)`
- [ ] Permission-gated: function must declare allowed URLs in manifest
- [ ] Timeout enforcement
- [ ] Response size limits
- [ ] Retry policy support

### 2.6 — Scheduled Functions (Cron)
- [ ] Trigger functions on a schedule: `triggers: [{ cron: "*/5 * * * *" }]`
- [ ] Local: in-process scheduler
- [ ] Production: distributed scheduler with leader election

---

## Phase 3: Observability & Debugging (Weeks 15–18)

### Goal
Full visibility into function execution with built-in telemetry.

### 3.1 — OTLP Integration
- [ ] OpenTelemetry traces, metrics, and logs emitted for every function invocation
- [ ] Auto-instrumented: no user code needed
- [ ] Spans: HTTP request → routing → pool checkout → WASM execution → host function calls → response
- [ ] Metrics: invocation count, duration histogram, error rate, pool utilization
- [ ] Export to any OTLP-compatible backend (Jaeger, Grafana, Datadog)

### 3.2 — Built-in Dashboard
- [ ] Web UI for development (like Aspire dashboard)
- [ ] Real-time function invocation log
- [ ] Execution traces with timing breakdown
- [ ] Resource usage per function (memory, CPU time)
- [ ] Error details with stack traces
- [ ] `ubiq dashboard` or auto-open on `ubiq dev`

### 3.3 — Error Handling & Reporting
- [ ] Structured error responses from functions
- [ ] Stack trace capture even from WASM (source maps for JS/TS)
- [ ] Error categorization: user error, timeout, OOM, permission denied
- [ ] Error rates surfaced in dashboard

---

## Phase 4: Plugin System & Middleware (Weeks 19–24)

### Goal
Extensible platform where community can build and share capabilities.

### 4.1 — Plugin Architecture
- [ ] Plugins are themselves WASM modules (sandboxed like functions)
- [ ] Plugin types:
  - **Middleware**: intercept requests/responses (auth, rate limiting, CORS)
  - **Services**: provide new host functions (email, SMS, payment)
  - **Triggers**: new event sources (webhooks, queue consumers)
- [ ] Plugin manifest declares capabilities required and provided
- [ ] Plugin registry (like npm/crates.io but for Ubiquitous plugins)

### 4.2 — Permission & Capability Audit
- [ ] Plugins declare what they need access to: specific URLs, KV namespaces, filesystem paths
- [ ] Runtime enforces declared permissions (deny by default)
- [ ] Audit report: what does this plugin actually use vs what it declares?
- [ ] Security score based on minimal-permission principle
- [ ] Community can flag over-permissioned plugins

### 4.3 — Middleware Pipeline
- [ ] Request → Middleware1 → Middleware2 → Function → Middleware2 → Middleware1 → Response
- [ ] Built-in middleware: CORS, rate limiting, request logging, auth token validation
- [ ] Middleware configured per-function or globally in `ubiq.toml`
- [ ] Short-circuit support (e.g., auth middleware returns 401 before hitting function)

### 4.4 — Built-in Plugins (First Party)
- [ ] `@ubiq/auth` — JWT validation, session management, OAuth flows
- [ ] `@ubiq/cors` — CORS configuration
- [ ] `@ubiq/rate-limit` — Token bucket rate limiting
- [ ] `@ubiq/cache` — Response caching
- [ ] `@ubiq/validate` — Request schema validation (from TypeScript types)

---

## Phase 5: Release & Deployment System (Weeks 25–32)

### Goal
Production deployment with atomic releases and coordinated rollbacks.

### 5.1 — Release Bundles
- [ ] `ubiq build --release` creates a bundle: all compiled WASM + manifest + checksums
- [ ] Bundle is a single artifact (tarball or OCI image)
- [ ] Semantic versioning enforced
- [ ] Content-addressed storage for deduplication
- [ ] Bundle metadata: functions included, versions, sizes, permissions summary

### 5.2 — Deployment Target
- [ ] `ubiq deploy` pushes bundle to upstream runtime
- [ ] Deployment targets configured in `ubiq.toml` (local, staging, production)
- [ ] Support multiple deployment strategies: blue-green, canary, rolling
- [ ] Pre-deployment health checks

### 5.3 — 2-Phase Commit Protocol
- [ ] Distributed consensus for multi-node deployments
- [ ] Phase 1 (Prepare): All nodes download and validate the bundle
- [ ] Phase 2 (Commit): All nodes atomically switch to new version
- [ ] If any node fails prepare → abort on all nodes
- [ ] Percentage-weighted routing during canary deployments (10% → 50% → 100%)
- [ ] Coordinated rollback: single command rolls back all nodes atomically

### 5.4 — Runtime Cluster
- [ ] Built-in distributed runtime (no Kubernetes required)
- [ ] Node discovery and health checking
- [ ] Leader election for coordination (Raft consensus)
- [ ] Request routing to appropriate nodes
- [ ] Auto-scaling based on load
- [ ] Node failure detection and traffic redistribution

### 5.5 — Local ↔ Production Parity
- [ ] `ubiq dev` and production runtime use the **identical WASM execution engine**
- [ ] Same resource limits, same host functions, same sandbox
- [ ] Only difference: data backends (embedded vs distributed)
- [ ] `ubiq deploy --local` for testing deployment flow locally

---

## Phase 6: Showcase Application — "Libra" Media Library (Weeks 33–40)

### Goal
Build a real application on Ubiquitous that competes with Audiobookshelf, Calibre, Plex, Jellyfin, and manages game ROMs/BIOSes. Proves the platform works for non-trivial applications.

### 6.1 — Core Media Library
- [ ] Plugin-based architecture: each media type is a plugin
- [ ] **Books Plugin**: EPUB, PDF, CBZ/CBR (comics), MOBI — metadata extraction, reading progress
- [ ] **Audiobooks Plugin**: MP3/M4B folder import, chapter detection, playback progress
- [ ] **Video Plugin**: MKV/MP4 scanning, metadata from TMDB/TVDB, subtitle support
- [ ] **Music Plugin**: Album/artist/track organization, scrobbling
- [ ] **ROMs Plugin**: ROM file management, BIOS library, per-console organization, hash verification
- [ ] Shared core: library scanning, metadata management, user progress tracking

### 6.2 — Media Server Functions
- [ ] `scan-library` — walk directories, identify media files, extract metadata
- [ ] `get-metadata` — fetch from external APIs (TMDB, MusicBrainz, OpenLibrary, No-Intro)
- [ ] `stream-media` — HTTP range request support for audio/video streaming
- [ ] `transcode` — on-the-fly transcoding (via WASM or native plugin)
- [ ] `sync-progress` — cross-device reading/playback progress
- [ ] `manage-collections` — user-curated collections across media types

### 6.3 — Web UI
- [ ] Built as static files served by Ubiquitous
- [ ] Responsive design (mobile, tablet, desktop)
- [ ] Library browser with cover art grid
- [ ] Integrated readers/players (ebook reader, audio player, video player)
- [ ] ROM launcher integration info (emulator configuration guides)
- [ ] Admin panel for library management

### 6.4 — Documentation & Tutorial
- [ ] "Build a Media Library with Ubiquitous" step-by-step guide
- [ ] Demonstrates: functions, plugins, KV store, file storage, events, cron
- [ ] Published as the primary showcase for the platform

---

## Phase 7: Polish & Launch Prep (Weeks 41–48)

### 7.1 — Documentation Site
- [ ] Getting started guide (<5 minutes to first running function)
- [ ] Concept guides (functions, plugins, deployment, permissions)
- [ ] API reference (all host functions, CLI commands, manifest format)
- [ ] Tutorials (build a TODO API, build a webhook handler, build a media library)
- [ ] Architecture deep-dives for contributors

### 7.2 — Performance & Benchmarking
- [ ] Published benchmarks: cold start, warm invocation, throughput, memory
- [ ] Comparison with AWS Lambda, Cloudflare Workers, Deno Deploy
- [ ] Optimization pass: sub-1ms warm invocations
- [ ] Load testing: 100K+ concurrent function instances

### 7.3 — Cross-Platform Testing
- [ ] CI/CD for Windows, macOS (Intel + ARM), Linux (x86_64 + ARM64)
- [ ] Single-binary distribution (GitHub releases, Homebrew, AUR, Chocolatey)
- [ ] Docker image for containerized deployments
- [ ] Install script: `curl -fsSL install.ubiq.dev | sh`

### 7.4 — Security Audit
- [ ] WASM sandbox escape review
- [ ] Host function permission enforcement audit
- [ ] Resource limit bypass testing
- [ ] Supply chain security for bundled compilers

### 7.5 — Open Source Launch
- [ ] Clean up repository, write CONTRIBUTING.md
- [ ] Choose license (Apache 2.0 or MIT)
- [ ] Launch on Hacker News, Reddit, Twitter/X
- [ ] Publish "Why We Built Ubiquitous" blog post
- [ ] Record demo video (5 min)

---

## Future Phases (Post-Launch)

### Phase 8: Cloud Offering
- [ ] Managed Ubiquitous hosting (deploy with `ubiq deploy --cloud`)
- [ ] Multi-tenant execution with billing
- [ ] Global edge deployment
- [ ] Managed databases, storage, secrets
- [ ] Usage-based pricing

### Phase 9: Advanced Runtime Features
- [ ] WebAssembly Component Model support
- [ ] Function composition (chain functions together)
- [ ] Durable execution (like Temporal/Durable Functions)
- [ ] WebSocket support for long-lived connections
- [ ] GPU access for ML inference functions

### Phase 10: Ecosystem Growth
- [ ] Plugin marketplace with verified publishers
- [ ] Template gallery (starter projects for common use cases)
- [ ] IDE extensions (VS Code, JetBrains)
- [ ] GitHub Actions integration
- [ ] Monitoring & alerting integrations

---

## Timeline Summary

| Phase | Duration | Focus | Key Deliverable |
|-------|----------|-------|----------------|
| **0** | Weeks 1–2 | Decisions | Architecture decision docs |
| **1** | Weeks 3–8 | Core DX | `ubiq dev` with hot reload, testing |
| **2** | Weeks 9–14 | Services | KV, storage, events, config |
| **3** | Weeks 15–18 | Observability | OTLP, dashboard, error handling |
| **4** | Weeks 19–24 | Plugins | Plugin system, middleware, permissions |
| **5** | Weeks 25–32 | Deployment | 2-phase commit, cluster, bundles |
| **6** | Weeks 33–40 | Showcase | "Libra" media library application |
| **7** | Weeks 41–48 | Launch | Docs, benchmarks, cross-platform, security |

**Total: ~12 months to launch-ready.**

---

## Success Metrics

### Phase 1 (Core DX)
- New project → running function: **< 30 seconds**
- File save → function reloaded: **< 1 second**
- Test execution: **< 500ms** for a simple test suite
- Zero external dependencies on developer machine

### Phase 5 (Deployment)
- Deploy command → live in production: **< 60 seconds**
- Rollback: **< 10 seconds**
- Zero-downtime deployments: **100%**

### Phase 7 (Launch)
- Cold start: **< 10ms**
- Warm invocation: **< 1ms**
- Memory per instance: **< 5MB**
- Concurrent instances per node: **100K+**
- Cross-platform: **Windows, macOS (Intel+ARM), Linux (x86_64+ARM64)**

---

## Risk Register

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| WASM compilation too slow for dev loop | Medium | High | Interpreter mode for dev; pre-compiled runtime modules; incremental compilation |
| Wasmtime/Extism API instability | Low | Medium | Pin versions; abstract behind internal interfaces |
| Cross-platform binary distribution complexity | Medium | Medium | Static linking; CI matrix; Docker fallback |
| Performance not competitive with native serverless | Medium | High | Benchmark early; optimize hot paths; consider native JS execution for perf |
| Plugin ecosystem bootstrap problem | High | Medium | Build 20+ first-party plugins; make plugin authoring trivially easy |
| 2-phase commit adds too much latency | Low | Medium | Async prepare phase; local caching of bundles |
| WASM sandbox limitations (no threads, limited I/O) | Medium | Medium | Component Model adoption; host function bridges for capabilities |
