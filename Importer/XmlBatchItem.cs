using System;
using System.Buffers;
using System.Collections.Generic;

using CommunityToolkit.HighPerformance.Buffers;

using Microsoft.Extensions.ObjectPool;

namespace Importer;

public sealed class XmlBatchItem : IResettable
{
    private readonly Queue<int> _xmlItemSizes = new();
    private readonly ArrayPoolBufferWriter<byte> _buffer = new();

    public IEnumerable<ReadOnlySequence<byte>> GetXmlItems()
    {
        ReadOnlyMemory<byte> written = _buffer.WrittenMemory;
        foreach (int xmlSize in _xmlItemSizes)
        {
            yield return new ReadOnlySequence<byte>(written[..xmlSize]);
            written = written[xmlSize..];
        }
    }

    public void Write(in ReadOnlySequence<byte> buffer)
    {
        _xmlItemSizes.Enqueue((int)buffer.Length);
        
        Span<byte> span = _buffer.GetSpan((int)buffer.Length);
        buffer.CopyTo(span);
        _buffer.Advance((int)buffer.Length);
    }
    
    /// <inheritdoc />
    public bool TryReset()
    {
        _buffer.Clear();
        _xmlItemSizes.Clear();
        return true;
    }
}