using Extism.Sdk;
using ubiquitous.functions.FunctionExecutor;

namespace ubiquitous.functions.tests.unit;

public class FunctionExecutorTests
{
    [Fact]
    public void RealWasmModule_LoadsSuccessfully()
    {
        var func = new FunctionExecutor();
        var pluginBytes = File.ReadAllBytes("test-harness.wasm");
        func.Load(pluginBytes);
    }

    [Fact]
    public void EmptyByteArray_ThrowsException()
    {
        var func = new FunctionExecutor();
        Assert.Throws<ExtismException>(() => func.Load(new byte[] { }));
    }

    [Fact(Skip = "not yet implemented")]
    public void WasmModuleWithoutProperExportsAndImports_ThrowsException()
    {
        var func = new FunctionExecutor();
        func.Load(new byte[] { });
    }

    [Fact]
    public void InvalidWasmModule_ThrowsException()
    {
        var func = new FunctionExecutor();
        // Get random bytes out of the middle of the wasm file, which "probably" isn't an invalid wasm file
        var pluginBytes = File.ReadAllBytes("test-harness.wasm").Skip(53821).Take(10204).ToArray();
        Assert.Throws<ExtismException>(() => func.Load(pluginBytes));
    }

    [Fact]
    public void StateTests_NotInitialized_CallingFails()
    {
        var func = new FunctionExecutor();
        var pluginBytes = File.ReadAllBytes("test-harness.wasm");
        func.Load(pluginBytes);
        Assert.Throws<PluginStateIncorrectException>(() =>
        {
            var result = func.Call("strlen", "Hello World!");
        });
    }

    [Fact]
    public void StateTests_NotLoaded_CallingFails()
    {
        var func = new FunctionExecutor();
        Assert.Throws<PluginStateIncorrectException>(() =>
        {
            var result = func.Call("strlen", "Hello World!");
        });
    }

    [Fact]
    public void StateTests_NotLoaded_InitializingFails()
    {
        var func = new FunctionExecutor();

        Assert.Throws<PluginStateIncorrectException>(() =>
        {
            var result = func.Init("Hello World!");
        });
    }

    [Fact]
    public void CorrectlyConstructedFunction_CallingSucceeds()
    {
        var func = new FunctionExecutor();
        var pluginBytes = File.ReadAllBytes("test-harness.wasm");
        func.Load(pluginBytes);
        func.Configure();
        func.Init("test");
        Assert.Equal("12", func.Call("strlen", "Hello World!"));
    }

    // TODO; validate timings are reasonable

    // TODO: write tests that validate _init won't work if it's not defined (isn't this the same as checking exports?)
}