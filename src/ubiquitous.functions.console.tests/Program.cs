// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using ubiquitous.functions.tests.unit;

Console.WriteLine("Hello, World!");

if (Stopwatch.IsHighResolution)
{
    Console.WriteLine("Operations timed using the system's high-resolution performance counter.");
}
else
{
    Console.WriteLine("Operations timed using the DateTime class.");
}

long frequency = Stopwatch.Frequency;
Console.WriteLine("  Timer frequency in ticks per second = {0}",
    frequency);
long nanosecPerTick = (1000L*1000L*1000L) / frequency;
Console.WriteLine("  Timer is accurate within {0} nanoseconds",
    nanosecPerTick);

var _func = new FunctionExecutor();
var input = "HelLo";
var pluginBytes = File.ReadAllBytes("test-harness.wasm");
_func.Load(pluginBytes);
//_func.Configure();
//_func.Init();
var result = _func.Call("strlen", input);
var length = Int32.Parse(result);
//Assert.Equal(expectedLength, length);
