using System;
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
                runtimeCache[runtime] = Module.FromFile(engine, "runtimes/ubiquitous_quickjs_v1.wasm");
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
            store.SetWasiConfiguration(new WasiConfiguration().WithInheritedStandardInput().WithInheritedStandardOutput().WithInheritedStandardError().WithEnvironmentVariables(new List<(string, string)> { ("UBIQUITOUS_RUNTIME_VERSION", "1.0.0"), ("WASMTIME_BACKTRACE_DETAILS", "1"), ("RUST_BACKTRACE", "full") }));
            //store.SetWasiConfiguration(new WasiConfiguration().WithEnvironmentVariables(new List<(string, string)> { ("UBIQUITOUS_RUNTIME_VERSION", "1.0.0"), ("WASMTIME_BACKTRACE_DETAILS", "1"), ("RUST_BACKTRACE", "full") }));
            linker.DefineWasi();

            // Add custom invoke_json hook that allows IPC from the guest to the host.
            linker.Define(
                "ubiquitous_functions",
                "invoke_json",
                Function.FromCallback(store, InvokeJson())
            );
            // TODO: change this to 'runtime' and fix the registration of the runtime name to be 'ubiquitous_quickjs_v1' instead of 'javy_quickjs_provider_v1'
            InstantiateModule("ubiquitous_quickjs_v1", runtimeCache[Runtime]);
            _functionLoaded = false;
        }


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

        private object _eventInput; // store the input to the event
        private bool _eventInputRetrieved; // has the input been retrieved yet?
        private bool _eventOutputSet; // has the output been set yet?
        private object _eventOutput; // store the output from the event


        public void StartupRuntime()
        {
            InvokeMethod("_start", false);
        }

        public void InvokeMethod(string methodName, bool expectIO, object eventInput = null)
        {
            if (!_functionLoaded)
            {
                throw new InvalidOperationException("Runtime is empty and has no user code. Call LoadFunctionCode before attempting to invoke.");
            }
            _eventInput = eventInput;
            // Configure store
            // TODO: set epochs so functions can't run forever, and add an epoch incrementer.
            //store.SetEpochDeadline((ulong)maxExecutionMs * 1000); +

            Function? functionReference = linker.GetFunction(store, "js_user_code_instance", methodName);
            if (functionReference == null)
            {
                throw new Exception($"Function {methodName} not found!");
            }
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
            if (expectIO)
            {
                // Function should have a return value at this point.  If it doesn't, then it timed out or it didn't properly follow the
                // calling conventions.
                if (!_eventInputRetrieved)
                {
                    Console.WriteLine("WARNING: function never retrieved input parameters. Possible implementation issue in plugin module.");
                }
                if (!_eventOutputSet)
                {
                    throw new Exception($"HOST: Invoked function {methodName} failed to return value.  Must call set_event_result from guest.  Possible implementation issue in plugin module.");
                }
            }

        }
        // TODO: get memory usage...

        //public FunctionConfig FunctionConfig { get; }

        private CallerFunc<int, int, int> InvokeJson()
        {
            return (Caller caller, int ptr, int size) =>
            {
                // TODO: switch this to a task and run it async so serialize/deserialize and handlers can all be async.


                Memory memory = caller!.GetMemory("memory");
                Function guest_malloc = caller.GetFunction("ubiquitous_functions_guest_malloc");
                if (guest_malloc == null)
                {
                    Console.WriteLine("HOST: Failed to find guest_malloc function");
                    return -1;
                }

                // TODO: check pointer and size to ensure that they are within reasonable limits (can't send us more than 256kb in a given request or something like that).
                Console.WriteLine("HOST: Called invokeJson with pointer: " + ptr + " and size: " + size);
                string bytes = Encoding.UTF8.GetString(memory.GetSpan<byte>(ptr, size));
                string input = memory.ReadString(ptr, size, Encoding.UTF8);
                //Console.WriteLine($"HOST: WASM module called invoke_json with value: {input}");

                // Dispatch to handler system and get back response...
                var inputJson = System.Text.Json.JsonSerializer.Deserialize<InvokeJsonPayload>(input);
                // TODO: write handler logic here.
                // We should just broadcast to all handlers in order, and the first one to "claim" an action gets it,
                // so we can externalize and IoC the handler mappings to a DI container based on configuration instead of it being hardcoded.
                switch (inputJson.action)
                {
                    case "log":
                        {
                            break;
                        }
                    case "set_event_result":
                        {
                            // This is an internal handler that the runtime will ultimately need to register, which
                            // stores the return value of the invocation.
                            // TODO: do some meaningful parsing / validation of this once we need it for something.
                            _eventOutput = inputJson.payload;
                            _eventOutputSet = true;
                            return 0; // void return
                        }
                    case "get_event_input":
                        {
                            // This is an internal handler that the runtime will ultimately need to register, which
                            // stores the input information for the invocation in the guest linear memory and returns a reference
                            // to that.
                            var inputResponse = AllocateGuest(memory, guest_malloc, this._eventInput);
                            _eventInputRetrieved = true;
                            return inputResponse;
                        }

                    default:
                        {
                            break;
                        }
                }
                HandlerResult response = Logger.Log(input);
                return AllocateGuest(memory, guest_malloc, response);

            };
        }

        private int AllocateGuest(Memory memory, Function allocator, object objectToSerialize)
        {
            //var responseExample = new Response() { ok = true, type = "LogResponse", payload = new LogResponse() { something = new() { "a", "b", "c" } } };
            string responseAsString = System.Text.Json.JsonSerializer.Serialize(objectToSerialize);
            int utf8ByteLength = Encoding.UTF8.GetByteCount(responseAsString);


            //Console.WriteLine($"HOST: Requesting WASM module to allocate {utf8ByteLength} bytes (+ 4 byte size header) in guest memory space for storing response...");

            //Console.WriteLine("HOST: Invoking guest_malloc...");
            int mem_loc = (int)(allocator.Invoke(utf8ByteLength + 4) ?? -1);
            //Console.WriteLine($"HOST: Guest malloc returned {mem_loc}");
            // Call WriteByte with each piece of the int32
            memory.WriteInt32(mem_loc, utf8ByteLength);
            memory.WriteString(mem_loc + 4, responseAsString, Encoding.UTF8);
            // https://stackoverflow.com/questions/39550856/what-is-the-right-way-to-allocate-data-to-pass-to-an-ffi-call might be relevant.
            //TODO: measure and consume fuel.  caller.ConsumeFuel(1000);
            return mem_loc;
        }

        public void Dispose()
        {
            store?.Dispose();
            linker?.Dispose();
        }
    }
}
