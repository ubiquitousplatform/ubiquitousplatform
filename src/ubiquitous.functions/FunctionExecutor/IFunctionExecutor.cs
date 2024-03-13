using Extism.Sdk;

namespace ubiquitous.functions.tests.unit;

/// <summary>
///     This is a thin wrapper around the underlying execution engine that abstracts out the steps of the lifecycle of a
///     piece of code.
///     Typically you need to load the code into memory, configure it in some way, then run lifecycle methods
///     (one-time initialization, one or more executions, and one-time teardown).
///     Host functions that need to be patched into the execution context are "plugged in" via a registration call to
///     RegisterHostFunction.
/// </summary>
public interface IFunctionExecutor
{
    public void Load(byte[] PluginBytes);

    public void Configure();
    public string Init(string input);
    public string Call(string method, string input);

    public void RegisterHostFunction(string name, Func<CurrentPlugin, long, long> callback);
}