# PDK Integration Plan: Multi-Language Compilation & SDK Shim

> Plan for establishing a working, tested compilation pipeline for every Extism PDK language,
> with a versioned Ubiquitous SDK shim, Garnet KV integration, and Aspire distributed tracing.

**Status**: Planning — March 2026  
**Depends on**: [RUNTIME-LANGUAGE-DECISION.md](../RUNTIME-LANGUAGE-DECISION.md) (decided: C# + Extism)

---

## Objective

Produce a verified, CI-tested baseline where:

1. **Every Extism PDK language** has a compilable example function in the `examples/` directory
2. Each example uses the **Ubiquitous SDK shim** to call KV (Garnet) and emit a trace span visible in Aspire
3. The SDK shim is **versioned** so that plugins compiled against older shim versions continue to work as we add host functions
4. We have a documented, reproducible toolchain per language including Docker-based fallbacks

---

## Supported Languages

Extism provides officially maintained PDKs for:

| Language | PDK Package | Target | Notes |
|----------|-------------|--------|-------|
| **TypeScript / JavaScript** | `@extism/js-pdk` | `wasm32-unknown-unknown` | Via extism-js compiler (QuickJS engine) |
| **Rust** | `extism-pdk` crate | `wasm32-unknown-unknown` | Native, smallest output binary |
| **Go** | `github.com/extism/go-pdk` | `wasm32-unknown-unknown` | Requires TinyGo |
| **Python** | `extism-pdk` package | `wasm32-unknown-unknown` | Experimental; via Extism Python PDK |
| **C#** | `Extism.Pdk` NuGet | `wasm32-unknown-unknown` | Via componentize-dotnet |
| **Zig** | `extism-pdk` (git) | `wasm32-freestanding` | Small output, manual memory management |
| **AssemblyScript** | `@extism/as-pdk` | `wasm32-unknown-unknown` | TypeScript subset, smallest TS output |
| **C** | `extism-pdk.h` | `wasm32-unknown-unknown` | Via clang --target wasm32 |
| **Haskell** | `extism-pdk` cabal | `wasm32-wasi` | Experimental, large binary |

MVP priority order: **TypeScript → Rust → Go → Python → C# → Zig → AssemblyScript**

---

## The Ubiquitous SDK Shim

### Purpose

The Extism PDK gives plugins the ability to call arbitrary host functions by name. Without a shim, each plugin
author would need to manually serialize/deserialize arguments, handle versioning, and know the exact host
function names. The Ubiquitous SDK shim provides:

- **Idiomatic API per language** — `kv.get("key")`, `trace.span("my-op")` etc.
- **Serialization** — MessagePack encoding of all host function arguments and responses
- **Versioning** — the shim declares what SDK version it requires; the host validates this at plugin load time
- **Backward compatibility** — older plugins compiled against older shim versions continue to work

### Why MessagePack

- **Compact binary format** — significantly smaller than JSON on the wire; important as all host function calls cross the WASM memory boundary
- **Schema-free** — no TypeSpec/IDL required; maps/arrays serialize naturally
- **Schemaless evolution** — adding new fields to a response is backward-compatible without re-compilation of older plugins
- **Wide language support** — first-class MessagePack libraries exist for every PDK language
- **Faster decode than JSON** — benchmarks show 2–5× faster decode for typical payloads

---

## Host Function Contract

### Call Envelope

Every host function call from a plugin uses the following MessagePack-encoded envelope, passed as the single byte buffer to the host function:

```
[
  protocol_version: uint8,   // always 1 for current version
  sdk_version:      uint16,  // semver minor * 100 + patch (e.g. 1.02 = 102)
  namespace:        str,     // "kv", "trace", "storage", "http", "config", "log"
  function:         str,     // "get", "set", "span_start", etc.
  payload:          any       // namespace+function-specific msgpack map
]
```

The host reads `protocol_version` first. If it does not recognise the version, it returns an error envelope.
`sdk_version` is advisory — the host logs a warning if the plugin's SDK is below a minimum supported floor but
still executes the call.

### Response Envelope

```
[
  ok:      bool,        // true = success, false = error
  version: uint16,      // host SDK version that handled this call
  data:    any          // response payload (namespace+function-specific) or error string
]
```

### Host Function Registration (C# side)

All host functions are registered under the Extism namespace `ubiq`:

```
ubiq::kv_get
ubiq::kv_set
ubiq::kv_delete
ubiq::kv_list
ubiq::trace_start_span
ubiq::trace_end_span
ubiq::trace_set_attr
ubiq::storage_read
ubiq::storage_write
ubiq::log_write
ubiq::config_get
ubiq::http_request
```

---

## SDK Versioning Strategy

### Semantic Versioning for Host Functions

The SDK version follows `MAJOR.MINOR.PATCH`:

- `MAJOR` bump: breaking change to the call envelope format, or removal of an existing function
- `MINOR` bump: new host functions added (backward-compatible — old plugins ignore new capabilities)
- `PATCH` bump: bug fixes, performance improvements — no contract change

### Version Floor Policy

The host maintains a **minimum supported SDK version** (the floor). Plugins compiled against an SDK version
below the floor will receive a structured error at first invocation with upgrade guidance, rather than silently
misbehaving. The floor advances with `MAJOR` bumps only.

| SDK Version Range | Host Behaviour |
|-------------------|---------------|
| Below floor | Hard reject at plugin load; error message with minimum version |
| At or above floor, below current | Execute normally; host may use newer response fields the plugin ignores |
| At current | Full compatibility |
| Above current | Host returns `ok: false, data: "sdk_version_too_new"` — plugin is ahead of host |

This is the **forward-compatibility gap** — a plugin should never be compiled against an SDK newer than the
host it is deployed to. This is analogous to targeting a newer .NET SDK than the runtime you are deploying to.

### Backward Compatibility Guarantees

When adding a new minor version:
- Existing function signatures do not change
- Existing response fields do not change or are removed
- New response fields MAY be added; old plugins will ignore them via msgpack's schema-free decoding
- New host functions MAY be added; old plugins simply never call them

When bumping to a new major version:
- A migration guide documents every breaking change
- The old major version remains the floor for 6 months (let existing deployments update)
- After 6 months, the floor advances to the new major version

### Plugin Manifest Declaration

```toml
# ubiq.toml
[function]
name = "create-user"
language = "typescript"
sdk_version = "1.2"   # minimum SDK the plugin was compiled against
```

The host validates this against the supported range at load time.

---

## Phase 1: TypeScript / JavaScript

### Goal
Working `examples/typescript/` that compiles, integrates KV + tracing, with documented build steps.

### Toolchain
```
Node.js >= 18
npm install -g @extism/js-pdk          # plugin compiler CLI
npm install @ubiquitous/sdk-js          # Ubiquitous SDK shim (to be built)
```

### Compilation Pipeline
```
source.ts
  → esbuild (bundle, CJS, es2020, no node_modules)
  → bundle.js
  → extism-js bundle.js -o output.wasm   # embeds QuickJS
```

### Example Function
```typescript
// examples/typescript/kv-trace-demo/index.ts
import { kv, trace } from "@ubiquitous/sdk";

export function run(input: string): string {
  const span = trace.startSpan("kv-trace-demo");
  const name = JSON.parse(input).name as string;

  kv.set(`greeting:${name}`, `Hello, ${name}!`);
  const value = kv.get(`greeting:${name}`)!;

  trace.setAttribute("greeting.key", `greeting:${name}`);
  span.end();

  return JSON.stringify({ message: value });
}
```

### Integration Test
- [ ] Compile to `.wasm` — verify output < 2MB
- [ ] Run via `ubiquitous.functions` Extism host — verify it loads cleanly
- [ ] Call `run({name:"Alice"})` — verify KV write appears in Garnet
- [ ] Verify a span named `kv-trace-demo` appears in the Aspire dashboard

### SDK Shim (`@ubiquitous/sdk`)
```typescript
// packages/sdk-js/src/kv.ts
import { call } from "./host";

export const kv = {
  get(key: string): string | null {
    const res = call("ubiq::kv_get", { key });
    return res.data?.value ?? null;
  },
  set(key: string, value: string, ttl?: number): void {
    call("ubiq::kv_set", { key, value, ttl });
  },
  delete(key: string): boolean {
    return call("ubiq::kv_delete", { key }).data?.deleted ?? false;
  },
  list(prefix: string): string[] {
    return call("ubiq::kv_list", { prefix }).data?.keys ?? [];
  },
};
```

The `call()` primitive encodes the envelope to MessagePack and invokes the Extism host function.

---

## Phase 2: Rust

### Goal
Working `examples/rust/` Rust crate targeting `wasm32-unknown-unknown` with UbiqKV + tracing.

### Toolchain
```
rustup target add wasm32-unknown-unknown
cargo add extism-pdk
cargo add ubiquitous-sdk             # Ubiquitous SDK shim crate (to be built)
```

### Compilation Pipeline
```
cargo build --target wasm32-unknown-unknown --release
# Optional post-process:
wasm-opt -O2 target/wasm32-unknown-unknown/release/my_fn.wasm -o output.wasm
```

### Example Function
```rust
// examples/rust/kv-trace-demo/src/lib.rs
use extism_pdk::*;
use ubiquitous_sdk::{kv, trace};
use serde::{Deserialize, Serialize};

#[derive(Deserialize)]
struct Input { name: String }

#[derive(Serialize)]
struct Output { message: String }

#[plugin_fn]
pub fn run(Json(input): Json<Input>) -> FnResult<Json<Output>> {
    let span = trace::start_span("kv-trace-demo")?;

    let key = format!("greeting:{}", input.name);
    let val = format!("Hello, {}!", input.name);
    kv::set(&key, &val)?;
    let stored = kv::get(&key)?.unwrap_or_default();

    trace::set_attribute("greeting.key", &key)?;
    span.end()?;

    Ok(Json(Output { message: stored }))
}
```

### SDK Shim (`ubiquitous-sdk` crate)
```rust
// crates/ubiquitous-sdk/src/kv.rs
use crate::host::{call, UbiqError};

pub fn get(key: &str) -> Result<Option<String>, UbiqError> {
    let res = call("ubiq::kv_get", &[("key", key)])?;
    Ok(res.data.and_then(|d| d.get("value").and_then(|v| v.as_str().map(String::from))))
}

pub fn set(key: &str, value: &str) -> Result<(), UbiqError> {
    call("ubiq::kv_set", &[("key", key), ("value", value)])?;
    Ok(())
}
```

### Integration Test
- [ ] `cargo build --target wasm32-unknown-unknown --release` succeeds
- [ ] Verify output < 500KB before wasm-opt, < 200KB after
- [ ] Load via Extism host, call `run({name:"Bob"})`, verify KV and span

---

## Phase 3: Go

### Goal
Working `examples/go/` with TinyGo compilation to WASM.

### Toolchain
```
brew install tinygo         # macOS
# or: https://tinygo.org/getting-started/install/
go get github.com/extism/go-pdk
go get github.com/ubiquitous/sdk-go    # Ubiquitous SDK shim (to be built)
```

### Compilation Pipeline
```
tinygo build -o output.wasm -target wasi ./...
```

> Note: standard `go build` does not produce working WASM for Extism. TinyGo is required.

### Example Function
```go
// examples/go/kv-trace-demo/main.go
package main

import (
    "encoding/json"
    pdk "github.com/extism/go-pdk"
    ubiq "github.com/ubiquitous/sdk-go"
)

type Input struct { Name string `json:"name"` }
type Output struct { Message string `json:"message"` }

//export run
func run() int32 {
    var input Input
    json.Unmarshal(pdk.Input(), &input)

    span := ubiq.Trace.StartSpan("kv-trace-demo")
    key := "greeting:" + input.Name
    ubiq.KV.Set(key, "Hello, "+input.Name+"!")
    value, _ := ubiq.KV.Get(key)
    ubiq.Trace.SetAttribute("greeting.key", key)
    span.End()

    out, _ := json.Marshal(Output{Message: value})
    pdk.OutputString(string(out))
    return 0
}

func main() {}
```

### Gotchas
- TinyGo does not support all standard library packages. Use `fmt.Sprintf` not `log.Println`.
- Goroutines are not supported in WASM builds.
- Use `//export` annotation, not Go module init.

### Integration Test
- [ ] `tinygo build -target wasi ./...` succeeds
- [ ] Verify output < 1MB
- [ ] Load via Extism host, validate KV + span

---

## Phase 4: Python

### Goal
Working `examples/python/` using the Extism Python PDK.

### Toolchain
```
pip install extism-pdk          # installs extism-py compiler
pip install ubiquitous-sdk      # Ubiquitous SDK shim (to be built)
```

> The Extism Python PDK bundles a MicroPython interpreter into the WASM output binary.
> Expected output size: 5–15MB. This is acceptable for MVP; optimization is a later concern.

### Compilation Pipeline
```
# extism-py compiles .py source into a WASM bundle with embedded interpreter
extism-py build main.py -o output.wasm
```

### Example Function
```python
# examples/python/kv-trace-demo/main.py
import extism
from ubiquitous_sdk import kv, trace
import json

@extism.plugin_fn
def run():
    input_data = json.loads(extism.input_str())
    name = input_data["name"]

    span = trace.start_span("kv-trace-demo")
    key = f"greeting:{name}"
    kv.set(key, f"Hello, {name}!")
    value = kv.get(key)
    trace.set_attribute("greeting.key", key)
    span.end()

    extism.output(json.dumps({"message": value}))
```

### Known Limitations
- Python output WASM is large (5–15MB due to embedded interpreter)
- Cold start is slower due to interpreter initialization (~20–50ms vs <1ms for Rust)
- Async/await is not supported in the PDK

### Integration Test
- [ ] `extism-py build` succeeds
- [ ] Load despite large size — verify memory limit is set appropriately (128MB+)
- [ ] Validate KV round-trip and span

---

## Phase 5: C# as Guest

### Goal
Working `examples/dotnet/` where a C# function compiles to WASM as an Extism plugin.

> This is separate from the C# host. Here C# is the **guest** language running inside the WASM sandbox.

### Toolchain
```
# Requires componentize-dotnet (dotnet-wasi-sdk)
dotnet add package Extism.Pdk
dotnet add package Ubiquitous.Sdk     # to be built
```

### Compilation Pipeline
```
# componentize-dotnet compiles C# to WASM module via NativeAOT + WASI
dotnet build -c Release /p:RuntimeIdentifier=wasi-wasm
```

> Note: componentize-dotnet is in preview. Expect rough edges.

### Example Function
```csharp
// examples/dotnet/KvTraceDemo/Plugin.cs
using Extism;
using Ubiquitous.Sdk;

public static class Plugin
{
    [UnmanagedCallersOnly(EntryPoint = "run")]
    public static int Run()
    {
        var input = JsonSerializer.Deserialize<InputData>(Pdk.GetInputString())!;

        using var span = Trace.StartSpan("kv-trace-demo");
        var key = $"greeting:{input.Name}";
        Kv.Set(key, $"Hello, {input.Name}!");
        var value = Kv.Get(key);
        Trace.SetAttribute("greeting.key", key);

        Pdk.SetOutput(JsonSerializer.Serialize(new { message = value }));
        return 0;
    }
}
```

### Integration Test
- [ ] `dotnet build -c Release` succeeds with wasi-wasm runtime identifier
- [ ] Output WASM loads in Extism host
- [ ] KV and span verified

---

## Phase 6: Zig

### Goal
Working `examples/zig/` — smallest possible WASM output with full SDK integration.

### Toolchain
```
# Zig ships with WASM cross-compilation built in
zig version  # >= 0.12
# Ubiquitous SDK shim is a zig package (to be built)
```

### Compilation Pipeline
```
zig build-lib src/main.zig -target wasm32-freestanding -dynamic \
  -rdynamic -O ReleaseSmall
```

### Example Function
```zig
// examples/zig/kv-trace-demo/src/main.zig
const std = @import("std");
const ubiq = @import("ubiquitous-sdk");

export fn run() i32 {
    var gpa = std.heap.GeneralPurposeAllocator(.{}){};
    defer _ = gpa.deinit();
    const allocator = gpa.allocator();

    const input = extism.input(allocator) catch return 1;
    defer allocator.free(input);

    const key = std.fmt.allocPrint(allocator, "greeting:{s}", .{input}) catch return 1;
    defer allocator.free(key);

    const span = ubiq.trace.startSpan("kv-trace-demo");
    ubiq.kv.set(key, "Hello!") catch return 1;
    ubiq.trace.setAttribute("greeting.key", key) catch {};
    span.end();

    extism.outputString("done");
    return 0;
}
```

### Integration Test
- [ ] `zig build-lib` produces output < 50KB (Zig is remarkably compact)
- [ ] Load and execute via Extism host
- [ ] Validate KV and span

---

## Phase 7: AssemblyScript

### Goal
Working `examples/assemblyscript/` — TypeScript syntax, smallest WASM footprint for simple functions.

### Toolchain
```
npm install --save-dev assemblyscript @extism/as-pdk
npm install @ubiquitous/sdk-as    # AssemblyScript SDK shim (to be built)
```

### Compilation Pipeline
```
npx asc assembly/index.ts --target release --outFile output.wasm
```

### Example Function
```typescript
// examples/assemblyscript/kv-trace-demo/assembly/index.ts
import { Host } from "@extism/as-pdk";
import { kv, trace } from "@ubiquitous/sdk-as";

export function run(): i32 {
  const input = JSON.parse<Map<string, string>>(Host.inputString());
  const name = input.get("name");

  const span = trace.startSpan("kv-trace-demo");
  const key = "greeting:" + name;
  kv.set(key, "Hello, " + name + "!");
  const value = kv.get(key);
  trace.setAttribute("greeting.key", key);
  span.end();

  Host.outputString(JSON.stringify<Map<string, string>>(
    new Map<string, string>().set("message", value)
  ));
  return 0;
}
```

### Integration Test
- [ ] `npx asc` compilation succeeds
- [ ] Output < 200KB
- [ ] Validate KV and span

---

## SDK Shim Build Plan

### Repository Structure

```
sdk/
  ubiquitous-sdk-js/          # @ubiquitous/sdk — TypeScript/JavaScript
    src/
      host.ts                 # msgpack envelope + Extism call() primitive
      kv.ts
      trace.ts
      storage.ts
      config.ts
      log.ts
      http.ts
    package.json
    tsconfig.json
    README.md

  ubiquitous-sdk-rust/        # ubiquitous-sdk crate
    src/
      host.rs                 # msgpack serialization + extism-pdk call
      kv.rs
      trace.rs
      ...
    Cargo.toml

  ubiquitous-sdk-go/          # github.com/ubiquitous/sdk-go
    kv/kv.go
    trace/trace.go
    host/host.go              # msgpack + extism go-pdk bridge
    go.mod

  ubiquitous-sdk-python/      # ubiquitous-sdk PyPI package
    ubiquitous/
      kv.py
      trace.py
      host.py                 # msgpack + extism call bridge

  ubiquitous-sdk-dotnet/      # Ubiquitous.Sdk NuGet package (guest-side)
    Ubiquitous.Sdk/
      Kv.cs
      Trace.cs
      Host.cs
    Ubiquitous.Sdk.csproj

  ubiquitous-sdk-zig/         # zig package (via build.zig.zon)
    src/
      root.zig
      kv.zig
      trace.zig
      host.zig

  ubiquitous-sdk-as/          # @ubiquitous/sdk-as (AssemblyScript)
    assembly/
      index.ts
      kv.ts
      trace.ts
      host.ts
```

### Build Order

1. **Define the msgpack envelope schema** — write it once in a canonical spec doc, then implement in each SDK
2. **TypeScript SDK first** — fastest iteration cycle; validates the envelope design
3. **Rust SDK** — reference implementation; most type-safe; will be used to validate correctness
4. **Go, Python, C#, Zig, AssemblyScript** — in priority order

### SDK Version Declaration

Each SDK package declares its version in package metadata:

```typescript
// TypeScript
export const SDK_VERSION = 102; // 1.02
```

```rust
// Rust
pub const SDK_VERSION: u16 = 102; // 1.02
```

This version is included in every envelope so the host can log warnings if plugins are stale.

---

## Integration Test Harness

### Per-Language Integration Test

Each language example gets an integration test that runs against the live Extism.NET host:

```
tests/integration/
  typescript/
    kv-trace-demo.test.ts       # calls the .wasm via the C# test host
  rust/
    kv_trace_demo_test.rs
  go/
    kv_trace_demo_test.go
  ...
```

### Test Scenarios (each language runs all)

| Scenario | Validates |
|----------|-----------|
| `kv.get("missing")` returns null | KV host function + null handling |
| `kv.set("k", "v")` then `kv.get("k")` returns `"v"` | KV round-trip via Garnet |
| `kv.list("prefix:")` returns correct keys | KV list with prefix filter |
| `trace.startSpan("name")` + `span.end()` | Trace span visible in Aspire OTLP |
| Host returns `sdk_version_too_new` when plugin version > host | Version rejection path |
| Old plugin (SDK v1.0) still works with SDK v1.2 host | Backward compatibility |
| `kv.set` with 1MB value succeeds | Resource limit boundary |
| `kv.set` with value > 1MB returns structured error | Resource limit enforcement |

### Aspire Trace Verification

Integration tests that verify spans need Aspire running. The test project will:

1. Start the Ubiquitous host via `WebApplicationFactory` with Aspire OTLP exporter configured to a test
   OpenTelemetry collector
2. Invoke the WASM function
3. Assert the trace collector received a span with the expected name and attributes

```csharp
// tests/integration/TraceIntegrationTest.cs
[Fact]
public async Task RunFunction_EmitsExpectedSpan()
{
    var result = await _host.InvokeAsync("kv-trace-demo", """{"name":"Charlie"}""");
    
    await _traceCollector.WaitForSpanAsync("kv-trace-demo", timeout: TimeSpan.FromSeconds(5));
    
    var span = _traceCollector.GetSpan("kv-trace-demo");
    Assert.Equal("greeting:Charlie", span.Attributes["greeting.key"]);
}
```

---

## Toolchain Reproducibility

### Docker-Based Builds

For each language, provide a `Dockerfile.build` so CI (and developers) can compile without installing
the language toolchain locally:

```dockerfile
# examples/rust/kv-trace-demo/Dockerfile.build
FROM rust:1.77-slim AS builder
RUN rustup target add wasm32-unknown-unknown
WORKDIR /src
COPY . .
RUN cargo build --target wasm32-unknown-unknown --release
RUN cp target/wasm32-unknown-unknown/release/*.wasm /output/

FROM scratch
COPY --from=builder /output/*.wasm /
```

The CI pipeline uses these Dockerfiles to produce the `.wasm` artifacts, which are then version-tagged
and checked into `examples/<language>/dist/` as binary fixtures for integration tests.

### CI Matrix

```yaml
# .github/workflows/pdk-integration.yml
strategy:
  matrix:
    language: [typescript, rust, go, python, dotnet, zig, assemblyscript]
    include:
      - language: typescript
        build_cmd: "npm run build"
        expected_max_wasm_kb: 2048
      - language: rust
        build_cmd: "cargo build --target wasm32-unknown-unknown --release"
        expected_max_wasm_kb: 200
      - language: go
        build_cmd: "tinygo build -target wasi ./..."
        expected_max_wasm_kb: 1024
      - language: python
        build_cmd: "extism-py build main.py -o output.wasm"
        expected_max_wasm_kb: 15360
      - language: dotnet
        build_cmd: "dotnet build -c Release /p:RuntimeIdentifier=wasi-wasm"
        expected_max_wasm_kb: 10240
      - language: zig
        build_cmd: "zig build-lib src/main.zig -target wasm32-freestanding -dynamic -O ReleaseSmall"
        expected_max_wasm_kb: 100
      - language: assemblyscript
        build_cmd: "npx asc assembly/index.ts --target release"
        expected_max_wasm_kb: 256
```

Each CI job:
1. Builds the `.wasm` artifact
2. Verifies output size is within `expected_max_wasm_kb`
3. Runs the integration test suite against a live Extism.NET host with Garnet + OTLP collector

---

## Milestone Breakdown

### Milestone 1 — Envelope & TypeScript SDK (Week 1–2)
- [ ] Define msgpack call/response envelope spec in `docs/SDK-ENVELOPE-SPEC.md`
- [ ] Implement `@ubiquitous/sdk` TypeScript shim v1.0
- [ ] Host-side: implement envelope parsing in `ubiquitous.functions` Extism host
- [ ] Implement `ubiq::kv_get`, `ubiq::kv_set`, `ubiq::kv_delete`, `ubiq::kv_list` host functions backed by Garnet
- [ ] Implement `ubiq::trace_start_span`, `ubiq::trace_end_span`, `ubiq::trace_set_attr` backed by OTLP
- [ ] TypeScript `kv-trace-demo` example compiling and passing integration tests
- [ ] Aspire trace verification test passing

### Milestone 2 — Rust & Go SDKs (Week 3–4)
- [ ] `ubiquitous-sdk` Rust crate v1.0 — all KV + trace functions
- [ ] `ubiquitous/sdk-go` module v1.0 — all KV + trace functions
- [ ] Rust `kv-trace-demo` example passing integration tests
- [ ] Go `kv-trace-demo` example passing integration tests
- [ ] CI matrix running for TypeScript, Rust, Go

### Milestone 3 — Python & C# Guest SDKs (Week 5–6)
- [ ] `ubiquitous-sdk` Python package v1.0
- [ ] `Ubiquitous.Sdk` NuGet package v1.0 (guest-side)
- [ ] Python and C# examples passing integration tests
- [ ] Document known limitations (Python bundle size, C# compilation requirements)

### Milestone 4 — Zig & AssemblyScript SDKs (Week 7–8)
- [ ] Zig `ubiquitous-sdk` package v1.0
- [ ] `@ubiquitous/sdk-as` AssemblyScript package v1.0
- [ ] Zig and AssemblyScript examples passing integration tests
- [ ] Full CI matrix green for all 7 languages

### Milestone 5 — Versioning Hardening (Week 9)
- [ ] Version floor enforcement in the Extism.NET host on plugin load
- [ ] Integration tests validating backward compatibility (old SDK v1.0 plugin against v1.2 host)
- [ ] Integration tests validating forward rejection (plugin SDK > host version)
- [ ] Plugin manifest `sdk_version` field parsed and validated at load time
- [ ] Upgrade guidance error message when plugin is below floor

### Milestone 6 — Documentation & Developer Guide (Week 10)
- [ ] `docs/SDK-ENVELOPE-SPEC.md` — complete spec with msgpack schema diagrams
- [ ] Per-language getting started guide in each SDK package README
- [ ] Update `docs/MODULE-SDK.md` to reflect the msgpack-based shim design
- [ ] Blog-style walkthrough: "Write a Ubiquitous function in 5 languages"

---

## Open Questions

1. **Is msgpack the right choice for all languages?** Python and Haskell have heavier msgpack library
   dependencies. Evaluate whether a simpler text-based format (CBOR, or even length-prefixed JSON) would
   be cleaner for those languages before committing to msgpack across the board. Decision needed in Milestone 1.

2. **Zig and AssemblyScript memory allocators**: both languages require explicit memory management for
   msgpack encode/decode. Need to decide whether the SDK shim uses a fixed-size stack allocator or requires
   the host to pass a per-call arena. Design this before Milestone 4.

3. **trace_start_span returns a span ID**: how is this ID managed across multiple host calls within a single
   plugin invocation? Options: (a) store in an Extism variable, (b) pass explicitly to trace_end_span and
   trace_set_attr, (c) implicit single-span per invocation. Option (b) is most explicit and works in all
   languages.

4. **SDK distribution**: should each SDK be published to its ecosystem package registry (npm, crates.io,
   pkg.go.dev, PyPI, NuGet) from day one, or kept internal until the API stabilises? Recommendation: keep
   unpublished through Milestone 5, publish after versioning hardening.

---

## References

- [Extism PDK Overview](https://extism.org/docs/category/write-a-plug-in)
- [Extism JS PDK](https://github.com/extism/js-pdk)
- [Extism Rust PDK](https://github.com/extism/rust-pdk)
- [Extism Go PDK](https://github.com/extism/go-pdk)
- [Extism Python PDK](https://github.com/extism/python-pdk)
- [Extism .NET PDK](https://github.com/extism/dotnet-pdk)
- [MessagePack spec](https://msgpack.org/index.html)
- [rmp-serde (Rust msgpack)](https://github.com/3Hren/msgpack-rust)
- [vmihailenco/msgpack (Go)](https://github.com/vmihailenco/msgpack)
- [Microsoft Garnet](https://github.com/microsoft/Garnet) — Embedded KV backing the `ubiq::kv_*` host functions
- [.NET Aspire OTLP](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/telemetry) — Trace span validation
