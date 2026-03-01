// Benchmarks comparing WASM invocation overhead using the Extism C# SDK.
//
// Timing approach:
//   All timing is done HOST-SIDE by BenchmarkDotNet's high-resolution clock.
//   We never try to time from inside the WASM sandbox.
//
//   Three guest functions:
//     - noop: echo input (baseline — pure WASM call overhead)
//     - host_call_1: calls a host function once
//     - host_call_10: calls a host function 10 times
//
//   Per-call FFI overhead ≈ (host_call_10 − noop) / 10

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Extism.Sdk;
using System.Numerics;

BenchmarkRunner.Run<WasmBenchmarks>();

[MemoryDiagnoser]
public class WasmBenchmarks
{
    private const string InputSeed = "123456789";
    private const ulong GuestMagic = 0x9E3779B97F4A7C15UL;
    private const ulong HostReqMagic = 0xC2B2AE3D27D4EB4FUL;
    private const ulong HostRespMagic = 0x165667B19E3779F9UL;

    private byte[] _wasmBytes = null!;
    private Plugin _warmPlugin = null!;
    private HostFunction[] _hostFunctions = null!;

    private static ulong GuestMix(ulong value, ulong round)
    {
        var x = value + GuestMagic + (round * 0x1000000001B3UL);
        return BitOperations.RotateLeft(x, (int)(round % 31) + 1)
               ^ (x * 0xFF51AFD7ED558CCDUL);
    }

    private static ulong HostMix(ulong value, ulong round)
    {
        var x = value * 1_664_525UL + 1_013_904_223UL + (round * 17UL);
        return BitOperations.RotateLeft(x, 9) ^ 0xA24BAED4963EE407UL;
    }

    private static ulong RequestTag(ulong round, ulong value)
    {
        return BitOperations.RotateLeft(value + HostReqMagic + (round * 97UL), 11);
    }

    private static ulong ResponseTag(ulong round, ulong value)
    {
        return BitOperations.RotateLeft(value + HostRespMagic + (round * 131UL), 7);
    }

    private static (ulong A, ulong B, ulong C) ParseTriplet(string input, string context)
    {
        var parts = input.Split(',');
        if (parts.Length != 3)
            throw new InvalidOperationException($"{context}: expected 3 fields, got {parts.Length}");

        if (!ulong.TryParse(parts[0], out var a))
            throw new InvalidOperationException($"{context}: invalid first field '{parts[0]}'");
        if (!ulong.TryParse(parts[1], out var b))
            throw new InvalidOperationException($"{context}: invalid second field '{parts[1]}'");
        if (!ulong.TryParse(parts[2], out var c))
            throw new InvalidOperationException($"{context}: invalid third field '{parts[2]}'");

        return (a, b, c);
    }

    private static ulong ModelNoop(ulong seed)
    {
        var value = seed;
        for (ulong round = 0; round < 10; round++)
            value = GuestMix(value, round);
        return value;
    }

    private static ulong ModelHostCall1(ulong seed)
    {
        var value = seed;
        value = GuestMix(value, 0);
        value = HostMix(value, 0);
        value = GuestMix(value, 10);
        return value;
    }

    private static ulong ModelHostCall10(ulong seed)
    {
        var value = seed;
        for (ulong round = 0; round < 10; round++)
        {
            value = GuestMix(value, round);
            value = HostMix(value, round);
            value = GuestMix(value, round + 100);
        }

        return value;
    }

    private void VerifyPlugin()
    {
        foreach (var seed in new[] { 1UL, 42UL, 123_456_789UL })
        {
            var input = seed.ToString();
            var noop = _warmPlugin.Call("noop", input);
            if (noop != ModelNoop(seed).ToString())
                throw new InvalidOperationException($"noop mismatch for seed {seed}: got {noop}");

            var one = _warmPlugin.Call("host_call_1", input);
            if (one != ModelHostCall1(seed).ToString())
                throw new InvalidOperationException($"host_call_1 mismatch for seed {seed}: got {one}");

            var ten = _warmPlugin.Call("host_call_10", input);
            if (ten != ModelHostCall10(seed).ToString())
                throw new InvalidOperationException($"host_call_10 mismatch for seed {seed}: got {ten}");
        }
    }

    /// Create a fresh set of host functions (HostFunction handles are consumed by Plugin creation).
    private static HostFunction[] CreateHostFunctions()
    {
        return new[]
        {
            HostFunction.FromMethod("host_kv_get", IntPtr.Zero,
                (CurrentPlugin plugin, long inputOffset) =>
                {
                    var payload = plugin.ReadString(inputOffset);
                    var (round, value, tag) = ParseTriplet(payload, "host request");

                    var expectedTag = RequestTag(round, value);
                    if (tag != expectedTag)
                        throw new InvalidOperationException(
                            $"host request tag mismatch: expected {expectedTag}, got {tag}");

                    var outputValue = HostMix(value, round);
                    var response = $"{round},{outputValue},{ResponseTag(round, outputValue)}";
                    return plugin.WriteString(response);
                })
        };
    }

    [GlobalSetup]
    public void Setup()
    {
        _wasmBytes = File.ReadAllBytes("bench_guest.wasm");

        // Pre-create one plugin for warm benchmarks (consumes its own set of host functions)
        _hostFunctions = CreateHostFunctions();
        var manifest = new Manifest(new ByteArrayWasmSource(_wasmBytes, "bench"));
        _warmPlugin = new Plugin(manifest, _hostFunctions, withWasi: true);

        VerifyPlugin();
    }

    // ── Cold start: compile + instantiate + call (measures module load cost) ───

    [Benchmark]
    public void ColdStartNoop()
    {
        // Create fresh host functions each iteration (handles are consumed by Plugin)
        var hostFns = CreateHostFunctions();
        var manifest = new Manifest(new ByteArrayWasmSource(_wasmBytes, "bench"));
        using var plugin = new Plugin(manifest, hostFns, withWasi: true);
        plugin.Call("noop", InputSeed);

        foreach (var hf in hostFns)
            hf.Dispose();
    }

    // ── Warm invocations (plugin already instantiated) ─────────────────────────

    [Benchmark(Baseline = true)]
    public string WarmNoop()
    {
        return _warmPlugin.Call("noop", InputSeed);
    }

    [Benchmark]
    public string WarmHostCall1()
    {
        return _warmPlugin.Call("host_call_1", InputSeed);
    }

    [Benchmark]
    public string WarmHostCall10()
    {
        return _warmPlugin.Call("host_call_10", InputSeed);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _warmPlugin?.Dispose();
        foreach (var hf in _hostFunctions)
            hf?.Dispose();
    }
}
