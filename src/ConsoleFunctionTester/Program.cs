// See https://aka.ms/new-console-template for more information

using System.Data;
using System.Text;
using ubiquitous.functions.tests.unit;

Console.WriteLine("Hello, World!");


var _func = new FunctionExecutor();
var pluginBytes = File.ReadAllBytes("test-harness.wasm");

Console.WriteLine("registering host functions");
_func.RegisterHostFunction("debug",
    (plugin, memoryOffset) => { Console.WriteLine($"WASMDBG: {plugin.ReadString(memoryOffset)}"); });

_func.RegisterHostFunction("ubiqDispatch",
    (plugin, memoryOffset) =>
    {
        var rawRequest = plugin.ReadBytes(memoryOffset);
        var interopVersion = rawRequest[0];
        if (interopVersion != 0)
        {
            throw new VersionNotFoundException(
                $"Ubiquitous Host Dispatch called with interop version {interopVersion}.  Only version 0 is currently supported.");
        }

        var encoding = (DispatchEncoding)rawRequest[1];
        if (encoding == DispatchEncoding.MessagePack)
        {
            throw new ArgumentOutOfRangeException("Ubiquitous Host Dispatch does not support MessagPack currently.");
        }

        // parse method definition
        // TODO: use enums for these
        var methodNamespace = rawRequest[2];
        var methodName = rawRequest[3];
        var methodVersion = rawRequest[4];
        var rawPayload = rawRequest.Slice(5);
        var payload = Encoding.UTF8.GetString(rawPayload);
        
        // TODO: write a real dispatch handler / dynamic plugin system here.
        
        Console.WriteLine(payload);

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