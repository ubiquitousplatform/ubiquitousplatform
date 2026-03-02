# Open Questions

> Decisions and uncertainties that need resolution. Answer these to unblock further iteration.

---

## Architecture & Runtime

### Q1: Runtime Implementation Language
**Context**: Current prototype is C#. Rust offers best WASM integration and smallest binary. Go is fastest to ship.

**Options**:
- A) Rust for everything (best long-term but highest initial investment)
- B) Keep C# runtime, Rust CLI (pragmatic hybrid)
- C) Go for everything (fastest to ship, may need rewrite later)
- D) Keep C# for everything (ship fastest with existing code, accept larger binary)

**My lean**: Rust for core runtime + CLI, but this doubles the timeline. What's the priority — shipping fast or shipping right?

---

### Q2: Extism vs Raw Wasmtime
**Context**: The codebase has two parallel execution engines. Extism provides multi-language PDK, plugin lifecycle, and memory management. Raw Wasmtime gives more control.

**Options**:
- A) Extism only (simpler, multi-language PDK, community support)
- B) Wasmtime only (more control, custom IPC, no external SDK dependency)
- C) Extism as default, Wasmtime as fallback for advanced use cases

**My lean**: Extism. The multi-language PDK is essential for polyglot support, and Extism is built on Wasmtime anyway. But does the `invoke_json` custom IPC in the Wasmtime path provide capabilities we'd lose?

---

### Q3: JavaScript Runtime Inside WASM
**Context**: Currently using QuickJS compiled to WASM (via Javy/extism-js). This adds overhead vs native V8/SpiderMonkey for JS execution.

**Question**: For the JS/TS path, should we:
- A) Always run JS in QuickJS-WASM (consistent sandbox, portable, slower)
- B) Offer a "native JS" mode using V8 isolates for performance (like Cloudflare Workers)
- C) Start with QuickJS-WASM, add native mode later as optimization

**Trade-off**: QuickJS in WASM is ~10-100x slower than V8 for compute-heavy JS, but provides perfect sandbox portability. Most serverless functions are I/O-bound, not CPU-bound.

---

### Q4: Component Model vs Preview1 WASI
**Context**: WASI Preview 1 is stable but limited. The Component Model (Preview 2) is the future but still maturing.

**Question**: Should we target WASI Preview 1 for initial release, or invest in Component Model early?

**My lean**: Start with Preview 1 for stability, design abstractions that can adopt Component Model later. But if the timeline is 12 months, Component Model may be stable by then.

---

## Developer Experience

### Q5: Routing Convention
**Context**: How should file paths map to HTTP routes?

**Options**:
- A) File name = route, method inferred from export name (`export function GET`, `export function POST`)
- B) File name = route, method always GET unless overridden in manifest
- C) File name includes method: `get-users.ts` → GET, `post-users.ts` → POST
- D) All routing explicit in `ubiq.toml`, no convention-based routing

**My lean**: Option A (like Next.js/SvelteKit). But should we support both convention and explicit?

---

### Q6: SDK Import Style
**Context**: How do functions access platform services?

**Options**:
- A) Explicit imports: `import { kv, log } from '@ubiq/sdk'`
- B) Global objects: `ubiq.kv.get(...)` (no import needed)
- C) Function parameters: `export default function(input, { kv, log }) { ... }`
- D) Decorator/annotation based: `@UseKV() export default function...`

**My lean**: Option A for TypeScript (idiomatic), Option C is simpler but less typed. What feels right?

---

### Q7: Function Signature
**Context**: What's the function contract?

**Options**:
- A) Single default export: `export default function(input) { return output; }`
- B) Named exports per method: `export function GET(input) { ... }`
- C) Handler object: `export default { GET(input) { ... }, POST(input) { ... } }`
- D) Class-based: `export default class Handler { GET(input) { ... } }`

**Trade-off**: Simpler is better for adoption. But multi-method support per file reduces file count.

---

### Q8: TypeScript Types for Input/Output
**Context**: Should we generate/enforce types for function I/O?

**Options**:
- A) Types are optional, JSON in/out by default
- B) Types required, auto-generate OpenAPI from TStype annotations (like FastAPI)
- C) Schema in manifest, codegen types from schema
- D) Runtime validation from types (like zod)

**My lean**: Start with A (optional), add B later as a killer feature. FastAPI proved this pattern is beloved.

---

## Business & Product

### Q9: Open Source Model
**Context**: How should the project be licensed and monetized?

**Options**:
- A) Fully open source (MIT/Apache), monetize with managed cloud hosting
- B) Open core — runtime is open, cloud features are commercial
- C) Source available (BSL/SSPL) — free to use, restrictions on competing hosted services
- D) Dual license (GPL for open source use, commercial license for proprietary)

