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

const INPUT_SEED: &str = "123456789";
const GUEST_MAGIC: u64 = 0x9E37_79B9_7F4A_7C15;
const HOST_REQ_MAGIC: u64 = 0xC2B2_AE3D_27D4_EB4F;
const HOST_RESP_MAGIC: u64 = 0x1656_67B1_9E37_79F9;

/// Path to the pre-built guest WASM module.
fn wasm_path() -> PathBuf {
    PathBuf::from(env!("CARGO_MANIFEST_DIR")).join("../wasm/bench_guest.wasm")
}

fn guest_mix(value: u64, round: u64) -> u64 {
    let x = value
        .wrapping_add(GUEST_MAGIC)
        .wrapping_add(round.wrapping_mul(0x1000_0000_01B3));
    x.rotate_left(((round as u32) % 31) + 1)
        ^ x.wrapping_mul(0xFF51_AFD7_ED55_8CCD)
}

fn host_mix(value: u64, round: u64) -> u64 {
    let x = value
        .wrapping_mul(1_664_525)
        .wrapping_add(1_013_904_223)
        .wrapping_add(round.wrapping_mul(17));
    x.rotate_left(9) ^ 0xA24B_AED4_963E_E407
}

fn req_tag(round: u64, value: u64) -> u64 {
    value
        .wrapping_add(HOST_REQ_MAGIC)
        .wrapping_add(round.wrapping_mul(97))
        .rotate_left(11)
}

fn resp_tag(round: u64, value: u64) -> u64 {
    value
        .wrapping_add(HOST_RESP_MAGIC)
        .wrapping_add(round.wrapping_mul(131))
        .rotate_left(7)
}

fn parse_triplet(input: &str, context: &str) -> Result<(u64, u64, u64), String> {
    let mut parts = input.split(',');
    let a = parts
        .next()
        .ok_or_else(|| format!("{context}: missing first field"))?
        .parse::<u64>()
        .map_err(|e| format!("{context}: invalid first field: {e}"))?;
    let b = parts
        .next()
        .ok_or_else(|| format!("{context}: missing second field"))?
        .parse::<u64>()
        .map_err(|e| format!("{context}: invalid second field: {e}"))?;
    let c = parts
        .next()
        .ok_or_else(|| format!("{context}: missing third field"))?
        .parse::<u64>()
        .map_err(|e| format!("{context}: invalid third field: {e}"))?;
    if parts.next().is_some() {
        return Err(format!("{context}: too many fields"));
    }
    Ok((a, b, c))
}

fn model_noop(seed: u64) -> u64 {
    let mut value = seed;
    for round in 0..10 {
        value = guest_mix(value, round);
    }
    value
}

fn model_host_call_1(seed: u64) -> u64 {
    let mut value = seed;
    value = guest_mix(value, 0);
    value = host_mix(value, 0);
    value = guest_mix(value, 10);
    value
}

fn model_host_call_10(seed: u64) -> u64 {
    let mut value = seed;
    for round in 0..10 {
        value = guest_mix(value, round);
        value = host_mix(value, round);
        value = guest_mix(value, round + 100);
    }
    value
}

fn verify_plugin(plugin: &mut Plugin) {
    let seeds = [1_u64, 42_u64, 123_456_789_u64];
    for seed in seeds {
        let input = seed.to_string();

        let noop: String = plugin.call("noop", input.as_str()).unwrap();
        assert_eq!(noop, model_noop(seed).to_string(), "noop mismatch for seed {seed}");

        let one: String = plugin.call("host_call_1", input.as_str()).unwrap();
        assert_eq!(
            one,
            model_host_call_1(seed).to_string(),
            "host_call_1 mismatch for seed {seed}"
        );

        let ten: String = plugin.call("host_call_10", input.as_str()).unwrap();
        assert_eq!(
            ten,
            model_host_call_10(seed).to_string(),
            "host_call_10 mismatch for seed {seed}"
        );
    }
}

// Host callback validates guest payload, performs deterministic host math,
// and signs the response so the guest can verify it.
host_fn!(host_kv_get_impl(_user_data: (); input: String) -> String {
    let (round, value, tag) = parse_triplet(&input, "host request")
        .map_err(|e| Error::msg(e))?;

    let expected = req_tag(round, value);
    if tag != expected {
        return Err(Error::msg(format!(
            "host request tag mismatch: expected {expected}, got {tag}"
        )));
    }

    let out = host_mix(value, round);
    Ok(format!("{round},{out},{}", resp_tag(round, out)))
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

    // One-time correctness check before timing.
    {
        let mut plugin = create_plugin(&wasm_bytes);
        verify_plugin(&mut plugin);
    }

    c.bench_function("rust_cold_start_noop", |b| {
        b.iter(|| {
            let mut plugin = create_plugin(black_box(&wasm_bytes));
            let _r: String = plugin.call("noop", INPUT_SEED).unwrap();
        });
    });
}

// ── Warm invocations (plugin already instantiated) ──────────────────────────

fn bench_warm_noop(c: &mut Criterion) {
    let wasm_bytes = std::fs::read(wasm_path()).unwrap();
    let mut plugin = create_plugin(&wasm_bytes);
    verify_plugin(&mut plugin);

    c.bench_function("rust_warm_noop", |b| {
        b.iter(|| {
            let _r: String = plugin.call("noop", black_box(INPUT_SEED)).unwrap();
        });
    });
}

fn bench_warm_host_call_1(c: &mut Criterion) {
    let wasm_bytes = std::fs::read(wasm_path()).unwrap();
    let mut plugin = create_plugin(&wasm_bytes);
    verify_plugin(&mut plugin);

    c.bench_function("rust_warm_host_call_1", |b| {
        b.iter(|| {
            let _r: String = plugin.call("host_call_1", black_box(INPUT_SEED)).unwrap();
        });
    });
}

fn bench_warm_host_call_10(c: &mut Criterion) {
    let wasm_bytes = std::fs::read(wasm_path()).unwrap();
    let mut plugin = create_plugin(&wasm_bytes);
    verify_plugin(&mut plugin);

    c.bench_function("rust_warm_host_call_10", |b| {
        b.iter(|| {
            let _r: String = plugin.call("host_call_10", black_box(INPUT_SEED)).unwrap();
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
