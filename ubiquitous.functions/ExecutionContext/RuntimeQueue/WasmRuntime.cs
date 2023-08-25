using System.Text;
using ubiquitous.functions.Handlers;
using Wasmtime;
using Module = Wasmtime.Module;

namespace ubiquitous.functions.ExecutionContext.RuntimeQueue
{

    internal class InstantiatedModule
    {
        public string ModuleName { get; set; }
        public IReadOnlyList<Import> Imports { get; set; }
        public IReadOnlyList<Export> Exports { get; set; }
    }
    public class WasmRuntime : IDisposable
    {
        // This could be refactored to have a better runtime cache in the future but this should suffice since we only really have
        // one possible runtime at the moment.
        private static Dictionary<string, Module> runtimeCache = new();

        private readonly Engine engine;
        public string Runtime { get; private set; }
        private Store store;
        private Linker linker;
        private Dictionary<string, InstantiatedModule> instantiatedModules = new();
        public Guid Id { get; } = Guid.NewGuid();
        // TODO: get Engine from DI.
        public WasmRuntime(Engine engine, string runtime)
        {
            if (runtime == null)
            {
                throw new ArgumentNullException(nameof(runtime));
            }
            if (runtime != "ubiquitous_quickjs_v1")
            {
                throw new ArgumentException("Invalid runtime", nameof(runtime));
            }

            this.engine = engine;
            this.Runtime = runtime;

            // Initialize our module in the cache if it's not already there.
            if (!runtimeCache.ContainsKey(runtime))
            {
                runtimeCache[runtime] = Module.FromFile(engine, "../ubiquitous.functions/runtimes/ubiquitous_quickjs_v1.wasm");
            }
            ResetRuntime();
        }

        public void ResetRuntime()
        {
            instantiatedModules.Clear();
            store?.Dispose();
            linker?.Dispose();
            // TODO: store can be initialized with data. how does this work? could we use this to our advantage for passing data to the wasm module?
            store = new Store(engine);

            linker = new Linker(engine);


            // Define Wasi
            //store.SetWasiConfiguration(new WasiConfiguration().WithInheritedStandardInput().WithInheritedStandardOutput().WithInheritedStandardError().WithEnvironmentVariables(new List<(string, string)> { ("UBIQUITOUS_RUNTIME_VERSION", "1.0.0"), ("WASMTIME_BACKTRACE_DETAILS", "1"), ("RUST_BACKTRACE", "full") }));
            store.SetWasiConfiguration(new WasiConfiguration().WithEnvironmentVariables(new List<(string, string)> { ("UBIQUITOUS_RUNTIME_VERSION", "1.0.0"), ("WASMTIME_BACKTRACE_DETAILS", "1"), ("RUST_BACKTRACE", "full") }));
            linker.DefineWasi();

            // Add custom invoke_json hook that allows IPC from the guest to the host.
            linker.Define(
                "ubiquitous_functions",
                "invoke_json",
                Function.FromCallback(store, _invokeJsonCallback)
            );
            // TODO: change this to 'runtime' and fix the registration of the runtime name to be 'ubiquitous_quickjs_v1' instead of 'javy_quickjs_provider_v1'
            InstantiateModule("javy_quickjs_provider_v1", runtimeCache[Runtime]);
            _functionLoaded = false;
        }

        private static CallerFunc<int, int, int> _invokeJsonCallback = InvokeJson();

        private void InstantiateModule(string wasmNamespace, Module module)
        {
            Instance wasm_module_instance = linker.Instantiate(store, module);
            instantiatedModules.Add(wasmNamespace, new InstantiatedModule { ModuleName = wasmNamespace, Imports = module.Imports, Exports = module.Exports });
            linker.DefineInstance(store, wasmNamespace, wasm_module_instance);
        }

        private bool _functionLoaded = false;
        public void LoadFunctionCode(string moduleName, byte[] functionCode)
        {
            if (functionCode == null)
            {
                throw new ArgumentNullException(nameof(functionCode));
            }
            if (moduleName == null)
            {
                throw new ArgumentNullException(nameof(moduleName));
            }

            if (moduleName != "js_user_code_instance")
            {
                throw new ArgumentException("Invalid module name, only js_user_code_instance is allowed.", nameof(moduleName));
            }

            Module module = Module.FromBytes(engine, moduleName, functionCode);
            InstantiateModule(moduleName, module);
            module.Dispose();

            _functionLoaded = true;
        }

        public void InvokeMethod(string methodName)
        {
            if (!_functionLoaded)
            {
                throw new InvalidOperationException("Runtime is empty and has no user code. Call LoadFunctionCode before attempting to invoke.");
            }

            // Configure store
            // TODO: set epochs so functions can't run forever, and add an epoch incrementer.
            //store.SetEpochDeadline((ulong)maxExecutionMs * 1000); +

            Function functionReference = linker.GetFunction(store, "js_user_code_instance", methodName)!;
            try
            {
                functionReference.Invoke();
            }
            catch (TrapException e)
            {
                //TODO: handle exceptions better.
                Console.WriteLine("HOST: TrapException: " + e);

            }
            catch (Exception e)
            {
                Console.WriteLine("HOST: Exception: " + e);
            }

        }
        // TODO: get memory usage...

        //public FunctionConfig FunctionConfig { get; }

        private static CallerFunc<int, int, int> InvokeJson()
        {
            return (Caller caller, int ptr, int size) =>
            {
                Memory memory = caller!.GetMemory("memory");

                Console.WriteLine("HOST: Called invokeJson with pointer: " + ptr + " and size: " + size);
                string bytes = Encoding.UTF8.GetString(memory.GetSpan<byte>(ptr, size));
                string input = memory.ReadString(ptr, size, Encoding.UTF8);
                //Console.WriteLine($"HOST: WASM module called invoke_json with value: {input}");

                // Dispatch to handler system and get back response...

                // TODO: write handler logic here.
                HandlerResult response = Logger.Log(input);
                //var responseExample = new Response() { ok = true, type = "LogResponse", payload = new LogResponse() { something = new() { "a", "b", "c" } } };
                string responseAsString = System.Text.Json.JsonSerializer.Serialize(response);
                int utf8ByteLength = Encoding.UTF8.GetByteCount(responseAsString);


                //Console.WriteLine($"HOST: Requesting WASM module to allocate {utf8ByteLength} bytes (+ 4 byte size header) in guest memory space for storing response...");
                Function guest_malloc = caller.GetFunction("ubiquitous_functions_guest_malloc");
                if (guest_malloc == null)
                {
                    Console.WriteLine("HOST: Failed to find guest_malloc function");
                    return -1;
                }
                //Console.WriteLine("HOST: Invoking guest_malloc...");
                int mem_loc = (int)(guest_malloc.Invoke(utf8ByteLength + 4) ?? -1);
                //Console.WriteLine($"HOST: Guest malloc returned {mem_loc}");
                // Call WriteByte with each piece of the int32
                memory.WriteInt32(mem_loc, utf8ByteLength);
                memory.WriteString(mem_loc + 4, responseAsString, Encoding.UTF8);
                // https://stackoverflow.com/questions/39550856/what-is-the-right-way-to-allocate-data-to-pass-to-an-ffi-call might be relevant.
                //TODO: measure and consume fuel.  caller.ConsumeFuel(1000);
                return mem_loc;
            };
        }

        public void Dispose()
        {
            store?.Dispose();
            linker?.Dispose();
        }
    }
}
