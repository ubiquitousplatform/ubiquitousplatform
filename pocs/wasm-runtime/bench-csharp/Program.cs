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

BenchmarkRunner.Run<WasmBenchmarks>();

[MemoryDiagnoser]
public class WasmBenchmarks
{
    private byte[] _wasmBytes = null!;
    private Plugin _warmPlugin = null!;
    private HostFunction[] _hostFunctions = null!;

    /// Create a fresh set of host functions (HostFunction handles are consumed by Plugin creation).
    private static HostFunction[] CreateHostFunctions()
    {
        return new[]
        {
            HostFunction.FromMethod("host_kv_get", IntPtr.Zero,
                (CurrentPlugin plugin, long inputOffset) =>
                {
                    var bytes = plugin.ReadBytes(inputOffset).ToArray();
                    return plugin.WriteBytes(bytes);
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

        // Smoke test: make sure it works before benchmarking
        var result = _warmPlugin.Call("noop", "smoke-test");
        if (result != "smoke-test")
            throw new Exception($"Smoke test failed: got '{result}'");
    }

    // ── Cold start: compile + instantiate + call (measures module load cost) ───

    [Benchmark]
    public void ColdStartNoop()
    {
        // Create fresh host functions each iteration (handles are consumed by Plugin)
        var hostFns = CreateHostFunctions();
        var manifest = new Manifest(new ByteArrayWasmSource(_wasmBytes, "bench"));
        using var plugin = new Plugin(manifest, hostFns, withWasi: true);
        plugin.Call("noop", "test");
    }

    // ── Warm invocations (plugin already instantiated) ─────────────────────────

    [Benchmark(Baseline = true)]
    public string WarmNoop()
    {
        return _warmPlugin.Call("noop", "test");
    }

    [Benchmark]
    public string WarmHostCall1()
    {
        return _warmPlugin.Call("host_call_1", "test");
    }

    [Benchmark]
    public string WarmHostCall10()
    {
        return _warmPlugin.Call("host_call_10", "test");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _warmPlugin?.Dispose();
        foreach (var hf in _hostFunctions)
            hf?.Dispose();
    }
}
