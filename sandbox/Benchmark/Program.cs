using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System.Reflection;

BenchmarkSwitcher.FromAssembly(Assembly.GetEntryAssembly()!).Run(args);

class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddDiagnoser(MemoryDiagnoser.Default);
        AddJob(Job.ShortRun
            .WithStrategy(RunStrategy.ColdStart)
            .WithWarmupCount(0)
            .WithIterationCount(1)
            .WithInvocationCount(1));
    }
}