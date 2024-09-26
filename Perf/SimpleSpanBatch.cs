using System;
using System.Buffers;
using System.Collections.Generic;
using FASTER.core;

namespace Perf;

public class SimpleSpanBatch : IReadOnlySpanBatch
{
    private readonly List<Memory<byte>> _memories = new();
    public int TotalEntries() => _memories.Count;

    public ReadOnlySpan<byte> Get(int index) => _memories[index].Span;

    public void Add(ReadOnlySequence<byte> sequence)
    {
        Memory<byte> buffer = GC.AllocateUninitializedArray<byte>((int)sequence.Length);
        sequence.CopyTo(buffer.Span);
        _memories.Add(buffer);
    }
}