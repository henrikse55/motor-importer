using System;
using System.Buffers;
using System.Text;
using BenchmarkDotNet.Attributes;
using CommunityToolkit.HighPerformance.Buffers;
using Importer.Utility;

namespace Perf;

[SimpleJob]
[MemoryDiagnoser]
public class StringUtilsBench
{
    public static readonly byte[] ContentBytes = Encoding.UTF8.GetBytes(LargeContent.LargeXmlEntry);
    private ReadOnlySequence<byte> sequence = new ReadOnlySequence<byte>(ContentBytes);

    private MemoryOwner<byte> _memory;

    [GlobalSetup]
    public void StartUp()
    {
        MemoryOwner<byte> owner = MemoryOwner<byte>.Allocate(ContentBytes.Length);
        ((Span<byte>) ContentBytes).CopyTo(owner.Span);
        _memory = owner;
    }

    [Benchmark(Baseline = true)]
    public string RemoveNameSpaceWithoutIndex()
    {
        return StringUtility.GetXmlWithoutNamespacesFromBytes(_memory.Span);
    }

    [Benchmark]
    public ReadOnlySpan<byte> RemoveNameSpacesFromSequence()
    {
        return StringUtility.GetXmlWithoutNamespaces(sequence);
    }
}