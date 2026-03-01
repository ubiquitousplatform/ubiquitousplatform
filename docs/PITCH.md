# Ubiquitous — Elevator Pitch

---

## The One-Liner

**Ubiquitous is a serverless framework that lets you build, test, and deploy sandboxed applications with a single binary and zero configuration.**

---

## The Problem

Building and deploying serverless applications today is painful:

1. **"Works on my machine"**: Local development environments diverge from production. Bugs appear only after deployment.

2. **Dependency hell**: Your app needs Node.js 18, but your CI has 16, and production has 20. Oh, and you need Docker, Terraform, and three AWS services just to deploy a function.

3. **Vendor lock-in**: Write for AWS Lambda and you're married to AWS. Cloudflare Workers? Married to Cloudflare. Every platform has its own runtime, its own quirks, its own limitations.

4. **Security is an afterthought**: Third-party packages can read your filesystem, make network requests to arbitrary URLs, and access environment variables. You find out when it's too late.

5. **Deployments are scary**: Rolling out to a distributed system means crossing your fingers. Rolling back means scrambling at 2 AM.

6. **Tooling fragmentation**: One tool to build, another to test, another to deploy, another to monitor. Each with its own config file, its own CLI, its own learning curve.

---

## The Solution

Ubiquitous is a **single binary** that includes everything:

```bash
# Install (once)
curl -fsSL https://ubiq.run/install | sh

# Create a project
ubiq new myapp && cd myapp

# Develop with hot reload
ubiq dev

# Test (in the same sandbox as production)
ubiq test

# Deploy atomically to your cluster
ubiq deploy
```

**That's it.** No Docker. No Kubernetes. No Terraform. No cloud provider SDKs.

---

## How It Works

Every function runs inside a **WebAssembly sandbox**. This gives you:

- **Identical local and production behavior** — the same WASM runtime runs everywhere
- **Security by default** — functions can't access anything they don't explicitly declare
- **Polyglot support** — write in TypeScript, JavaScript, Rust, Go, or Python
- **Sub-millisecond warm starts** — pre-warmed WASM instance pools
- **Trivial scaling** — (tbd)) concurrent sandboxed instances on a single node

---

## What Makes Us Different

### vs. AWS Lambda / Cloudflare Workers
- **No vendor lock-in.** Self-host or use our cloud. Functions run identically everywhere.
- **Local development is real.** Same runtime, not a cloud simulacrum.
- **Atomic deployments.** coordinated 2-phase commit across a distributed fleet of nodes and any number of functions. Every deployment is safe.

### vs. Docker / Kubernetes
- **No containers.** WASM sandboxes start in microseconds, not seconds. This means you don't have to wait for scale-out or scale-in.
- **No YAML.** A 10-line `ubiq.toml` replaces pages of Kubernetes manifests.
- **No ops team needed.** Built-in clustering, health checks, and auto-scaling.

### vs. Firebase / Supabase
- **Self-hostable.** Your data stays on your infrastructure.
- **Not a proprietary platform.** Open source runtime, standard protocols.
- **Real sandboxing.** Functions can't access each other's data by default.

### vs. Rails / Django / Next.js
- **Language-agnostic.** Not tied to Ruby, Python, or JavaScript.
- **Built-in deployment.** `ubiq deploy` is all you need.
- **Sandboxed by architecture.** Security isn't bolted on, it's the runtime model.

---

## Key Features

| Feature | Status |
|---------|--------|
| Single binary, zero dependencies | Core feature |
| Hot reload in < 1 second | Core feature |
| Tests run in production sandbox | Core feature |
| Built-in KV store | Core feature |
| OTLP observability (traces, metrics, logs) | Core feature |
| Plugin system with permission auditing | Core feature |
| 2-phase commit atomic deployments | Core feature |
| Coordinated multi-node rollbacks | Core feature |
| File-system based routing | Core feature |
| Monorepo support | Core feature |
| Built-in middleware pipeline | Core feature |
| Cron scheduling | Core feature |
| 100K+ concurrent instances per node | Performance target |
| Sub-1ms warm invocations | Performance target |
| Cross-platform (Win/Mac/Linux, Intel/ARM) | Core feature |

---

## The Business

### Target Market
1. **Indie developers and startups** — Ship faster with less infrastructure complexity
2. **Self-hosting enthusiasts** — Run your own cloud without cloud vendor costs
3. **Platform engineering teams** — Internal developer platform in a box

### Revenue Model
- **Open source runtime** (MIT license) — free forever, build community
- **Ubiquitous Cloud** — managed hosting, pay per invocation
- **Enterprise** — SSO, audit logs, dedicated support, SLA
- **Plugin Marketplace** — revenue share with plugin authors

### Competitive Moat
1. **WASM runtime** — years of engineering investment, hard to replicate
2. **2-phase commit deployment** — no other serverless platform offers this
3. **Plugin ecosystem** — network effects of community-built extensions
4. **"Rails for serverless"** — opinionated DX creates loyalty

---

## The Vision

Every developer should be able to go from idea to production in minutes, not days. Your code should run identically on your laptop and in a datacenter. Security should be a guarantee, not a checklist. And deploying should be as simple as saving a file.

**Ubiquitous makes serverless development ubiquitous.**

---

## Traction / Validation

- Working prototype with two WASM execution engines (Wasmtime + Extism)
- Sub-millisecond warm invocation latency demonstrated
- Auto-scaling pool supporting 1,000+ concurrent instances
- TypeScript → WASM compilation pipeline operational
- Test harness executing inside WASM sandbox validated
- Host function IPC protocol (KV, logging) implemented and tested

---

## What We Need

- **3 months** to ship the MVP (CLI + runtime + test harness + hot reload)
- **6 months** to platform services (KV, storage, events, observability)
- **12 months** to production-ready with deployment system and showcase app

A single senior developer can build the MVP. Two developers can ship production-ready in 12 months.

---

## The Ask

We're looking for:
1. **Early adopters** — developers willing to try the alpha and give feedback
2. **Contributors** — Rust/TypeScript engineers excited about WASM
3. **Funding** — to accelerate from 12 months to 6 months with a small team

If you are interested in any of the above, and inspired by the project, please reach out via GitHub!

---

*"The best framework is the one you don't have to think about."*
