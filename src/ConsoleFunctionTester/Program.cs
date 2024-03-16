// See https://aka.ms/new-console-template for more information

using System.Text;
using ubiquitous.functions.tests.unit;

Console.WriteLine("Hello, World!");


var _func = new FunctionExecutor();
var pluginBytes = File.ReadAllBytes("test-harness.wasm");

Console.WriteLine("registering host functions");
_func.RegisterHostFunction("debug", (plugin, memoryOffset) =>
{
    Console.WriteLine(plugin.ReadString(memoryOffset));
});

_func.RegisterHostFunction("ubiqDispatch",
    (plugin, memoryOffset) =>
    {
        return plugin.WriteBytes(Encoding.UTF8.GetBytes(""));
    });


Console.WriteLine("registered host functions. calling load");
_func.Load(pluginBytes);
_func.Configure();
Console.WriteLine("configured. calling init...");
_func.Init("test");


Console.WriteLine("initted, calling echo...");
// should call ubiqDispatch host function
_func.Call("ubiqEcho", "hihello");

Console.WriteLine("done!");