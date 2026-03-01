# WASM Runtime Benchmark: Extism C# vs Extism Rust

> POC benchmarking the FFI overhead of calling WASM functions through Extism,
> comparing the C# (.NET) host SDK against the native Rust host SDK.

---

## Results (Apple M1 Pro, 2026-03-01)

### Rust Host (`extism` crate + `criterion`)

| Benchmark | Mean | Notes |
|---|---|---|
| **Cold start** (compile + instantiate + call) | **1,483 μs** | Module compilation dominates |
| **Warm noop** (pure WASM call) | **2.43 μs** | Baseline — entering/exiting sandbox |
| **Warm host_call_1** (1 host fn) | **3.27 μs** | 1 host function round-trip |
| **Warm host_call_10** (10 host fns) | **9.00 μs** | 10 host function round-trips |

### C# Host (`Extism.Sdk` + `BenchmarkDotNet`)

| Benchmark | Mean | Allocated | Notes |
|---|---|---|---|
| **Cold start** (compile + instantiate + call) | **2,987 μs** | 457 KB | 2× Rust (GC + FFI setup) |
| **Warm noop** (pure WASM call) | **2.28 μs** | 64 B | Baseline — nearly identical to Rust |
| **Warm host_call_1** (1 host fn) | **3.30 μs** | 128 B | 1 host function round-trip |
| **Warm host_call_10** (10 host fns) | **13.10 μs** | 704 B | 10 host function round-trips |

### Derived: Per-Host-Call FFI Overhead

```
Rust:  (9.00 − 2.43) / 10 = 0.66 μs per host function call
C#:    (13.10 − 2.28) / 10 = 1.08 μs per host function call
```

**C# is 1.6× slower per host function call** due to the additional managed→native→WASM→native→managed
boundary vs Rust's native→WASM→native path.

### Key Takeaways

1. **Warm invocation overhead is nearly identical** (2.28 μs C# vs 2.43 μs Rust).
   The WASM sandbox entry/exit cost dominates, not the host language.

2. **Host function calls add ~0.66 μs (Rust) or ~1.08 μs (C#) each.**
   For a function making 5 host calls, that's 3.3 μs (Rust) or 5.4 μs (C#) of FFI overhead —
   negligible compared to any real work (network I/O, KV lookup, etc.).

3. **Cold start is 2× slower in C#** (2,987 μs vs 1,483 μs). This matters for the first
   invocation but not for warmed pools. The C# cold start also allocates 457 KB of managed memory.

4. **C# allocates 64 B per warm call** (Extism SDK managed wrappers).
   Gen0 collections are light and won't cause GC pauses at this allocation rate.

5. **The FFI overhead is NOT a blocker for C#.** A 0.42 μs difference per host call
   is dwarfed by any real host function work (KV store lookup: ~10-100 μs,
   HTTP call: ~1-100 ms). The ecosystem advantages of C# (Garnet, SignalR, Aspire)
   far outweigh this micro-overhead.

---

## How Timing Works (The FFI Measurement Problem)

**Q: How do you measure FFI time if getting the clock IS an FFI call?**

**A: You don't measure from inside WASM. All timing is host-side.**

The host (C# or Rust) has direct access to high-resolution OS clocks:
- C#: `Stopwatch.GetTimestamp()` (used by BenchmarkDotNet)
- Rust: `std::time::Instant` (used by criterion)

We time the entire `plugin.call()` from the host's perspective, then subtract:

```
per_host_call = (time_for_10_host_calls − time_for_0_host_calls) / 10
```

The `noop` function (0 host calls) captures the baseline cost of entering/exiting
the WASM sandbox. The `host_call_10` function adds exactly 10 host function
round-trips on top. The difference isolates the FFI boundary crossing cost.

Using 10 calls per iteration amplifies the signal above measurement noise
(criterion's timer resolution is ~1 ns).

The host function itself is a trivial echo (read input bytes → write them back),
so host-side execution time is negligible vs the boundary crossing cost.

---

## Project Structure

```
pocs/wasm-runtime/
├── README.md                  ← you are here
├── build-guest.sh             ← builds the guest WASM module
├── wasm/
│   └── bench_guest.wasm       ← 88 KB, pre-built guest module
├── guest-rust/                ← Extism plugin (Rust → WASM)
│   ├── Cargo.toml
│   ├── .cargo/config.toml     ← sets target to wasm32-unknown-unknown
│   └── src/lib.rs             ← noop, host_call_1, host_call_10
├── bench-rust/                ← Rust host benchmark (criterion)
│   ├── Cargo.toml
│   ├── src/lib.rs
│   └── benches/wasm_bench.rs
└── bench-csharp/              ← C# host benchmark (BenchmarkDotNet)
    ├── bench-csharp.csproj
    └── Program.cs
```

### Guest WASM Module (`guest-rust/`)

Three exported functions, all using the Extism PDK:

| Function | Behavior | Purpose |
|---|---|---|
| `noop` | Echo input → output | Baseline (0 host calls) |
| `host_call_1` | Call `host_kv_get` once | 1 host call overhead |
| `host_call_10` | Call `host_kv_get` 10× | 10 host calls (amplifies signal) |

The guest declares a host function import `host_kv_get(String) → String` which
the host must provide. Both benchmarks register this as a trivial echo.

---

## How To Run

### Prerequisites

```bash
# Rust toolchain with WASM target
rustup target add wasm32-unknown-unknown

# .NET 8+ SDK
dotnet --version
```

### 1. Build the guest WASM module

```bash
./build-guest.sh
```

### 2. Run the Rust benchmark

```bash
cd bench-rust
cargo bench
```

Results appear in the terminal and in `target/criterion/`.

### 3. Run the C# benchmark

```bash
cd bench-csharp
dotnet run -c Release
```

Results appear in the terminal and in `BenchmarkDotNet.Artifacts/results/`.

---

## Benchmark Details

### Cold Start
- Creates a brand-new `Plugin` from raw WASM bytes (compilation + instantiation)
- Calls `noop` once
- Disposes the plugin
- Measures the full "first request" cost

### Warm Noop
- Uses a pre-created, long-lived `Plugin` instance
- Calls the `noop` function which echoes input back
- Measures pure WASM invocation overhead (sandbox entry → trivial work → sandbox exit)

### Warm Host Call (1 and 10)
- Uses the same pre-created `Plugin`
- Guest function calls the `host_kv_get` host function N times
- Each host call: guest suspends → Extism reads guest memory → host callback runs → Extism writes result → guest resumes
- The difference vs noop isolates the host function FFI overhead
