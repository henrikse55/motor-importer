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
}