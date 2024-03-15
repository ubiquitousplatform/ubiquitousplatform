// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;

Console.WriteLine("Hello, World!");

var summary = BenchmarkRunner.Run<BenchmarkWasmRuntimes.Extism>();
Console.WriteLine(summary);