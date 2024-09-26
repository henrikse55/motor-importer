using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Perf.Utils;

namespace Perf;

[SimpleJob]
[MemoryDiagnoser]
public class LogWrite
{
    [Params(3_000, 30_000, 300_000)]
    public int ReadUntil { get; set; }
    
    [Benchmark(Baseline = true)]
    public async Task ReadWithoutBatching()
    {
        await new ReaderTester(ReadUntil).Run();
    }
    
    [Benchmark]
    public async Task ReadWithSimpleBatching()
    {
        await new ReaderTester(ReadUntil).RunSimpleBatching();
    }
}