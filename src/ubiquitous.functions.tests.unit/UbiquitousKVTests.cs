using System.Text;

namespace ubiquitous.functions.tests.unit;

public class UbiquitousKVTests
{
    private readonly FunctionExecutor _func;

    public UbiquitousKVTests()
    {
        _func = new FunctionExecutor();
        var pluginBytes = File.ReadAllBytes("test-harness.wasm");

        _func.RegisterHostFunction("ubiqDispatch",
            (plugin, memoryOffset) =>
            {

                return plugin.WriteBytes(Encoding.UTF8.GetBytes(""));
            });

        /*_func.RegisterHostFunction("ubiqDispatch", (plugin, memoryOffset) =>
        {
            //var key = plugin.ReadString(keyOffset);
            var bytes = plugin.ReadBytes(memoryOffset);

            // TODO: parse host header
            // TODO: dispatch this call to our own internal host processing pipeline
            // TODO: how do we restrict access to specific calls?

            /if (!kvStore.TryGetValue(key, out var value))
            {
                value = new byte[] { 0, 0, 0, 0 };
            }


            //Console.WriteLine($"Read {BitConverter.ToUInt32(value)} from key={key}");
            return plugin.WriteBytes(Encoding.UTF8.GetBytes(""));
        });*/
        _func.Load(pluginBytes);
        _func.Configure();
        _func.Init("test");
    }

    [Fact]
    public void FakeTest()
    {
        Console.WriteLine("hello");
        // should call ubiqDispatch host function
        _func.Call("ubiqEcho", "hihello");
    }
}