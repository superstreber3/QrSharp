using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

namespace QrSharpBenchmark;

public class FastConfig : ManualConfig
{
    public FastConfig()
    {
        AddJob(Job.ShortRun); // ShortRun is a predefined "fast" job in BenchmarkDotNet
        AddLogger(ConsoleLogger.Default); // Add default logger
        AddColumnProvider(DefaultColumnProviders.Instance); // Default columns in the result table
        AddExporter(DefaultExporters.Markdown);
    }
}

public class DefaultConfig : ManualConfig
{
    // Use BenchmarkDotNet's default settings. 
    // No need to add anything here.
}