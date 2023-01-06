
namespace ubiquitous.functions;

internal class WasmExecutionContext : IExecutionContext, IAsyncDisposable
{
    public ExecutionContextType ExecutionContextType { get; private set; }

    private Command? _executingCommand;
    private static readonly Shell _defaultCommandSettings = new Shell(options => options.ThrowOnError());
    private readonly string _source;
    private object _lockObj;
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


    public WasmExecutionContext(string source)
    {
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
        _executingCommand.Kill();
    }

    public async Task HandleEventAsync(ExecutionEvent evt)
    {
        // Convert event to JSON
        await _executingCommand.StandardInput.WriteAsync("hi");
        // When the event is over, release the claim.
        this.ReleaseContext();
    }

    public async Task StartupAsync()
    {
        // TODO: don't require making a temp file.  We had to do this because
        // in 'deno run -' mode, to start the program, it has to receive an EOF
        // and you can't write that from the stream,  you have to close the stream,
        // which will prevent us from using STDIN/STDOUT for IPC.
        // TODO: create startup event with current time, env vars, etc.

        // Start up a new deno process in "STDIN" mode to read the source from STDIN.
        //var fileName = Guid.NewGuid() + ".js";
        //await File.WriteAllTextAsync(fileName, this._source);
        //var command = _defaultCommandSettings.Run("deno", "run", fileName);
        var command = _defaultCommandSettings.Run("deno", "run", "-");
        //var result = command.Result;
        char[] eof = new char[1] { (char)0x00 };
        // Send the source code with an EOF so deno will execute it.
        CancellationToken init_token = new CancellationToken();
        await command.StandardInput.WriteAsync(_source.ToCharArray(), init_token);
        await command.StandardInput.WriteAsync(eof, init_token);
        //command.StandardInput.Close();
        // Wait until startup complete shows up on stdout of the process.
        var response = await command.StandardOutput.ReadLineAsync();
        command.StandardInput.Write("hello again!");
        _executingCommand = command;
        // TODO: verify response is a success.
        // TODO: cancel startup if it exceeds the timeout. make this configurable



        //// inspect the result
        //if (!result.Success)
        //{
        //    Console.Error.WriteLine($"command failed with exit code {result.ExitCode}: {result.StandardError}");
        //}
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

