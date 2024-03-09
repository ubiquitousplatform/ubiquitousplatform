namespace ubiquitous.functions.tests.unit;

// This tests native functionality exposed by Extism.
// Examples:
//   * function I/O with plain strings and ints
//   * function I/O with JSON and MessagePack
//   * Builtin Extism methods
//      * Config Get
//      * Variable Get/Set
//   * WASI disabled/enabled
//      * Get WASI values
//   * Call simple Host Functions with various numbers of parameters
//   * Test memory limits and timeout limits are enforced by the Extism runtime
//   * ?? Measure Fuel
public class ExtismNativeTests
{
    private readonly FunctionExecutor _func;

    public ExtismNativeTests()
    {
        _func = new FunctionExecutor();
        var pluginBytes = File.ReadAllBytes("test-harness.wasm");
        _func.Load(pluginBytes);
        _func.Configure();
        _func.Init("test");
    }

    [Fact(Skip = "Not yet implemented")]
    public void VarsTest_VarsEnabled_CanSetVars()
    {
        //var result = _func.Call("doNothing", "");
        //Assert.Equal("", result);
    }

    [Fact(Skip = "Not yet implemented")]
    public void VarsTest_VarsDisabled_CannotSetVars()
    {
        //var result = _func.Call("doNothing", "");
        //Assert.Equal("", result);
    }

    [Fact(Skip = "Not yet implemented")]
    public void VarsTest_SetVarsLimit_CannotExceedLimit()
    {
        //var result = _func.Call("doNothing", "");
        //Assert.Equal("", result);
    }
}