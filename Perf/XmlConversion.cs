using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using CommunityToolkit.HighPerformance.Buffers;

namespace Perf;

[SimpleJob]
[MemoryDiagnoser]
public class XmlConversion
{
    private readonly byte[] _contentBytes = Encoding.UTF8.GetBytes(LargeContent.LargeXmlEntry);
    private MemoryOwner<byte> _processItem;

    [GlobalSetup]
    public void StartUp()
    {
        _processItem = MemoryOwner<byte>.Allocate(_contentBytes.Length);
        ((Span<byte>) _contentBytes).CopyTo(_processItem.Span);
    }
}