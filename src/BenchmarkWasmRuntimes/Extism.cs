using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Extism.Sdk;

namespace BenchmarkWasmRuntimes;

// https://www.codemag.com/Article/2209061/Benchmarking-.NET-6-Applications-Using-BenchmarkDotNet-A-Deep-Dive
[MemoryDiagnoser]
public class Extism
{
    private Plugin _extismVarsCountVowels;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var manifest =
            new Manifest(
                new UrlWasmSource("https://github.com/extism/plugins/releases/latest/download/count_vowels.wasm"));
        
        _extismVarsCountVowels = new Plugin(manifest, new HostFunction[] { }, true);
        
        /*
        FunctionPool pool = new();*/

//await pool.ExecuteFunction("a", "b");
        // var funcCode = File.ReadAllBytes("javy-example.wasm");

        //Write your initialization code here
    }

    [Benchmark]
    public void ExtismVarsCountVowelsPlugin()
    {
        var output = _extismVarsCountVowels.Call("count_vowels", "Hello, World!");
        // Console.WriteLine(output);
        //Write your code here   
    }
    
    [Benchmark]
    public void ExtismVarsCountVowelsPluginWithMemoryReset()
    {
        _extismVarsCountVowels.
        var output = _extismVarsCountVowels.Call("count_vowels", "Hello, World!");
        // Console.WriteLine(output);
        //Write your code here   
    }

    [Benchmark]
    public void HostFunctionKVCountVowelsPlugin()
    {
        //Write your code here
        
    }
}