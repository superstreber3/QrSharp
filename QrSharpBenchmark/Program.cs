using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace QrSharpBenchmark;

internal static class Program
{
    private static IConfig? _chosenConfig;

    private static void Main()
    {
        Console.WriteLine("Choose a profile:");
        Console.WriteLine("1. Fast");
        Console.WriteLine("2. Default");


        switch (Console.ReadLine())
        {
            case "1":
                _chosenConfig = new FastConfig();
                break;
            case "2":
                _chosenConfig = new DefaultConfig();
                break;
            default:
                Console.WriteLine("Invalid profile choice.");
                return;
        }

        var benchmarkTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.GetMethods().Any(m => m.GetCustomAttribute<BenchmarkAttribute>() is not null))
            .ToArray();

        Console.WriteLine("Select a benchmark class:");
        for (var i = 0; i < benchmarkTypes.Length; i++)
        {
            Console.WriteLine($"{i + 1}. {benchmarkTypes[i].Name}");
        }

        Console.WriteLine($"{benchmarkTypes.Length + 1}. All");

        if (int.TryParse(Console.ReadLine(), out var classChoice) && classChoice <= benchmarkTypes.Length + 1 &&
            classChoice > 0)
        {
            if (classChoice == benchmarkTypes.Length + 1)
            {
                BenchmarkRunner.Run(benchmarkTypes, _chosenConfig);
                return;
            }

            var selectedType = benchmarkTypes[classChoice - 1];
            SelectAndRunBenchmarksForType(selectedType);
        }
        else
        {
            Console.WriteLine("Invalid selection.");
        }
    }

    private static void SelectAndRunBenchmarksForType(Type benchmarkType)
    {
        var benchmarkMethods = benchmarkType.GetMethods()
            .Where(m => m.GetCustomAttribute<BenchmarkAttribute>() is not null)
            .ToArray();

        Console.WriteLine($"Select a method from {benchmarkType.Name}:");
        for (var i = 0; i < benchmarkMethods.Length; i++)
        {
            Console.WriteLine($"{i + 1}. {benchmarkMethods[i].Name}");
        }

        Console.WriteLine($"{benchmarkMethods.Length + 1}. All");

        if (int.TryParse(Console.ReadLine(), out var methodChoice) && methodChoice <= benchmarkMethods.Length + 1 &&
            methodChoice > 0)
        {
            if (methodChoice == benchmarkMethods.Length + 1)
            {
                BenchmarkRunner.Run(benchmarkType, _chosenConfig);
                return;
            }

            var selectedMethod = benchmarkMethods[methodChoice - 1];
            BenchmarkRunner.Run(benchmarkType, new[] { selectedMethod }, _chosenConfig);
        }
        else
        {
            Console.WriteLine("Invalid selection.");
        }
    }
}