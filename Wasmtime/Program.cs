// See https://aka.ms/new-console-template for more information
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Wasmtime;
using WasmtimeExamples;

Console.WriteLine("Hello, World!");

Console.WriteLine("Calling wasi-data-sharing");

// Define wasmtime store and engine
// TODO: look into Epoch and timeouts
// TODO: measure fuel
// TODO: measure memory usage
System.Environment.SetEnvironmentVariable("WASMTIME_BACKTRACE_DETAILS", "1");

using var engine = new Engine();




using var linker = new Linker(engine);
// TODO: store can be initialized with data. how does this work? could we use this to our advantage for passing data to the wasm module?
using var store = new Store(engine);

// store.SetLimits
// would be cool to make WASMTIME_BACKTRACE_DETAILS = 1, RUST_BACKTRACE = full, and RUST_BACKTRACE = 1 as valid options in configuration
store.SetWasiConfiguration(new WasiConfiguration().WithEnvironmentVariables(new List<(string, string)> { ("UBIQUITOUS_RUNTIME_VERSION", "0.0.1"), ("WASMTIME_BACKTRACE_DETAILS", "1"), ("RUST_BACKTRACE", "full") }).WithInheritedStandardInput().WithInheritedStandardError().WithInheritedStandardOutput());

// Set memory / CPU limits, fuel, epoch timeout, etc.




using var module = Module.FromFile(engine, "wasi-data-sharing.wasm");

// how do we do stdin/stdout/stderr? we can inherit them but then that seems unsafe, if we don't inherit can we use the file version safely?

linker.DefineWasi();
/*fn get_input_size() -> i32;
fn get_input(ptr: i32);
fn set_output(ptr: i32, size: i32);*/

Instance instance = null;


var inputExample = new Input() { name = "hey", num = 5 };

var inputAsString = System.Text.Json.JsonSerializer.Serialize(inputExample);




Console.WriteLine("inputAsString: " + inputAsString);

/*
fn get_response_size() -> i32;// fn get_input_size() -> i32;
fn get_response(ptr: i32);// fn get_input(ptr: i32);
fn invoke(ptr: i32, size: i32, format: i32);// fn set_output(ptr: i32, size: i32);
*/

linker.Define(
    "ubiquitous_functions",
    "get_input_size",
    Function.FromCallback(store, () => Encoding.ASCII.GetByteCount(inputAsString))
);
linker.Define(
    "ubiquitous_functions",
    "get_input",
    // GetInput has a preallocated memory size based on calling get_input_size, and it expects us to write to it
    Function.FromCallback(store, (Caller caller, int ptr) =>
    {
        Console.WriteLine($"Called get_input with value: {ptr}");
        //caller!.GetMemory("memory")!.WriteByte(ptr, System.Text.Encoding.ASCII.GetBytes(inputAsString)[0]);
        caller!.GetMemory("memory")!.WriteString(ptr, inputAsString, System.Text.Encoding.ASCII);
    })
);

linker.Define(
    "ubiquitous_functions",
    "invoke_json",
    Function.FromCallback(store, (Caller caller, int ptr, int size) =>
    {
        Console.WriteLine("Called method:");
        Console.WriteLine(caller.GetMemory("memory")!.ReadString(ptr, size));
    })
);

instance = linker.Instantiate(store, module);
//Console.WriteLine("Exports:");
//module.Exports.ToList().ForEach(e => Console.WriteLine(e.Name));
var run = instance.GetAction("_start")!;
run();


