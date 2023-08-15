// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Wasmtime;
using WasmtimeExamples;

Console.WriteLine("Hello, World!");

Console.WriteLine("Calling javy-example");

// Define wasmtime store and engine
// TODO: look into Epoch and timeouts
// TODO: measure fuel
// TODO: measure memory usage
System.Environment.SetEnvironmentVariable("WASMTIME_BACKTRACE_DETAILS", "1");


// .WithEpochInterruption(true).WithFuelConsumption(true).WithDebugInfo(true)

// For future reference:
// Multi Memory can't be used because rust doesn't work https://github.com/rust-lang/rust/issues/73755
// Multi Value can't be used because you have to use cargo wasi sdk to build and it's just recently kinda working. https://github.com/rust-lang/rust/issues/73755
using var engine = new Engine(new Config().WithFuelConsumption(true)); // Can this engine be reused?




using var linker = new Linker(engine); // Can this linker be reused?
using var store = new Store(engine);// TODO: store can be initialized with data. how does this work? could we use this to our advantage for passing data to the wasm module?


store.AddFuel(1000000000);
//store.SetEpochDeadline(1000000000);

// store.SetLimits
// would be cool to make WASMTIME_BACKTRACE_DETAILS = 1, RUST_BACKTRACE = full, and RUST_BACKTRACE = 1 as valid options in configuration
store.SetWasiConfiguration(new WasiConfiguration().WithEnvironmentVariables(new List<(string, string)> { ("UBIQUITOUS_RUNTIME_VERSION", "0.0.1"), ("WASMTIME_BACKTRACE_DETAILS", "1"), ("RUST_BACKTRACE", "full") }).WithInheritedStandardInput().WithInheritedStandardError().WithInheritedStandardOutput());

// Set memory / CPU limits, fuel, epoch timeout, etc.


using var module = Module.FromFile(engine, "javy-example.wasm");



// how do we do stdin/stdout/stderr? we can inherit them but then that seems unsafe, if we don't inherit can we use the file version safely?

linker.DefineWasi();
/*fn get_input_size() -> i32;
fn get_input(ptr: i32);
fn set_output(ptr: i32, size: i32);*/

Instance instance = null;


var responseExample = new Response() { ok = true, type = "LogResponse", payload = new LogResponse() { something = new() { "a", "b", "c" } } };

var responseAsString = System.Text.Json.JsonSerializer.Serialize(responseExample);




//Console.WriteLine("inputAsString: " + responseAsString);

/*
fn get_response_size() -> i32;// fn get_input_size() -> i32;
fn get_response(ptr: i32);// fn get_input(ptr: i32);
fn invoke(ptr: i32, size: i32, format: i32);// fn set_output(ptr: i32, size: i32);
*/

/*
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
        Console.WriteLine($"Called get_response with value: {ptr}");
        
        //caller!.GetMemory("memory")!.WriteByte(ptr, System.Text.Encoding.ASCII.GetBytes(inputAsString)[0]);
        caller!.GetMemory("memory")!.WriteString(ptr, responseAsString, System.Text.Encoding.UTF8);
        caller!.GetMemory("caller_response");
    })
);*/

linker.Define(
    "ubiquitous_functions",
    "invoke_json",
    Function.FromCallback(store, (Caller caller, int ptr, int size) =>
    {
        var memory = caller!.GetMemory("memory");
        
        // Optimizations: don't convert to UTF8 and iterate the string
        // Convert to utf8 but then write the string using the WriteString method
        var utf8Bytes = Encoding.UTF8.GetBytes(responseAsString);
        var input = caller!.GetMemory("memory")!.ReadString(ptr, size, Encoding.UTF8);
        Console.WriteLine($"Called invoke_json with value: {input}");
        Console.WriteLine($"Requesting WASM module to allocate {utf8Bytes.Length} bytes (+ 4 byte size header) in guest memory space for storing response...");
        var guest_malloc = caller.GetFunction("ubiquitous_functions_guest_malloc");
        int mem_loc = (int)guest_malloc.Invoke(utf8Bytes.Length + 4);
        // Call WriteByte with each piece of the int32
        caller!.GetMemory("memory")!.WriteByte(mem_loc, (byte)(utf8Bytes.Length >> 24));
        caller!.GetMemory("memory")!.WriteByte(mem_loc, (byte)(utf8Bytes.Length >> 16));
        caller!.GetMemory("memory")!.WriteByte(mem_loc, (byte)(utf8Bytes.Length >> 8));
        caller!.GetMemory("memory")!.WriteByte(mem_loc, (byte)(utf8Bytes.Length >> 0));
        caller!.GetMemory("memory")!.WriteString(mem_loc + 4, responseAsString, Encoding.UTF8);
        // https://stackoverflow.com/questions/39550856/what-is-the-right-way-to-allocate-data-to-pass-to-an-ffi-call might be relevant.
        //caller.ConsumeFuel(1000);

        //var add = caller.GetFunction("add");
        //var result = add.Invoke(1337, 42);
        //guest_malloc!.Invoke(responseAsString.Length); // TODO: ensure response is UTF8 encoded before measuring length. also add one for null terminator?
        //caller!.GetMemory("memory")!.WriteString(ptr, responseAsString, Encoding.UTF8);
        // TODO: all calls must require an ID so that we can properly trace the context? or do we track the ID of the instance automatically?
        return (Int32)mem_loc;
    })
);

var start = Stopwatch.StartNew();
var iterations = 1000;
for (int i = 0; i < iterations; i++)
{
    instance = linker.Instantiate(store, module);

    //Console.WriteLine("Exports:");
    //module.Exports.ToList().ForEach(e => Console.WriteLine(e.Name));
    var run = instance.GetAction("_start")!;
    
    run();

    // TODO: call a specific method for "_initialize"

    // TODO: call a specific method for "_

}

// write the elapsed time and the number of iterations
Console.WriteLine($"Elapsed: {start.ElapsedMilliseconds}ms");
Console.WriteLine($"Iterations: {iterations}");
Console.WriteLine("Done");
