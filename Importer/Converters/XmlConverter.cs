using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using FASTER.core;

using Importer.Utility;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Importer.Converters;

[SkipLocalsInit]
public sealed partial class XmlConverter
{
    public static ReadOnlySpan<byte> PatchXmlData(ReadOnlySpan<byte> content)
    {
        TagLiterals literals = new();
        int index = content.IndexOf("<Statistik>"u8);
        if (index == -1)
            index = 0;
            
        content = content.Slice(index);

        if (content.IndexOf(literals.EndingTagWithoutNameSpace) == -1)
        {
            Span<byte> fixedXml = GC.AllocateUninitializedArray<byte>(content.Length + literals.EndingTagWithoutNameSpace.Length);

            content.CopyTo(fixedXml);
            literals.EndingTagWithoutNameSpace.CopyTo(fixedXml[content.Length..]);

            return fixedXml;
        }

        return content;
    }
    
    public static ReadOnlySpan<byte> PatchXmlData(ReadOnlySequence<byte> content)
    {
        if (content.IsSingleSegment)
            return PatchXmlData(content.FirstSpan);
        
        //TODO: Avoid Mem Copy
        Span<byte> fixedBuffer = GC.AllocateUninitializedArray<byte>((int)content.Length);
        content.CopyTo(fixedBuffer);
        return PatchXmlData(fixedBuffer);
    }
    
    public static ValueTask<ReadOnlyMemory<byte>> PatchXmlDataAsync(ReadOnlyMemory<byte> content)
    {
        int index = content.Span.IndexOf("<Statistik>"u8);
        if (index == -1)
            index = 0;
            
        content = content[index..];

        return ValueTask.FromResult(content);
    }
}