# WASM Runtime Benchmark: Extism C# vs Extism Rust

> POC benchmarking the FFI overhead of calling WASM functions through Extism,
> comparing the C# (.NET) host SDK against the native Rust host SDK.

---

## Results (Apple M1 Pro, 2026-03-01, verified round-trip protocol)

### Rust Host (`extism` crate + `criterion`)

| Benchmark | Mean | Notes |
|---|---|---|
| **Cold start** (compile + instantiate + call) | **1,586 μs** | Module compilation dominates |
| **Warm noop** (guest math only) | **2.54 μs** | Baseline with deterministic guest compute |
| **Warm host_call_1** (1 verified host fn) | **4.36 μs** | 1 host round-trip + request/response validation |
| **Warm host_call_10** (10 verified host fns) | **20.25 μs** | 10 round-trips with host+guest compute each round |

### C# Host (`Extism.Sdk` + `BenchmarkDotNet`)

| Benchmark | Mean | Allocated | Notes |
|---|---|---|---|
| **Cold start** (compile + instantiate + call) | **3,176 μs** | 481 KB | ~2× Rust (GC + FFI setup) |
| **Warm noop** (guest math only) | **2.40 μs** | 104 B | Baseline with deterministic guest compute |
| **Warm host_call_1** (1 verified host fn) | **4.28 μs** | 632 B | 1 host round-trip + request/response validation |
| **Warm host_call_10** (10 verified host fns) | **23.83 μs** | 5,328 B | 10 round-trips with host+guest compute each round |

### Derived: Per-Host-Call FFI Overhead

```
Rust:  (20.25 − 2.54) / 10 = 1.77 μs per verified host call
C#:    (23.83 − 2.40) / 10 = 2.14 μs per verified host call
```

**C# is ~1.21× slower per verified host call** due to the additional managed→native→WASM→native→managed
boundary and higher managed allocation pressure.

### Key Takeaways

1. **Warm invocation baseline remains nearly identical** (2.40 μs C# vs 2.54 μs Rust)
   The WASM sandbox entry/exit cost dominates, not the host language.

2. **Verified host function calls add ~1.77 μs (Rust) or ~2.14 μs (C#) each.**
   This now includes boundary crossing + lightweight host/guest math + payload signature checks,
   so it is a stricter and more trustworthy upper-bound than the previous echo-only micro-benchmark.

3. **Cold start is still about 2× slower in C#** (3,176 μs vs 1,586 μs). This matters for the first
   invocation but not for warmed pools. The C# cold start also allocates 481 KB of managed memory.

4. **C# allocates 104 B per warm noop and 5.3 KB per 10-host-call benchmark** due to managed wrappers
   and string payload handling in the verified protocol.
   Gen0 collections are light and won't cause GC pauses at this allocation rate.

5. **The host-language delta remains small in absolute terms.** A ~0.37 μs difference per verified call
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

In this revised benchmark, the host callback is intentionally **not** a pure echo:
- guest sends `round,value,tag`
- host validates tag, performs deterministic math, returns `round,newValue,tag`
- guest validates host tag before continuing

This proves data is crossing both directions correctly and that both sides execute compute.
So the result is no longer "raw boundary only"; it is "boundary + lightweight verified work".

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
| `noop` | Runs deterministic guest-only math | Baseline (0 host calls) |
| `host_call_1` | 1 verified round-trip guest→host→guest | 1 host call overhead with validation |
| `host_call_10` | 10 verified round-trips | 10 host calls (amplifies signal) |

The guest declares a host function import `host_kv_get(String) → String`.
Both benchmarks register a host callback that validates request signatures,
applies deterministic host math, and signs responses.

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
