use extism_pdk::*;

/// Baseline: takes input, returns it unchanged.
/// Measures pure WASM invocation overhead (enter sandbox, run trivial code, exit).
#[plugin_fn]
pub fn noop(input: String) -> FnResult<String> {
    Ok(input)
}

// Declare host function that the host must provide.
// The host will implement this as a trivial echo (return input unchanged)
// so we're measuring the FFI boundary crossing cost, not host-side logic.
#[host_fn]
extern "ExtismHost" {
    fn host_kv_get(input: String) -> String;
}

/// Calls the host function exactly 1 time.
/// Overhead vs noop = 1 host function round-trip.
#[plugin_fn]
pub fn host_call_1(input: String) -> FnResult<String> {
    let result = unsafe { host_kv_get(input)? };
    Ok(result)
}

/// Calls the host function exactly 10 times in sequence.
/// (host_call_10 - noop) / 10 = per-call FFI overhead.
/// Using 10 calls amplifies the signal above measurement noise.
#[plugin_fn]
pub fn host_call_10(input: String) -> FnResult<String> {
    let mut val = input;
    for _ in 0..10 {
        val = unsafe { host_kv_get(val)? };
    }
    Ok(val)
}
