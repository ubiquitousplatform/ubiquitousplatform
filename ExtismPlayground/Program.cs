// See https://aka.ms/new-console-template for more information
using System;
using System.Reflection.Metadata;
using System.Text;
using ubiquitous.functions;

var source = await File.ReadAllBytesAsync("count_vowels.wasm");

/*
async Task<string> callFunctionAsync()
{
    var localSource = await File.ReadAllBytesAsync("count_vowels.wasm");
    using var context = new Extism.Sdk.Native.Context();
    using var plugin = context.CreatePlugin(localSource, withWasi: false);
    // TODO: call HTTP endpoint to test out http.  also test out websockets, etc.

    var output = Encoding.UTF8.GetString(
        plugin.CallFunction("count_vowels", Encoding.UTF8.GetBytes("Hello World!"))
    );
    return output;
}*/

FunctionPool pool = new FunctionPool();

/*
for (int i = 0; i < 100; i++)
{
    Console.WriteLine($"Auto-dispose iteration {i}");
    using Extism.Sdk.Native.Context context = new Extism.Sdk.Native.Context();
    using var plugin = context.CreatePlugin(source, withWasi: false);
    // TODO: call HTTP endpoint to test out http.  also test out websockets, etc.

    var output = Encoding.UTF8.GetString(
        plugin.CallFunction("count_vowels", Encoding.UTF8.GetBytes("Hello World!"))
    );
    Console.WriteLine(output);
}

for (int i = 0; i < 100; i++)
{
    Console.WriteLine($"No using iteration {i}");
    Extism.Sdk.Native.Context context = new Extism.Sdk.Native.Context();
    var plugin = context.CreatePlugin(source, withWasi: false);
    // TODO: call HTTP endpoint to test out http.  also test out websockets, etc.

    var output = Encoding.UTF8.GetString(
        plugin.CallFunction("count_vowels", Encoding.UTF8.GetBytes("Hello World!"))
    );

    Console.WriteLine(output);
}


*/

// Test async function invocation
/*
for (int i = 0; i < 100; i++) {
    Console.WriteLine($"async function iteration {i}");
    var result = await callFunctionAsync();
    // await Task.Delay(500);
}

for (int i = 0; i < 100; i++)
{
    Console.WriteLine($"Context iteration {i}");
    // TODO: lookup function name/version in cache and load function source from storage
    var newContext = new WasmExecutionContext(source);
    await newContext.StartupAsync();
    await newContext.HandleEventAsync(new ExecutionEvent());
}
*/
// This Crashes
for (int i = 0; i < 10000; i++)
{
    Console.WriteLine($"FunctionPool iteration {i}");
    var output = await pool.ExecuteFunction("a", "b");
    Console.WriteLine(output);
}


// var a = "hi";

