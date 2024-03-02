using BenchmarkDotNet.Attributes;

namespace BenchmarkWasmRuntimes;

// https://www.codemag.com/Article/2209061/Benchmarking-.NET-6-Applications-Using-BenchmarkDotNet-A-Deep-Dive
[MemoryDiagnoser]
public class WasmtimeDotNet
{
    [GlobalSetup]
    public void GlobalSetup()
    {
        /*
        FunctionPool pool = new();*/

//await pool.ExecuteFunction("a", "b");
        var funcCode = File.ReadAllBytes("javy-example.wasm");

        //Write your initialization code here
    }

    [Benchmark]
    public void MyFirstBenchmarkMethod()
    {
        //Write your code here   
    }

    [Benchmark]
    public void MySecondBenchmarkMethod()
    {
        //Write your code here
    }
}