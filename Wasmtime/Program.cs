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
// From: https://github.com/bytecodealliance/wasmtime/issues/4109 fuel measurement is expensive. Epochs are more like how people expect stuff to work anyway?



var maxExecutionMs = 2500;

// store.AddFuel(1000000000);

//
//store.SetLimits

// store.SetLimits
// would be cool to make WASMTIME_BACKTRACE_DETAILS = 1, RUST_BACKTRACE = full, and RUST_BACKTRACE = 1 as valid options in configuration
// store.SetWasiConfiguration(new WasiConfiguration().WithEnvironmentVariables(new List<(string, string)> { ("UBIQUITOUS_RUNTIME_VERSION", "0.0.1"), ("WASMTIME_BACKTRACE_DETAILS", "1"), ("RUST_BACKTRACE", "full") }).WithInheritedStandardInput().WithInheritedStandardError().WithInheritedStandardOutput());

// Set memory / CPU limits, fuel, epoch timeout, etc.

// All this stuff can be done once at startup and then reused for every invocation
using var engine = new Engine(new Config().WithEpochInterruption(true)); // Can this engine be reused?


using var module = Module.FromFile(engine, "javy-example.wasm");


// how do we do stdin/stdout/stderr? we can inherit them but then that seems unsafe, if we don't inherit can we use the file version safely?







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



var iterations = 1000;
for (int i = 0; i < iterations; i++)
{

    using var store = new Store(engine);// TODO: store can be initialized with data. how does this work? could we use this to our advantage for passing data to the wasm module?
    store.SetWasiConfiguration(new WasiConfiguration().WithEnvironmentVariables(new List<(string, string)> { ("UBIQUITOUS_RUNTIME_VERSION", "0.0.1"), ("WASMTIME_BACKTRACE_DETAILS", "1"), ("RUST_BACKTRACE", "full") }));

    store.SetEpochDeadline((ulong)maxExecutionMs * 1000);

    using var linker = new Linker(engine); // Can this linker be reused?
    linker.DefineWasi();
    linker.Define(
        "ubiquitous_functions",
        "invoke_json",
        Function.FromCallback(store, InvokeJson())
    );

    Instance instance = null;

    instance = linker.Instantiate(store, module);

    //Console.WriteLine("Exports:");
    //module.Exports.ToList().ForEach(e => Console.WriteLine(e.Name));
    var run = instance.GetAction("_start")!;
    engine.IncrementEpoch();

    run();

    // TODO: call a specific method for "_initialize"

    // TODO: call a specific method for "_

}

var start = Stopwatch.StartNew();



for (int i = 0; i < iterations; i++)
{
    var storeSw = Stopwatch.StartNew();
    using var store = new Store(engine);// TODO: store can be initialized with data. how does this work? could we use this to our advantage for passing data to the wasm module?
    store.SetWasiConfiguration(new WasiConfiguration().WithEnvironmentVariables(new List<(string, string)> { ("UBIQUITOUS_RUNTIME_VERSION", "0.0.1"), ("WASMTIME_BACKTRACE_DETAILS", "1"), ("RUST_BACKTRACE", "full") }));

    store.SetEpochDeadline((ulong)maxExecutionMs * 1000);

    Console.WriteLine("storeSw: " + storeSw.Elapsed);
    var linkerSw = Stopwatch.StartNew();
    using var linker = new Linker(engine); // Can this linker be reused?
    Console.WriteLine("linkerSw: " + linkerSw.Elapsed);
    linker.DefineWasi();
    Console.WriteLine("linkerSw: " + linkerSw.Elapsed);
    linker.Define(
        "ubiquitous_functions",
        "invoke_json",
        Function.FromCallback(store, InvokeJson)
    );
    Console.WriteLine("linkerSw: " + linkerSw.Elapsed);
    
    var instanceSw = Stopwatch.StartNew();
    Instance instance = null;

    instance = linker.Instantiate(store, module);

    //Console.WriteLine("Exports:");
    Console.WriteLine("instanceSw: " + instanceSw.Elapsed);
    var runSw = Stopwatch.StartNew();
    var run = instance.GetAction("_start")!;
    //module.Exports.ToList().ForEach(e => Console.WriteLine(e.Name));
    //engine.IncrementEpoch();
    run();
    Console.WriteLine("runsw: " + runSw.Elapsed);
    // TODO: call a specific method for "_initialize"

    // TODO: call a specific method for "_

}


/* 

Multiple Stores
The Linker type is designed to be compatible, in some scenarios, with instantiation in multiple Stores. Specifically host-defined functions created in Linker with Linker::func_new, Linker::func_wrap, and their async versions are compatible to instantiate into any Store. This enables programs which want to instantiate lots of modules to create one Linker value at program start up and use that continuously for each Store created over the lifetime of the program.

Note that once Store-owned items, such as Global, are defined witin a Linker then it is no longer compatible with any Store. At that point only the Store that owns the Global can be used to instantiate modules.

Multiple Engines
The Linker type is not compatible with usage between multiple Engine values. An Engine is provided when a Linker is created and only stores and items which originate from that Engine can be used with this Linker. If more than one Engine is used with a Linker then that may cause a panic at runtime, similar to how if a Func is used with the wrong Store that can also panic at runtime.
 from: https://docs.wasmtime.dev/api/wasmtime/struct.Linker.html#:~:text=A%20Linker%20is%20a%20way,used%20to%20instantiate%20a%20Module%20.
*/

// write the elapsed time and the number of iterations
Console.WriteLine($"Elapsed: {start.ElapsedMilliseconds}ms");
Console.WriteLine($"Iterations: {iterations}");
Console.WriteLine("Done");

static CallerFunc<int, int, int> InvokeJson()
{
    return (Caller caller, int ptr, int size) =>
    {
        var memory = caller!.GetMemory("memory");

        var responseExample = new Response() { ok = true, type = "LogResponse", payload = new LogResponse() { something = new() { "a", "b", "c" } } };
        var responseAsString = System.Text.Json.JsonSerializer.Serialize(responseExample);
        var utf8ByteLength = Encoding.UTF8.GetByteCount(responseAsString);
        var input = memory.ReadString(ptr, size, Encoding.UTF8);
        //Console.WriteLine($"Called invoke_json with value: {input}");
        //Console.WriteLine($"Requesting WASM module to allocate {utf8ByteLength} bytes (+ 4 byte size header) in guest memory space for storing response...");
        var guest_malloc = caller.GetFunction("ubiquitous_functions_guest_malloc");
        if (guest_malloc == null)
        {
            Console.WriteLine("Failed to find guest_malloc function");
            return -1;
        }
        //Console.WriteLine("Invoking guest_malloc...");
        int mem_loc = (int)(guest_malloc.Invoke(utf8ByteLength + 4) ?? -1);
        //Console.WriteLine($"Guest malloc returned {mem_loc}");
        // Call WriteByte with each piece of the int32
        memory.WriteInt32(mem_loc, utf8ByteLength);
        memory.WriteString(mem_loc + 4, responseAsString, Encoding.UTF8);
        // https://stackoverflow.com/questions/39550856/what-is-the-right-way-to-allocate-data-to-pass-to-an-ffi-call might be relevant.
        //TODO: measure and consume fuel.  caller.ConsumeFuel(1000);
        return mem_loc;
    };
}