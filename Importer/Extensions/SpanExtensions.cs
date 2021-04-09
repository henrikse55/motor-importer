using System.Buffers;
using Microsoft.Toolkit.HighPerformance.Buffers;

namespace Importer.Extensions
{
    public static class SpanExtensions
    {
        public static MemoryOwner<TType> CopyToMemoryOwner<TType>(this ReadOnlySequence<TType> sequence)
        {
            MemoryOwner<TType> owner = MemoryOwner<TType>.Allocate((int)sequence.Length);
            sequence.CopyTo(owner.Span);
            return owner;
        }
    }
}