using System.Collections.Concurrent;
using ubiquitous.functions.ExecutionContext.RuntimeQueue;
using Wasmtime;

namespace ubiquitous.functions.ExecutionContext.FunctionPool
{
    public class FunctionPool : IFunctionPool
    {
        private Dictionary<string, CapacityConfig> _capacityConfig;
        private Engine _engine;

        public ConcurrentDictionary<string, ConcurrentDictionary<Guid, WasmRuntime>> AllocatedRuntimes { get; private set; } = new();
        public Dictionary<string, ConcurrentQueue<WasmRuntime>> AvailableRuntimes { get; private set; } = new();

        public FunctionPool()
        {
            this._capacityConfig = new Dictionary<string, CapacityConfig>();
            this._engine = new Engine();
            _capacityConfig.Add("ubiquitous_quickjs_v1", new CapacityConfig() { MaxCapacity = 1000, MinCapacity = 10, OverprovisionTargetPercentage = 10 });

            ScaleToTargetCapacity(runtime: "ubiquitous_quickjs_v1");
        }
        public FunctionPool(Dictionary<string, CapacityConfig> initialRuntimeCapacity)
        {
            this._capacityConfig = initialRuntimeCapacity;
            ScaleToTargetCapacity(runtime: "ubiquitous_quickjs_v1");

        }
        public void CheckinRuntime(WasmRuntime runtimeInstance)
        {
            // TODO: make this async
            // Remove the runtime from the allocated runtimes.
            if (!AllocatedRuntimes[runtimeInstance.Runtime].TryRemove(runtimeInstance.Id, out _))
            {
                Console.WriteLine("Failed to remove runtime from allocated runtimes.");
            }
            runtimeInstance.ResetRuntime();
            AvailableRuntimes[runtimeInstance.Runtime].Enqueue(runtimeInstance);
        }

        public WasmRuntime? CheckoutRuntime(string runtime)
        {
            // TODO: Check if servicing this request will take us below our over-provisioned capacity. If so, scale up.
            // if (AllocatedRuntimes[runtime].Count == )

            // Update 

            //AvailableRuntimes[runtime].Enqueue(new WasmRuntime(_engine, runtime));
            ScaleToTargetCapacity(runtime);
            WasmRuntime? allocatedRuntime = AvailableRuntimes[runtime].TryDequeue(out WasmRuntime? runtimeInstance) ? runtimeInstance : null;
            if (allocatedRuntime == null)
            {
                Console.WriteLine("No available runtimes after scaling.");
                return null;
            }
            AllocatedRuntimes[runtime].TryAdd(allocatedRuntime.Id, allocatedRuntime);
            return allocatedRuntime;
        }
        private void ScaleToTargetCapacity(string runtime)
        {
            // TODO: make this a background task
            if (!AvailableRuntimes.ContainsKey(runtime)) AvailableRuntimes[runtime] = new();

            if (!AllocatedRuntimes.ContainsKey(runtime)) AllocatedRuntimes[runtime] = new();

            // We want to make sure that our allocated runtimes are at least the minimum capacity, but
            // we also want to make sure that we have enough available runtimes to service requests by overprovisioning
            // up to a certain percentage.
            int targetCapacity = AllocatedRuntimes[runtime].Count * _capacityConfig[runtime].OverprovisionTargetPercentage / 100;
            if (targetCapacity < _capacityConfig[runtime].MinCapacity)
            {
                targetCapacity = _capacityConfig[runtime].MinCapacity;
            }

            else if (targetCapacity > _capacityConfig[runtime].MaxCapacity)
            {
                targetCapacity = _capacityConfig[runtime].MaxCapacity;
            }

            Console.WriteLine($"Currently used: {AllocatedRuntimes[runtime].Count}.  Unused: {AvailableRuntimes[runtime].Count}. Scaling {runtime} to {targetCapacity} runtimes.");

            // First check if we are underprovisioned below minimum capacity limits. If so, scale up to min capacity.
            while (AllocatedRuntimes[runtime].Count + AvailableRuntimes[runtime].Count < targetCapacity)
            {
                AvailableRuntimes[runtime].Enqueue(new WasmRuntime(_engine, runtime));
                Console.WriteLine("Scaling...");
            }
        }

        void IFunctionPool.RequestCapacityUpdate(string runtime, Dictionary<string, CapacityConfig> capacity)
        {
            _capacityConfig = capacity;
        }
    }

}
