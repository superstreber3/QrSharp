using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace QrSharpBenchmark;

internal static class Program
{
    private static void Main()
    {
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
                BenchmarkRunner.Run(benchmarkTypes);
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
                BenchmarkRunner.Run(benchmarkType);
                return;
            }

            var selectedMethod = benchmarkMethods[methodChoice - 1];
            BenchmarkRunner.Run(benchmarkType, new[] { selectedMethod });
        }
        else
        {
            Console.WriteLine("Invalid selection.");
        }
    }
}