using System.Collections.Concurrent;
using ubiquitous.functions.ExecutionContext.RuntimeQueue;
using Wasmtime;

namespace ubiquitous.functions.ExecutionContext.FunctionPool
{
    public class FunctionPool : IFunctionPool
    {
        private Dictionary<string, CapacityConfig> _capacityConfig;
        private Engine _engine;

        public ConcurrentDictionary<string, ConcurrentDictionary<string, WasmRuntime>> AllocatedRuntimes { get; private set; } = new();
        public Dictionary<string, ConcurrentQueue<WasmRuntime>> AvailableRuntimes { get; private set; } = new();

        public FunctionPool()
        {
            this._capacityConfig = new Dictionary<string, CapacityConfig>();
            this._engine = new Engine();
            _capacityConfig.Add("ubiquitous_quickjs_v1", new CapacityConfig() { MaxCapacity = 1000, MinCapacity = 1, OverprovisionTargetPercentage = 10 });
        }
        public FunctionPool(Dictionary<string, CapacityConfig> initialRuntimeCapacity)
        {
            this._capacityConfig = initialRuntimeCapacity;
        }
        public void CheckinRuntime(WasmRuntime runtimeInstance)
        {
            //AvailableRuntimes[runtimeInstance.].Enqueue(new WasmRuntime(_engine, runtime));
        }

        public WasmRuntime? CheckoutRuntime(string runtime)
        {
            // TODO: Check if servicing this request will take us below our over-provisioned capacity. If so, scale up.
            // if (AllocatedRuntimes[runtime].Count == )

            // Update 
            if (!AvailableRuntimes.ContainsKey(runtime)) AvailableRuntimes[runtime] = new ConcurrentQueue<WasmRuntime>();

            AvailableRuntimes[runtime].Enqueue(new WasmRuntime(_engine, runtime));
            return AvailableRuntimes[runtime].TryDequeue(out WasmRuntime? runtimeInstance) ? runtimeInstance : null;
        }

        void IFunctionPool.RequestCapacityUpdate(string runtime, Dictionary<string, CapacityConfig> capacity)
        {
            _capacityConfig = capacity;
        }
    }

}
