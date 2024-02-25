// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using BenchmarkWasmRuntimes;

Console.WriteLine("Hello, World!");
 
var summary = BenchmarkRunner.Run<WasmtimeDotNet>();