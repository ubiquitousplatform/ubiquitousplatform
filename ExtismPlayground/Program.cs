// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using ubiquitous.functions.ExecutionContext.FunctionPool;

// var source = await File.ReadAllBytesAsync("count_vowels.wasm");

FunctionPool pool = new();

int iterations = 1000;

// Warm up function pool before measuring

//await pool.ExecuteFunction("a", "b");
//await pool.ExecuteFunction("a", "b");
//await pool.ExecuteFunction("a", "b");

// Test Serial Invocations
Stopwatch synchronousSw = Stopwatch.StartNew();
for (int i = 0; i < iterations; i++)
{
    Stopwatch sw = Stopwatch.StartNew();
    var output = await pool.ExecuteFunction("a", "b");
    if (output.Replace(" ", string.Empty) != "{\"count\":3}")
    {
        throw new ArgumentException($"unexpected output {output} on iteration {i}");
    }
    Console.WriteLine($"Synchronous FunctionPool iteration {i} completed in {sw.ElapsedMilliseconds} ms");

}
synchronousSw.Stop();

// Test Parallel Invocations
// Create a bogus array of x items to use for Parallel.ForEach.
int[] iterArray = new int[iterations];
for (int i = 0; i < iterations; i++)
{
    iterArray[i] = i;
}

await Task.Run(() => Parallel.ForEach(iterArray, (i) =>
        {
            //var sw = Stopwatch.StartNew();
            var output = pool.ExecuteFunction("a", "b").Result;
            if (output != "{\"count\": 3}")
            {
                throw new ArgumentException($"unexpected output {output} on iteration {i}");
            }
            //Console.WriteLine($"Async FunctionPool iteration {i} completed in {sw.ElapsedMilliseconds} ms");
        }));

Stopwatch asyncSw = Stopwatch.StartNew();
await Task.Run(() => Parallel.ForEach(iterArray, (i) =>
{
    // var sw = Stopwatch.StartNew();
    var output = pool.ExecuteFunction("a", "b").Result;
    if (output != "{\"count\": 3}")
    {
        throw new ArgumentException($"unexpected output {output} on iteration {i}");
    }
    //Console.WriteLine($"Async FunctionPool iteration {i} completed in {sw.ElapsedMilliseconds} ms");
}));
Console.WriteLine($"Synchronous FunctionPool testing completed in {synchronousSw.ElapsedMilliseconds} ms");
Console.WriteLine($"Parallel FunctionPool testing completed in {asyncSw.ElapsedMilliseconds} ms");
// var a = "hi";

