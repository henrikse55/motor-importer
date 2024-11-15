using System;
using System.Buffers;
using System.Collections.Generic;

using CommunityToolkit.HighPerformance.Buffers;

using Microsoft.Extensions.ObjectPool;

namespace Importer;

public sealed class XmlBatchItem : IResettable
{
    private readonly ArrayPoolBufferWriter<int> _xmlItemSizes = new();
    private readonly ArrayPoolBufferWriter<byte> _buffer = new();

    public IEnumerable<ReadOnlySequence<byte>> GetXmlItems()
    {
        ReadOnlyMemory<byte> written = _buffer.WrittenMemory;
        ReadOnlyMemory<int> sizes = _xmlItemSizes.WrittenMemory;
        for (int index = 0; index < sizes.Length; index++)
        {
            int xmlSize = sizes.Span[index];
            yield return new ReadOnlySequence<byte>(written[..xmlSize]);
            written = written[xmlSize..];
        }
    }

    public void Write(in ReadOnlySequence<byte> buffer)
    {
        int bufferLength = (int)buffer.Length;
        Span<int> item = _xmlItemSizes.GetSpan(1);
        item[0] = bufferLength;
        _xmlItemSizes.Advance(1);
        
        Span<byte> span = _buffer.GetSpan(bufferLength);
        buffer.CopyTo(span);
        _buffer.Advance(bufferLength);
    }
    
    /// <inheritdoc />
    public bool TryReset()
    {
        _buffer.Clear();
        _xmlItemSizes.Clear();
        return true;
    }
}