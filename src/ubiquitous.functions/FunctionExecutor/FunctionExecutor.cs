using System.Diagnostics;
using System.Net.Sockets;
using Extism.Sdk;
using Extism.Sdk.Native;
using ubiquitous.functions.FunctionExecutor;

namespace ubiquitous.functions.tests.unit;

public enum FunctionLifecycle
{
    Empty,         // default starting state
    Configuring,   // Configure is in-process
    Configured,    // Configure has completed
    Loading,       // Load is in-process
    Loaded,        // Load is completed, code hash is validated, plugin is constructed
    Initializing,  // Init has been called and is currently running
    Active,         // Init has completed and function is ready for invoke loop. This is the steady-state
    ShuttingDown,  // Shutdown was called (automatically or manually) and is currently in process
    Terminated     // Plugin has ended its lifecycle and is no longer usable, but exists for statistics purposes.
}

public class FunctionExecutorStatistics
{
    public Dictionary<FunctionLifecycle, long> LifecyclePhaseTimeMicroseconds { get; set; } =
        new Dictionary<FunctionLifecycle, long>();
    
    
}
public class FunctionExecutor: IFunctionExecutor
{

    private Plugin _plugin;

    private FunctionLifecycle _currentLifecycleState;
    
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
        var _plugin = new Plugin(manifest, new HostFunction[] { }, true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public void Init()
    {
        
        throw new NotImplementedException();
    }

    public string Call(string method, string input)
    {
        if (_currentLifecycleState != FunctionLifecycle.Active)
        {
            throw new PluginNotInitializedException("Function is in incorrect state to be Called.  A function cannot be called until it has been loaded and initialized.");
        }

        return _plugin.Call(method, input);
    }

    public void RegisterHostFunction()
    {
        throw new NotImplementedException();
    }

    public void Configure()
    {
        throw new NotImplementedException();
    }
}