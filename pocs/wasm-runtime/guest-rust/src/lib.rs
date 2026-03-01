use extism_pdk::*;

const GUEST_MAGIC: u64 = 0x9E37_79B9_7F4A_7C15;
const HOST_REQ_MAGIC: u64 = 0xC2B2_AE3D_27D4_EB4F;
const HOST_RESP_MAGIC: u64 = 0x1656_67B1_9E37_79F9;

fn guest_mix(value: u64, round: u64) -> u64 {
    let x = value
        .wrapping_add(GUEST_MAGIC)
        .wrapping_add(round.wrapping_mul(0x1000_0000_01B3));
    x.rotate_left(((round as u32) % 31) + 1)
        ^ x.wrapping_mul(0xFF51_AFD7_ED55_8CCD)
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

fn parse_seed(input: String) -> FnResult<u64> {
    Ok(input
        .trim()
        .parse::<u64>()
        .map_err(|e| Error::msg(format!("invalid seed: {e}")) )?)
}

fn parse_triplet(input: &str, context: &str) -> FnResult<(u64, u64, u64)> {
    let mut parts = input.split(',');
    let a = parts
        .next()
        .ok_or_else(|| Error::msg(format!("{context}: missing first field")))?
        .parse::<u64>()
        .map_err(|e| Error::msg(format!("{context}: invalid first field: {e}")))?;
    let b = parts
        .next()
        .ok_or_else(|| Error::msg(format!("{context}: missing second field")))?
        .parse::<u64>()
        .map_err(|e| Error::msg(format!("{context}: invalid second field: {e}")))?;
    let c = parts
        .next()
        .ok_or_else(|| Error::msg(format!("{context}: missing third field")))?
        .parse::<u64>()
        .map_err(|e| Error::msg(format!("{context}: invalid third field: {e}")))?;

    if parts.next().is_some() {
        return Err(Error::msg(format!("{context}: too many fields")).into());
    }

    Ok((a, b, c))
}

fn call_verified_host(round: u64, value: u64) -> FnResult<u64> {
    let payload = format!("{round},{value},{}", req_tag(round, value));
    let response = unsafe { host_kv_get(payload)? };
    let (resp_round, resp_value, tag) = parse_triplet(&response, "host response")?;

    if resp_round != round {
        return Err(Error::msg(format!(
            "host response round mismatch: expected {round}, got {resp_round}"
        ))
        .into());
    }

    let expected_tag = resp_tag(round, resp_value);
    if tag != expected_tag {
        return Err(Error::msg(format!(
            "host response tag mismatch: expected {expected_tag}, got {tag}"
        ))
        .into());
    }

    Ok(resp_value)
}

/// Baseline: runs guest-only deterministic math.
/// Measures pure WASM invocation overhead plus minimal real guest compute.
#[plugin_fn]
pub fn noop(input: String) -> FnResult<String> {
    let mut value = parse_seed(input)?;
    for round in 0..10 {
        value = guest_mix(value, round);
    }
    Ok(value.to_string())
}

// Declare host function that the host must provide.
// The host validates request data, performs deterministic host-side math,
// and returns a signed payload.
#[host_fn]
extern "ExtismHost" {
    fn host_kv_get(input: String) -> String;
}

/// Calls the host function exactly 1 time.
/// Overhead vs noop = 1 host function round-trip.
#[plugin_fn]
pub fn host_call_1(input: String) -> FnResult<String> {
    let mut value = parse_seed(input)?;
    value = guest_mix(value, 0);
    value = call_verified_host(0, value)?;
    value = guest_mix(value, 10);
    Ok(value.to_string())
}

/// Calls the host function exactly 10 times in sequence.
/// (host_call_10 - noop) / 10 = per-call FFI overhead.
/// Using 10 calls amplifies the signal above measurement noise.
#[plugin_fn]
pub fn host_call_10(input: String) -> FnResult<String> {
    let mut value = parse_seed(input)?;
    for round in 0..10 {
        value = guest_mix(value, round);
        value = call_verified_host(round, value)?;
        value = guest_mix(value, round + 100);
    }
    Ok(value.to_string())
}

