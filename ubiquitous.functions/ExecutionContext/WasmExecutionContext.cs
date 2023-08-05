
using Extism.Sdk.Native;
using System.Text;
using Wasmtime;

namespace ubiquitous.functions;

public class WasmExecutionContext : IExecutionContext, IAsyncDisposable
{
    public ExecutionContextType ExecutionContextType { get; private set; }

    //private Command? _executingCommand;
    //private static readonly Shell _defaultCommandSettings = new Shell(options => options.ThrowOnError());
    private readonly Byte[] _source;
    private object _lockObj = new();
    private volatile bool _hasBeenClaimed = false;

    public bool ReserveContext()
    {
        if (_hasBeenClaimed) return false;
        var claimed = false;
        lock (_lockObj)
        {
            if (_hasBeenClaimed == false)
            {
                claimed = true;
                _hasBeenClaimed = true;
            }
        }
        return claimed;
    }

    public void ReleaseContext()
    {
        lock (_lockObj)
        {
            _hasBeenClaimed = false;
        }
    }

    private Engine engine;
    private Instance instance;

    static WasmExecutionContext()
    {
        // TODO: use IoC so we can do this after startup and reduce the startup time.
        System.Environment.SetEnvironmentVariable("WASMTIME_BACKTRACE_DETAILS", "1");


    }
    public WasmExecutionContext(Byte[] source)
    {

        engine = new Engine();

        // TODO: The linker depends on the store for the callback registration, so you have to create both of them on every invocation. can we optimize this?

        using var linker = new Linker(engine);
        // TODO: store can be initialized with data. how does this work? could we use this to our advantage for passing data to the wasm module?
        using var store = new Store(engine);


        // store.SetLimits
        // would be cool to make WASMTIME_BACKTRACE_DETAILS = 1, RUST_BACKTRACE = full, and RUST_BACKTRACE = 1 as valid options in configuration
        store.SetWasiConfiguration(new WasiConfiguration().WithEnvironmentVariables(new List<(string, string)> { ("UBIQUITOUS_RUNTIME_VERSION", "0.0.1"), ("WASMTIME_BACKTRACE_DETAILS", "1"), ("RUST_BACKTRACE", "full") }).WithInheritedStandardInput().WithInheritedStandardError().WithInheritedStandardOutput());

        // Set memory / CPU limits, fuel, epoch timeout, etc.

        // how do we do stdin/stdout/stderr? we can inherit them but then that seems unsafe, if we don't inherit can we use the file version safely?

        linker.DefineWasi();


        /*var responseExample = new Response() { ok = true, type = "LogResponse", payload = new LogResponse() { something = new() { "a", "b", "c" } } };

        var responseAsString = System.Text.Json.JsonSerializer.Serialize(responseExample);*/
        var responseAsString = "{ \"ok\":true,\"type\":\"LogResponse\",\"payload\":{\"something\":[\"a\",\"b\",\"c\"]}}";

        linker.Define(
            "ubiquitous_functions",
            "get_response_size",
            Function.FromCallback(store, () =>
            Encoding.ASCII.GetByteCount(responseAsString))
        );
        linker.Define(
            "ubiquitous_functions",
            "get_response",
            // GetInput has a preallocated memory size based on calling get_input_size, and it expects us to write to it
            Function.FromCallback(store, (Caller caller, int ptr) =>
            {
                // TODO: implement BufferManager
                Console.WriteLine($"Called get_response with value: {ptr}");
                //caller!.GetMemory("memory")!.WriteByte(ptr, System.Text.Encoding.ASCII.GetBytes(inputAsString)[0]);
                caller!.GetMemory("memory")!.WriteString(ptr, responseAsString, System.Text.Encoding.ASCII);
            })
        );

        linker.Define(
            "ubiquitous_functions",
            "invoke_json",
            Function.FromCallback(store, (Caller caller, int ptr, int size) =>
            {
                // TODO: we could consume fuel here to account for the overhead of the given function call
                var input = caller!.GetMemory("memory")!.ReadString(ptr, size);
                // TODO: all calls must require an ID so that we can properly trace the context? or do we track the ID of the instance automatically?

            })
        );


        //using var module = Module.FromFile(engine, "wasi-data-sharing.wasm");
        using var module = Module.FromFile(engine, "javy-example.wasm");
        instance = linker.Instantiate(store, module);

        //Console.WriteLine("Exports:");
        //module.Exports.ToList().ForEach(e => Console.WriteLine(e.Name));


        // TODO: Take a byte span since the plugin will copy the plugin code into its own memory space.  Verify that the plugin keeps working even when the span is changed.
        // This will save a byte array copy, but in that case we can't store the source privately and need to do anything we want with the source during the initialization
        // of this constructor.
        this.ExecutionContextType = ExecutionContextType.WASM;
        this._source = source;
    }

    public async ValueTask DisposeAsync()
    {
        // TODO: test this is actually working.
        await this.ShutdownAsync();
    }

    public async Task ShutdownAsync()
    {
        // TODO: if any event is currently being processed, let it finish first.
        // TODO: graceful shutdown by signaling interrupt and waiting a few secs.
        //_executingCommand.Kill();
    }

    // TODO: make the event response a type

    public async Task<string> HandleEventAsync(ExecutionEvent evt)
    {
        // TODO: save the context and plugin on this object, and release them when disposed, so we can reuse the plugin and context.
        //using Extism.Sdk.Native.Context context = new Extism.Sdk.Native.Context();
        //using var plugin = context.CreatePlugin(_source, withWasi: false);
        //TODO: call HTTP endpoint to test out http.also test out websockets, etc.
        string output = null;
        lock (_lockObj)
        {
            var run = instance.GetAction("_start")!;

            run();

            /*output = Encoding.UTF8.GetString(
                _plugin.CallFunction("count_vowels", Encoding.UTF8.GetBytes("Hello World!"))
            );*/
        }

        this.ReleaseContext();
        return output;
    }

    public async Task StartupAsync()
    {



        // TODO: create startup event with current time, env vars, etc.

        // TODO: verify response is a success.
        // TODO: cancel startup if it exceeds the timeout. make this configurable


    }
}


/* 

Features:
 - Create an instance of a runner
 - Runner should have a list of all functions and their versions
 - Each function+version should have a config (just like Lambda)
 - Store functions using ubiquitous.storage
 - Runtime for now will just be `deno run xxx`
 - Support input/output (from either Event system or from Background Runner)
  - Take inspiration from Lambda event format

Later
 - Enable default concurrency setting


*/

