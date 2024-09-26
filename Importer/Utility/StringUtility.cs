using System;
using System.Buffers;
using System.Text;
using CommunityToolkit.HighPerformance.Buffers;

namespace Importer.Utility;

public static partial class StringUtility
{
    public static string GetXmlWithoutNamespacesFromBytes(ReadOnlySpan<byte> content)
    {
        return RemoveNameSpaceWithoutIndexing(content, "ns:"u8);
    }

    private static string RemoveNameSpaceWithoutIndexing(ReadOnlySpan<byte> content, in ReadOnlySpan<byte> delimiter)
    {
        using ArrayPoolBufferWriter<char> buffer = new(content.Length);

        while (true)
        {
            int length = content.IndexOf(delimiter);

            if (length == -1)
                break;

            ReadOnlySpan<byte> rawSlice = content.Slice(0, length);

            Span<char> bufferSlice = buffer.GetSpan(rawSlice.Length);
            int count = Encoding.UTF8.GetChars(rawSlice, bufferSlice);
            buffer.Advance(count);

            content = content.Slice(length + delimiter.Length);
        }

        int result = Encoding.UTF8.GetChars(content, buffer.GetSpan(content.Length));
        buffer.Advance(result);

        return new string(buffer.WrittenSpan);
    }
}