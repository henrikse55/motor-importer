using System;
using System.Text;
using Microsoft.Toolkit.HighPerformance.Buffers;

namespace Importer.Utility
{
    public static class StringUtility
    {
        /// <see cref="GetXmlWithoutNamespacesFromBytes" />
        [Obsolete("Use new implementation")]
        public static string RemoveNamespaceFromByteString(MemoryOwner<byte> content)
        {
            return RemoveSubstringFromByteString(content, Constants.NameSpaceDelimiterBytes);
        }

        public static string GetXmlWithoutNamespacesFromBytes(ReadOnlySpan<byte> content)
        {
            return RemoveNameSpaceWithoutIndexing(content, Constants.NameSpaceDelimiterBytes);
        }

        private static string RemoveNameSpaceWithoutIndexing(ReadOnlySpan<byte> content, ReadOnlySpan<byte> delimiter)
        {
            using ArrayPoolBufferWriter<char> buffer = new(content.Length);

            while (true)
            {
                int length = content.IndexOf(delimiter);

                if (length == -1)
                    break;

                ReadOnlySpan<byte> rawSlice = content.Slice(0, length);

                Span<char> bufferSlice = buffer.GetSpan(rawSlice.Length);
                int count = Encoding.Default.GetChars(rawSlice, bufferSlice);
                buffer.Advance(count);

                content = content.Slice(length + delimiter.Length);
            }

            int result = Encoding.UTF8.GetChars(content, buffer.GetSpan(content.Length));
            buffer.Advance(result);

            return new string(buffer.WrittenSpan);
        }

        private static string RemoveSubstringFromByteString(MemoryOwner<byte> content, ReadOnlySpan<byte> delimiter)
        {
            int[] indexes = SpanUtility.FindAllDelimiterIndexes(content.Span, delimiter);

            int finalLength = content.Length - indexes.Length * delimiter.Length;

            ByteStringState stringState = new(content, indexes, delimiter.Length);

            string result = CreateString(finalLength, stringState);

            return result;
        }

        private static string CreateString(int finalLength, ByteStringState stringState)
        {
            return string.Create(finalLength, stringState, (state, contentState) =>
            {
                ReadOnlySpan<byte> raw = contentState.Content.Span;
                foreach (int index in contentState.Indexes)
                {
                    int length = index - 1;
                    length = length <= 0 ? 1 : length;

                    Span<char> stateSlice = state.Slice(0, length);
                    ReadOnlySpan<byte> rawSlice = raw.Slice(0, length);
                    StringPool.Shared.GetOrAdd(rawSlice, Encoding.ASCII).AsSpan().CopyTo(stateSlice);

                    state = state.Slice(length);
                    raw = raw.Slice(length + contentState.DelimiterLength);
                }

                StringPool.Shared.GetOrAdd(raw, Encoding.ASCII).AsSpan().CopyTo(state);
            });
        }

        private readonly struct ByteStringState
        {
            public readonly MemoryOwner<byte> Content;
            public readonly int[] Indexes;
            public readonly int DelimiterLength;

            public ByteStringState(MemoryOwner<byte> content, int[] indexes, int delimiterLength)
            {
                Content = content;
                Indexes = indexes;
                DelimiterLength = delimiterLength;
            }
        }
    }
}