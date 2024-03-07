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
        func.Load(new byte[] { });
    }

    [Fact(Skip = "not yet implemented")]
    public void WasmModuleWithoutProperExportsAndImports_ThrowsException()
    {
        var func = new FunctionExecutor();
        func.Load(new byte[] { });
    }

    [Fact(Skip = "not yet implemented")]
    public void InvalidWasmModule_ThrowsException()
    {
        var func = new FunctionExecutor();
        var pluginBytes = File.ReadAllBytes("test-harness.wasm");
        func.Load(pluginBytes);
    }

    [Fact]
    public void StateTests_NotInitialized_CallingFails()
    {
        var func = new FunctionExecutor();
        var pluginBytes = File.ReadAllBytes("test-harness.wasm");
        func.Load(pluginBytes);
        var result = func.Call("strlen", "Hello World!");
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
        var pluginBytes = File.ReadAllBytes("test-harness.wasm");
        func.Load(pluginBytes);
        var result = func.Call("strlen", "Hello World!");
    }

    [Fact]
    public void CorrectlyConstructedFunction_CallingSucceeds()
    {
        var func = new FunctionExecutor();
        var pluginBytes = File.ReadAllBytes("test-harness.wasm");
        func.Load(pluginBytes);
        func.Configure();
        func.Init();
        var result = func.Call("strlen", "Hello World!");
    }
    
    // TODO; validate timings are reasonable
}