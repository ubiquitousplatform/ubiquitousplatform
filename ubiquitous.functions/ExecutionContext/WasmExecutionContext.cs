
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

    public WasmExecutionContext(Byte[] source)
    {
        // Design Decision: could possibly keep source as a readonly span for zero copy, but downside would be the caller would own the memory and could
        // take it out from under us. we're taking the copy overhead so that we own our byte[]. If we made function pool guaranteed not to reallocate the memory
        // we could do this more cheaply and scalably.
        this.ExecutionContextType = ExecutionContextType.WASM;
        this._source = source;
    }

    public async ValueTask DisposeAsync()
    {
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
        using Extism.Sdk.Native.Context context = new Extism.Sdk.Native.Context();
        using var plugin = context.CreatePlugin(_source, withWasi: false);
        //TODO: call HTTP endpoint to test out http.also test out websockets, etc.

       var output = Encoding.UTF8.GetString(
           plugin.CallFunction("count_vowels", Encoding.UTF8.GetBytes("Hello World!"))
       );

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

