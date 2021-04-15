using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Importer.Utility;
using Microsoft.Toolkit.HighPerformance.Buffers;

namespace Perf
{
    [SimpleJob]
    [MemoryDiagnoser]
    [EventPipeProfiler(EventPipeProfile.GcVerbose)]
    public class StringUtils
    {
        public readonly byte[] ContentBytes = Encoding.UTF8.GetBytes(LargeContent.LargeXmlEntry);

        private MemoryOwner<byte> _memory;

        [GlobalSetup]
        public void StartUp()
        {
            MemoryOwner<byte> owner = MemoryOwner<byte>.Allocate(ContentBytes.Length);
            ((Span<byte>) ContentBytes).CopyTo(owner.Span);
            _memory = owner;
        }

        [Benchmark(Baseline = true)]
        public string RemoveNameSpaceToString()
        {
            return StringUtility.RemoveNamespaceFromByteString(_memory);
        }

        [Benchmark]
        public string RemoveNameSpaceWithoutIndex()
        {
            return StringUtility.GetXmlWithoutNamespacesFromBytes(_memory.Span);
        }
    }
}