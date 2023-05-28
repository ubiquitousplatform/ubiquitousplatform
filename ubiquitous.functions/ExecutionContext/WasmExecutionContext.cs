
using Extism.Sdk.Native;
using System.Text;

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

    private Context _context;
    private Plugin _plugin;

    public WasmExecutionContext(Byte[] source)
    {
        // TODO: Take a byte span since the plugin will copy the plugin code into its own memory space.  Verify that the plugin keeps working even when the span is changed.
        // This will save a byte array copy, but in that case we can't store the source privately and need to do anything we want with the source during the initialization
        // of this constructor.
        this.ExecutionContextType = ExecutionContextType.WASM;
        this._source = source;
        _context = new Context();
        _plugin = _context.CreatePlugin(_source, withWasi: true);
    }

    public async ValueTask DisposeAsync()
    {
        // TODO: test this is actually working.
        await this.ShutdownAsync();
        _plugin.Dispose();
        _context.Dispose();
        _plugin = null;
        _context = null;
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
            output = Encoding.UTF8.GetString(
                _plugin.CallFunction("count_vowels", Encoding.UTF8.GetBytes("Hello World!"))
            );
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