**My lean**: Option A or B. The runtime should be fully open to maximize adoption. Monetize the cloud hosting, enterprise support, and managed plugins.

---

### Q10: Primary Target Audience
**Context**: Who are we building for first?

**Options**:
- A) Solo developers and indie hackers (simplicity-first, self-hostable)
- B) Startups (rapid development, scale from prototype to production)
- C) Enterprise teams (security, compliance, distributed systems)
- D) Platform engineers building internal developer platforms

**My lean**: A and B first. The "Heroku for WASM" positioning. Enterprise features come later.

---

### Q11: Media Library App — Scope for MVP
**Context**: The "Libra" showcase app that competes with Audiobookshelf, Plex, Jellyfin, etc.

**Questions**:
1. Which media type should we start with? (Books? Audio? Video? ROMs?)
2. Should this be a separate project or live in this repo?
3. What's the minimum feature set to be useful?
4. Do you want a web UI, mobile app, or both?
5. Should it focus on self-hosting (competing with Plex/Jellyfin) or be cloud-native?

**My lean**: Start with ebooks + audiobooks (simpler than video, clear market gap). Self-hosted first. Web UI only for MVP.

---

### Q12: Name
**Context**: "Ubiquitous" is descriptive but long. CLI commands like `ubiquitous dev` are too verbose.

**Options**:
- A) Keep "Ubiquitous" with `ubiq` CLI shorthand
- B) Rename to something shorter (Ubiq? Wave? Wax? Ubi?)
- C) "Ubiquitous" is the platform, but the CLI tool has its own short name

**My lean**: "Ubiquitous" as the platform brand, `ubiq` as the CLI tool.

---

## Technical Details

### Q13: Compiler Bundling Strategy
**Context**: We need esbuild, extism-js/Javy, potentially cargo/tinygo/componentize-py.

**Options**:
- A) Bundle all compilers in the binary (large but zero-fetch)
- B) Download compilers on first use (small binary, requires internet)
- C) Hybrid: bundle JS/TS compilers (most common), download others on demand

**My lean**: Option C. JS/TS is the 80% case, bundle those. Download Rust/Go/Python compilers on demand.

---

### Q14: KV Store Technology (if staying with C#)
**Context**: You mentioned Garnet (Microsoft's Redis-compatible cache in C#).

**Options**:
- A) Garnet (C#-native, Redis-compatible, Microsoft-backed)
- B) SQLite via `Microsoft.Data.Sqlite` (simpler, embedded, battle-tested)
- C) LiteDB (C#-native document store, embedded)
- D) RocksDB via RocksDBSharp (LSM-tree, high write throughput)

**For dev**: SQLite is simplest. For production: Garnet or Redis.

---

### Q15: Existing Rust Prototype
**Context**: There's a Rust prototype in `rust/ubiquitous-functions/` using axum + extism. It has compilation errors and is early-stage.

**Question**: Should we:
- A) Build on this prototype and fix it up
- B) Start fresh with lessons learned from C# prototype
- C) Archive the Rust prototype, keep C# as the primary path

**My lean**: B if we go Rust. The C# prototype teaches us the architecture; the Rust rewrite should be clean.

---

### Q16: Multi-Tenancy
**Context**: For the cloud offering and the runtime cluster.

**Question**: When multiple users/apps share a cluster, how do we isolate them?

**Options**:
- A) OS-process isolation (one process per tenant)
- B) WASM sandbox isolation only (shared process, different WASM instances)
- C) Namespace isolation (shared everything, logical separation)
- D) Node isolation (dedicated nodes per tenant)

**My lean**: B for now (WASM is designed for this). Add A or D for enterprise plans.

---

### Q17: Database Integration
**Context**: The KV store handles simple key-value data. But real apps often need relational data.

**Question**: Should Ubiquitous include:
- A) KV only — keep it simple, users bring their own database
- B) KV + embedded SQLite — functions can query a local SQLite database
- C) KV + a full ORM-like abstraction
- D) KV + a document store (like MongoDB queries)

**My lean**: Start with A, add B later. SQLite in WASM is well-proven. But this is a big scope decision.

---

## Priority Questions (Answer These First)

1. **Q1**: Runtime language (this determines everything else)
2. **Q10**: Target audience (this determines feature priority)
3. **Q9**: Open source model (this determines business model and community strategy)
4. **Q5 + Q7**: Routing + function signature (this determines the core DX)
5. **Q11**: Media library scope (this determines the showcase app direction)
