using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;

namespace ubiquitous.functions
{

    public record FunctionDefinition
    {
        string? functionName;
        string? version;
        int minContexts = 0;
        int maxContexts = 10;
        int overprovisionTargetPercentage = 20;
    }

    public record FunctionBundle
    {
        public string? FunctionName;
        public string? Version; // TODO: ULID
        public byte[]? Code;
        public byte[]? Sha256;

    }
    public class FunctionPool
    {
        public async void AddFunctionDefinition()
        {

        }


        public async void StartAutoscaling()
        {

        }


        ConcurrentDictionary<string, FunctionBundle> _functionBundleCache = new ConcurrentDictionary<string, FunctionBundle>();
        // Store Function code in cache.  If it exists in cache, then just use it directly.
        public async Task<FunctionBundle> GetFunctionBundleAsync(string functionName, string version)
        {
            var cacheKey = $"{functionName}-{version}";
            // TODO: use ubiquitous.storage to load function.
            if (!_functionBundleCache.TryGetValue(cacheKey, out var bundle))
            {
                using (SHA256 mySHA256 = SHA256.Create())
                {

                    // This is kinda inefficient since it reads file twice but we're going to reimplement this on top of ubiquitous.storage anyway.
                    var functionSource = await File.ReadAllBytesAsync("count_vowels_js.wasm");
                    var fileStream = File.OpenRead("count_vowels_js.wasm");
                    FunctionBundle newBundle = new()
                    {
                        FunctionName = functionName,
                        Version = version,
                        Sha256 = await mySHA256.ComputeHashAsync(fileStream),
                        Code = functionSource
                    };
                    _functionBundleCache.TryAdd(cacheKey, newBundle);
                    return newBundle;
                }
            }
            return bundle;
            
        }

        public async Task<string> ExecuteFunction(string functionName, string version)
        { 
            var bundle = await GetFunctionBundleAsync(functionName, version);
            if (bundle == null) throw new Exception("Unable to retrieve function bundle code.");
            // TODO: use the SHA256 as a key into the contexts, so a change in the code results in a new pool item.
            // TODO: eventually the SHA256 of the code will just be 1 piece of the context caching - env vars will need to be used too, etc. account for this.
            // TODO: periodically expire old contexts, track how often they were invoked and scale them down if no longer in use.
            var context = await GetExecutionContextAsync(bundle.Code);
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

