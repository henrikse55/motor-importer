using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Importer;

public readonly ref struct TagLiterals
{
    public readonly ReadOnlySpan<byte> EndingTag = "</ns:Statistik>"u8;
    public readonly ReadOnlySpan<byte> StartTag = "<Statistik>"u8;
    public readonly ReadOnlySpan<byte> EndingTagWithoutNameSpace = "</Statistik>"u8;
    public readonly ReadOnlySpan<byte> NameSpaceDelimiter = "ns:"u8;

    public TagLiterals()
    {
    }
}