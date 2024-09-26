using System.Buffers;
using System.Runtime.CompilerServices;

using CommunityToolkit.HighPerformance.Buffers;

namespace Importer.Extensions;

internal static class SpanExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryOwner<TType> CopyToMemoryOwner<TType>(this ReadOnlySequence<TType> sequence)
    {
        MemoryOwner<TType> owner = MemoryOwner<TType>.Allocate((int)sequence.Length);
        sequence.CopyTo(owner.Span);
        return owner;
    }
}