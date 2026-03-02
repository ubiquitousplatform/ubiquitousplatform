# Ubiquitous Platform — Documentation

> Serverless framework and runtime built on WebAssembly.

---

## Quick Links

| Document | Description |
|----------|-------------|
| [PITCH.md](PITCH.md) | Elevator pitch — why Ubiquitous exists |
| [GETTING-STARTED.md](GETTING-STARTED.md) | Zero to running function in 30 seconds |
| [PROJECT-PLAN.md](PROJECT-PLAN.md) | Phased roadmap from MVP to launch |
| [QUESTIONS.md](QUESTIONS.md) | Open decisions needing resolution |

---

## Architecture Documentation

| Document | Description |
|----------|-------------|
| [ARCHITECTURE.md](ARCHITECTURE.md) | System overview, request lifecycle, design principles |
| [MODULE-CLI.md](MODULE-CLI.md) | CLI tool (`ubiq`) — commands, flags, workflows |
| [MODULE-EXECUTION-ENGINE.md](MODULE-EXECUTION-ENGINE.md) | WASM engine, function pool, host function IPC, resource limits |
| [MODULE-MANIFEST.md](MODULE-MANIFEST.md) | Project structure, `ubiq.toml`, routing, conventions |
| [MODULE-SERVICES.md](MODULE-SERVICES.md) | KV store, file storage, events, HTTP client, config, logging, cron |
| [MODULE-SDK.md](MODULE-SDK.md) | SDK & standard library — the API functions use to access the platform |
| [MODULE-PLUGINS.md](MODULE-PLUGINS.md) | Plugin system, middleware, permissions, registry |
| [MODULE-DEPLOYMENT.md](MODULE-DEPLOYMENT.md) | Release bundles, 2-phase commit, cluster, rollback |
| [MODULE-OBSERVABILITY.md](MODULE-OBSERVABILITY.md) | OTLP telemetry, dashboard, error tracking, health checks |
| [MODULE-TEST-HARNESS.md](MODULE-TEST-HARNESS.md) | Test framework, WASM-sandboxed tests, mocking |
| [SECURITY-MODEL.md](SECURITY-MODEL.md) | Sandbox guarantees, permissions, auditing, multi-tenancy |

---

## Analysis & Decisions

| Document | Description |
|----------|-------------|
| [RUNTIME-LANGUAGE-DECISION.md](RUNTIME-LANGUAGE-DECISION.md) | C# vs Rust vs Go — benchmarks and recommendation |
| [FRAMEWORK-COMPARISON.md](FRAMEWORK-COMPARISON.md) | Deep comparison of 10 popular web frameworks |

---

## Showcase Application

| Document | Description |
|----------|-------------|
| [LIBRA-MEDIA-APP.md](LIBRA-MEDIA-APP.md) | "Libra" — plugin-based media library (books, audio, video, ROMs) |

---

## Reading Order

If you're new to the project:

1. **[PITCH.md](PITCH.md)** — Understand the vision (2 min)
2. **[GETTING-STARTED.md](GETTING-STARTED.md)** — See the developer experience (3 min)
3. **[ARCHITECTURE.md](ARCHITECTURE.md)** — How it all fits together (5 min)
4. **[PROJECT-PLAN.md](PROJECT-PLAN.md)** — What we're building and when (10 min)
5. **Module docs** — Deep dive into specific areas as needed
6. **[QUESTIONS.md](QUESTIONS.md)** — Help make key decisions

---

## Current State

The existing codebase (outside `docs/`) contains:

- **Two working WASM execution engines** (Wasmtime-direct + Extism SDK) in C#
- **Function pool** with auto-scaling (10–1000 instances)
- **Host function IPC protocol** for guest-host communication
- **ASP.NET Core API** serving as the runtime HTTP layer
- **TypeScript → WASM compilation pipeline** (esbuild → extism-js)
- **Test harness** executing inside WASM sandbox
- **Performance benchmarks** comparing runtime approaches
- **Example functions** in JS and TS (weather forecast, count vowels)
- **Host functions API** defined in TypeSpec (KV store)
- **Early Rust prototype** (axum + extism)

This documentation defines where we're going from here.
