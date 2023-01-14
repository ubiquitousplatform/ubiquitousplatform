using System;
using System.Collections.Concurrent;

namespace ubiquitous.functions
{

    public class FunctionPool
    {
        public async void AddFunctionDefinition(string functionName, string version)
        {

        }


        public async void StartAutoscaling()
        {

        }

        public async Task<string> ExecuteFunction(string functionName, string version)
        {
            // TODO: lookup function name/version in cache and load function source from storage
            var functionSource = await File.ReadAllBytesAsync("count_vowels.wasm");
            var context = await GetExecutionContextAsync(functionSource);
            return await context.HandleEventAsync(new ExecutionEvent());
        }


        private ConcurrentBag<IExecutionContext> _executionContexts = new();

        private async Task<IExecutionContext> GetExecutionContextAsync(Byte[] source)
        {

            // Find a process in the pool that's not used.  If there are none not-used, then
            // create a new process.

            // TODO: automatically scale up / down execution contexts (sliding window)
            // TODO: support multiple functions by hashing the source as a lookup key.
            // TODO: when scaling to 0, clear function code / version out of memory.

            // Loop over all execution contexts in current cache.
            foreach (var context in _executionContexts)
            {
                //var context = entry as TypescriptProcessExecutionContext;
                // If any of them are not currently executing, try and get an exclusive handle to them.
                if (context.ReserveContext())
                {
                    return context;
                }
            }

            // If we still haven't found a context, scale out a new one.
            var newContext = new WasmExecutionContext(source);
            await newContext.StartupAsync();
            // TODO: add metrics to track the total size and the new context.
            _executionContexts.Add(newContext);
            return newContext;

        }
    }


    /*


    Features:
     - Create an instance of a runner
     - Runner should have a list of all functions and their versions
     - Each function+version should have a config (just like Lambda)
     - Store functions using ubiquitous.storage
     - Runtime for now will just be `deno run xxx`
     - Support input/output (from either Event system or from Background Runner)
      - Take inspiration from Lambda event format

    Later
     - Enable default concurrency setting


    */
}

