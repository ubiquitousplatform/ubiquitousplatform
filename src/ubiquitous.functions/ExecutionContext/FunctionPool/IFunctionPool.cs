using ubiquitous.functions.ExecutionContext.RuntimeQueue;

namespace ubiquitous.functions.ExecutionContext.FunctionPool
{

    public interface IFunctionPool
    {
        public WasmRuntime? CheckoutRuntime(string runtime);
        public void CheckinRuntime(WasmRuntime runtimeInstance);
        internal void RequestCapacityUpdate(string runtime, Dictionary<string, CapacityConfig> capacity);
    }
}