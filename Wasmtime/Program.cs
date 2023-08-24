using System.Diagnostics;
using System.Text;
using ubiquitous.functions.ExecutionContext.RuntimeQueue;
using Wasmtime;
using WasmtimeExamples;

Console.WriteLine("HOST: Hello, World!");

Console.WriteLine("HOST: Calling javy-example");

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




/*
var js_runtime = "uqibuitous_js_v1";
var functionPool = new FunctionPool();
functionPool.AddRuntime(js_runtime, (Engine engine) =>
{
    // Factory for creating a new instance of the runtime (new up wasm module, register function handlers, etc.)
    // Must return a dictionary with the elapsed ms of the construction and the name of the instance

    // Runtime init code goes here
});

functionPool.SetRuntimeConcurrency(js_runtime, new ConcurrencySettings() {
Minimum: 10, Maximum: 1000, OverprovisioningPercentage: 10});


*/



// store.AddFuel(1000000000);

//
//store.SetLimits

// store.SetLimits
// would be cool to make WASMTIME_BACKTRACE_DETAILS = 1, RUST_BACKTRACE = full, and RUST_BACKTRACE = 1 as valid options in configuration
// store.SetWasiConfiguration(new WasiConfiguration().WithEnvironmentVariables(new List<(string, string)> { ("UBIQUITOUS_RUNTIME_VERSION", "0.0.1"), ("WASMTIME_BACKTRACE_DETAILS", "1"), ("RUST_BACKTRACE", "full") }).WithInheritedStandardInput().WithInheritedStandardError().WithInheritedStandardOutput());

// Set memory / CPU limits, fuel, epoch timeout, etc.

// All this stuff can be done once at startup and then reused for every invocation

using var engine = new Engine(new Config().WithEpochInterruption(true)); // Can this engine be reused?

// TODO: use DI for this.
// runtime.
using var module = Module.FromFile(engine, "javy-example.wasm");



// how do we do stdin/stdout/stderr? we can inherit them but then that seems unsafe, if we don't inherit can we use the file version safely?




var start = Stopwatch.StartNew();

var iterations = 100000;

for (int i = 0; i < iterations; i++)
{

    var runtime = new WasmRuntime(engine, "ubiquitous_quickjs_v1");
    runtime.InvokeMethod("_start");
    // var run = instance.GetAction("_start")!;
    var runSw = Stopwatch.StartNew();
    //module.Exports.ToList().ForEach(e => Console.WriteLine(e.Name));
    //engine.IncrementEpoch();
    
    // Lifecycle should be: register module (and check constraints during registration such as only 1 memory export total), store memory exports for each module, as well as function exports
    // then instantiate the instances, start a stopwatch, opt function into Epoch tracking, run function, then at the end calculate memory usage and epoch usage.
    Console.WriteLine("HOST: runsw: " + runSw.Elapsed);
    Console.WriteLine("Imports for js_user_code_instance:");
    module.Imports.ToList().ForEach(e => Console.WriteLine($"{e.ModuleName}::{e.Name} ({e.GetType()})"));
    Console.WriteLine("Exports for js_user_code_instance:");
    module.Exports.ToList().ForEach(e => Console.WriteLine($"{e.Name} ({e.GetType()})"));
    Console.WriteLine("Imports for javy_quickjs_provider_v1:");
    quickjs_provider.Imports.ToList().ForEach(e => Console.WriteLine($"{e.ModuleName}::{e.Name} ({e.GetType()})"));
    Console.WriteLine("Exports for javy_quickjs_provider_v1:");
    quickjs_provider.Exports.ToList().ForEach(e => Console.WriteLine($"{e.Name} ({e.GetType()})"));
    var javyMemoryUsageInBytes = linker.GetMemory(store, "javy_quickjs_provider_v1", "memory")!.GetSize() * 64 * 1024;
    //var codeMemoryUsageInBytes = linker.GetMemory(store, "js_user_code_instance", "memory")!.GetSize() * 64 * 1024;
    Console.WriteLine("HOST: Memory consumption:" + javyMemoryUsageInBytes);

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
Console.WriteLine("HOST: Done");
