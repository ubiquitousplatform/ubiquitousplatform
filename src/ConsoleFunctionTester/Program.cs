// See https://aka.ms/new-console-template for more information

using System.Text;
using System.Text.Json;
using ubiquitous.functions.tests.unit;

Console.WriteLine("Hello, World!");


var _func = new FunctionExecutor();
var pluginBytes = File.ReadAllBytes("test-harness.wasm");

Console.WriteLine("registering host functions");
_func.RegisterHostFunction("debug", (plugin, memoryOffset) =>
{
    Console.WriteLine("WASM STDOUT:" + plugin.ReadString(memoryOffset));
});


_func.RegisterHostFunction("ubiqDispatch",
    (plugin, memoryOffset) =>
    {

        // TODO: implement lookup table for function names
        var bytes = plugin.ReadBytes(memoryOffset);
        // TODO: can we map this to a record type or something efficiently
        var interopVersion = bytes[0];
        var encoding = bytes[1];
        var functionNamespace = bytes[2];
        var functionMethod = bytes[3];
        var functionVersion = bytes[4];
        // TODO: make an enum for the interop versions
        switch (interopVersion)
        {
            case 0:
                // TODO: we need to define the json schema for this based on the function namespace, method, and version.

                var stdoutWriteInput = JsonSerializer.Deserialize<StdoutWriteInput>(Encoding.UTF8.GetString(bytes[5..]));
                Console.WriteLine("STDOUT: " + stdoutWriteInput.Message);
                break;
            default:
                throw new Exception("Unknown interop version");
                break;
        }
        return plugin.WriteBytes(Encoding.UTF8.GetBytes("hello from ubiqDispatch"));
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