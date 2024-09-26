using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;

using CommunityToolkit.HighPerformance;

using Importer.Converters;

using Microsoft.IO;

namespace Importer.Utility;

public static partial class StringUtility
{
    
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> PatchXmlData(in ReadOnlySpan<byte> content)
    {
        int index = content.IndexOf("<Statistik>"u8);
        return index == -1 ? content : content[index..];
    }
    
    
    public static ReadOnlySpan<byte> GetXmlWithoutNamespaces(in ReadOnlySequence<byte> content)
    {
        ArrayBufferWriter<byte> buffer = new((int)content.Length);

        SequenceReader<byte> reader = new(content);
        while (!reader.End)
        {
            if (reader.TryReadTo(out ReadOnlySequence<byte> result, "ns:"u8))
            {
                int length = (int)result.Length;

                Span<byte> writeableSpan = buffer.GetSpan(length);
                result.CopyTo(writeableSpan);
                buffer.Advance(length);
            }
            else
            {
                int readerRemaining = (int)reader.Remaining;
                
                Span<byte> writeableSpan = buffer.GetSpan(readerRemaining);
                reader.UnreadSequence.CopyTo(writeableSpan);
                
                buffer.Advance(readerRemaining);
                reader.Advance(readerRemaining);
            }
        }

        
        ReadOnlySpan<byte> endingTag = "</Statistik>"u8;
        int endingTagLength = endingTag.Length;
        
        Span<byte> ending = buffer.GetSpan(endingTagLength);
        endingTag.CopyTo(ending);
        buffer.Advance(endingTagLength);
        
        return buffer.WrittenSpan;
    }
    
    public static MemoryStream GetXmlWithoutNamespacesStream(in ReadOnlySequence<byte> content, in RecyclableMemoryStreamManager manager)
    {
        var stream = manager.GetStream();
        
        SequenceReader<byte> reader = new(content);
        while (!reader.End)
        {
            if (reader.TryReadTo(out ReadOnlySequence<byte> result, "ns:"u8))
            {
                foreach (ReadOnlyMemory<byte> memory in result)
                {
                    stream.Write(memory.Span);
                }

            }
            else
            {
                ReadOnlySpan<byte> readerUnreadSpan = reader.UnreadSpan;
                stream.Write(readerUnreadSpan);
                reader.Advance(readerUnreadSpan.Length);
            }
        }
        
        stream.Write("</Statistik>"u8);

        ReadOnlySpan<byte> buffer = stream.GetBuffer().AsSpan();
        stream.Position = buffer.IndexOf("<Statistik>"u8);
        
        return stream;
    }
    
    public static ReadOnlyMemory<byte> GetXmlWithoutNamespacesToMemory(in ReadOnlySequence<byte> content)
    {
        ArrayBufferWriter<byte> buffer = new((int)content.Length);

        SequenceReader<byte> reader = new(content);
        while (!reader.End)
        {
            if (reader.TryReadTo(out ReadOnlySequence<byte> result, "ns:"u8))
            {
                int length = (int)result.Length;

                Span<byte> writeableSpan = buffer.GetSpan(length);
                result.CopyTo(writeableSpan);
                buffer.Advance(length);
            }
            else
            {
                int readerRemaining = (int)reader.Remaining;
                
                Span<byte> writeableSpan = buffer.GetSpan(readerRemaining);
                reader.UnreadSequence.CopyTo(writeableSpan);
                
                buffer.Advance(readerRemaining);
                reader.Advance(readerRemaining);
            }
        }

        
        ReadOnlySpan<byte> endingTag = "</Statistik>"u8;
        int endingTagLength = endingTag.Length;
        
        Span<byte> ending = buffer.GetSpan(endingTagLength);
        endingTag.CopyTo(ending);
        buffer.Advance(endingTagLength);
        
        return buffer.WrittenMemory;
    }
}