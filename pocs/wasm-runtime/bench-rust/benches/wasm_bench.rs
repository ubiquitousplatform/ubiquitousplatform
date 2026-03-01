//! Benchmarks comparing WASM invocation overhead using the Extism Rust SDK.
//!
//! Timing approach:
//!   All timing is done HOST-SIDE by criterion's high-resolution clock.
//!   We never try to time from inside the WASM sandbox (which would require
//!   an FFI call to read the clock — the very thing we're trying to measure).
//!
//!   Instead we run three exported functions:
//!     - `noop`: takes input, returns it unchanged (baseline)
//!     - `host_call_1`: calls a host function once
//!     - `host_call_10`: calls a host function 10 times
//!
//!   Per-call FFI overhead ≈ (host_call_10 − noop) / 10
//!   Using 10 calls amplifies the signal above timer noise.

use criterion::{black_box, criterion_group, criterion_main, Criterion};
use extism::*;
use std::path::PathBuf;

/// Path to the pre-built guest WASM module.
fn wasm_path() -> PathBuf {
    PathBuf::from(env!("CARGO_MANIFEST_DIR")).join("../wasm/bench_guest.wasm")
}

// The host function the guest calls.  It echoes input back unchanged,
// so we measure only the boundary-crossing cost, not host-side logic.
host_fn!(host_kv_get_impl(_user_data: (); input: String) -> String {
    Ok(input)
});

/// Create a Plugin with the host function registered, ready to call.
fn create_plugin(wasm_bytes: &[u8]) -> Plugin {
    let f = Function::new(
        "host_kv_get",
        [PTR],
        [PTR],
        UserData::new(()),
        host_kv_get_impl,
    );
    let manifest = Manifest::new([Wasm::data(wasm_bytes.to_vec())]);
    Plugin::new(&manifest, [f], true).unwrap()
}

// ── Cold start: compile module + instantiate + call ─────────────────────────

fn bench_cold_start(c: &mut Criterion) {
    let wasm_bytes = std::fs::read(wasm_path()).expect("bench_guest.wasm not found — run build-guest.sh first");

    c.bench_function("rust_cold_start_noop", |b| {
        b.iter(|| {
            let mut plugin = create_plugin(black_box(&wasm_bytes));
            let _r: String = plugin.call("noop", "test").unwrap();
        });
    });
}

// ── Warm invocations (plugin already instantiated) ──────────────────────────

fn bench_warm_noop(c: &mut Criterion) {
    let wasm_bytes = std::fs::read(wasm_path()).unwrap();
    let mut plugin = create_plugin(&wasm_bytes);

    c.bench_function("rust_warm_noop", |b| {
        b.iter(|| {
            let _r: String = plugin.call("noop", black_box("test")).unwrap();
        });
    });
}

fn bench_warm_host_call_1(c: &mut Criterion) {
    let wasm_bytes = std::fs::read(wasm_path()).unwrap();
    let mut plugin = create_plugin(&wasm_bytes);

    c.bench_function("rust_warm_host_call_1", |b| {
        b.iter(|| {
            let _r: String = plugin.call("host_call_1", black_box("test")).unwrap();
        });
    });
}

fn bench_warm_host_call_10(c: &mut Criterion) {
    let wasm_bytes = std::fs::read(wasm_path()).unwrap();
    let mut plugin = create_plugin(&wasm_bytes);

    c.bench_function("rust_warm_host_call_10", |b| {
        b.iter(|| {
            let _r: String = plugin.call("host_call_10", black_box("test")).unwrap();
        });
    });
}

criterion_group!(
    benches,
    bench_cold_start,
    bench_warm_noop,
    bench_warm_host_call_1,
    bench_warm_host_call_10,
);
criterion_main!(benches);
