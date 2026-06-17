// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Running;
using ProductService.Benchmarks;

Console.WriteLine("Hello, World!");

    
BenchmarkRunner.Run<MappingBenchmark>();
    