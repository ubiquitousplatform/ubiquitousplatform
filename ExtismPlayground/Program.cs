// See https://aka.ms/new-console-template for more information
using System;
using System.Text;
using ubiquitous.functions;

var source = await File.ReadAllBytesAsync("count_vowels.wasm");

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

FunctionPool pool = new FunctionPool();

for (int i = 0; i < 100; i++)
{
    Console.WriteLine($"FunctionPool iteration {i}");
    var output = await pool.ExecuteFunction("a", "b");
    Console.WriteLine(output);
}


var a = "hi";

