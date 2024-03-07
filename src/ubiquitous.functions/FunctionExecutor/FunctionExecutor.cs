using System.Diagnostics;
using Extism.Sdk;
using ubiquitous.functions.FunctionExecutor;
using ubiquitous.stdlib;

namespace ubiquitous.functions.tests.unit;

public enum FunctionLifecycle
{
    Empty, // default starting state
    Configuring, // Configure is in-process
    Configured, // Configure has completed
    Loading, // Load is in-process
    Loaded, // Load is completed, code hash is validated, plugin is constructed
    Initializing, // Init has been called and is currently running
    Active, // Init has completed and function is ready for invoke loop. This is the steady-state
    ShuttingDown, // Shutdown was called (automatically or manually) and is currently in process
    Terminated // Plugin has ended its lifecycle and is no longer usable, but exists for statistics purposes.
}

public record ExecutionMetrics
{
    // Other metrics? host function invocations?
    // Push metrics to Metrics endpoint in OTLP?
    public long ExecutionTimeMicroseconds { get; set; }
    public long StartUTC { get; set; }
    public long EndUTC { get; set; }

    public FunctionLifecycle LifecycleState { get; set; }
}

public class FunctionExecutorStatistics
{
    public List<ExecutionMetrics> LifecyclePhaseMetrics { get; set; } = new();
}

public class FunctionExecutor : IFunctionExecutor
{
    private FunctionLifecycle _currentLifecycleState;


    private Plugin _plugin;

    public FunctionExecutorStatistics LifecycleStatistics = new();

    public void Load(byte[] pluginBytes)
    {
        var timer = Stopwatch.StartNew();

        // TODO: there should be a Function Factory that returns plugins so that a given plugin only needs to be newed up once...
        // assuming they can all share a manifest.
        // does anything in the manifest need to be dynamic? eg. the timeout?

        // TODO: set name dynamically
        var manifest =
            new Manifest(
                new ByteArrayWasmSource(pluginBytes, "hello"));
        //var manifest = new Manifest(new PathWasmSource("test-harness.wasm", "test-harness"));
        // TODO: register timeout 
        manifest.Timeout = TimeSpan.FromSeconds(5);
        try
        {
            _plugin = new Plugin(manifest, new HostFunction[] { }, true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        _currentLifecycleState = FunctionLifecycle.Loaded;
    }

    public string Call(string method, string input)
    {
        var (metrics, sw) = StartLifecyclePhase(FunctionLifecycle.Active, "Call");
        var result = _plugin.Call(method, input);
        EndLifecyclePhase(metrics, sw);
        return result;
    }

    public void RegisterHostFunction()
    {
        throw new NotImplementedException();
    }

    public void Configure()
    {
        Console.WriteLine("TODO: pass in config and set up plugin");
    }

    public string Init(string input)
    {
        var (metrics, sw) = StartLifecyclePhase(FunctionLifecycle.Loaded, "Init", FunctionLifecycle.Initializing);
        var result = _plugin.Call("_init", input);
        EndLifecyclePhase(metrics, sw, FunctionLifecycle.Active);
        return result;
    }

    private void EndLifecyclePhase(ExecutionMetrics metrics, Stopwatch sw, FunctionLifecycle? destinationState = null)
    {
        metrics.ExecutionTimeMicroseconds = sw.ElapsedMicroseconds();
        metrics.EndUTC = Timestamp.UtcMs;
        if (destinationState != _currentLifecycleState)
        {
            metrics.LifecycleState = _currentLifecycleState;
            // if the state has transitioned, then report metrics
            LifecycleStatistics.LifecyclePhaseMetrics.Add(metrics);
        }

        _currentLifecycleState = destinationState ?? _currentLifecycleState;
    }

    private (ExecutionMetrics metrics, Stopwatch sw) StartLifecyclePhase(FunctionLifecycle expectedState,
        string phaseDesc, FunctionLifecycle? transitoryState = null)
    {
        if (_currentLifecycleState != expectedState)
            throw new PluginStateIncorrectException(
                $"Function is in incorrect state to be {phaseDesc}ed.  Expected phase: {expectedState}, Actual phase: {_currentLifecycleState}.");
        var metrics = new ExecutionMetrics();
        metrics.StartUTC = Timestamp.UtcMs;
        var sw = Stopwatch.StartNew();
        _currentLifecycleState = transitoryState ?? _currentLifecycleState;
        return (metrics, sw);
    }
}