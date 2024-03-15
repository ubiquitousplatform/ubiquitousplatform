// See https://aka.ms/new-console-template for more information

using System.Text;
using ubiquitous.functions.tests.unit;

Console.WriteLine("Hello, World!");


var _func = new FunctionExecutor();
var pluginBytes = File.ReadAllBytes("test-harness.wasm");

Console.WriteLine("hello");
_func.RegisterHostFunction("ubiqDispatch",
    (plugin, memoryOffset) => { return plugin.WriteBytes(Encoding.UTF8.GetBytes("")); });


Console.WriteLine("hello");
_func.Load(pluginBytes);
_func.Configure();
Console.WriteLine("hello");
_func.Init("test");


Console.WriteLine("hello");
// should call ubiqDispatch host function
_func.Call("ubiqEcho", "hihello");

Console.WriteLine("FIN!");