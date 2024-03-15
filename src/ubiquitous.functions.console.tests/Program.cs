// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using ubiquitous.functions.tests.unit;

Console.WriteLine("Hello, World!");

if (Stopwatch.IsHighResolution)
    Console.WriteLine("Operations timed using the system's high-resolution performance counter.");
else
    Console.WriteLine("Operations timed using the DateTime class.");

var frequency = Stopwatch.Frequency;
Console.WriteLine("  Timer frequency in ticks per second = {0}",
    frequency);
var nanosecPerTick = 1000L * 1000L * 1000L / frequency;
Console.WriteLine("  Timer is accurate within {0} nanoseconds",
    nanosecPerTick);

var _func = new FunctionExecutor();
var input = "HelLo";
var pluginBytes = File.ReadAllBytes("test-harness.wasm");
//_func.Load(pluginBytes);
//_func.Configure();
//_func.Init();
//Assert.Equal(expectedLength, length);


//_func.RegisterHostFunction("ubiqDispatch",
//(plugin, memoryOffset) => { return plugin.WriteBytes(Encoding.UTF8.GetBytes("")); });


_func.Load(pluginBytes);
_func.Configure();
_func.Init("test");


Console.WriteLine("hello");
// should call ubiqDispatch host function
var result = _func.Call("strlen", input);
var length = int.Parse(result);

_func.Call("ubiqEcho", "hihello");