using System;
using Importer.Utility;
using Microsoft.Toolkit.HighPerformance.Buffers;

namespace Importer.Converters
{
    public partial class XmlConverter
    {
        private string PatchXmlData(ReadOnlySpan<byte> content)
        {
            using MemoryOwner<byte> fixedXml = ApplyXmlFix(content);
            return StringUtility.GetXmlWithoutNamespacesFromBytes(fixedXml);
        }

        private MemoryOwner<byte> ApplyXmlFix(ReadOnlySpan<byte> content)
        {
            content = FindStartOfXml(content);
            MemoryOwner<byte> expanded = AddEndTag(content);
            return expanded;
        }

        private ReadOnlySpan<byte> FindStartOfXml(ReadOnlySpan<byte> content)
        {
            int index = content.IndexOf(Constants.StartTagBytes);
            return content.Slice(index);
        }

        private MemoryOwner<byte> AddEndTag(ReadOnlySpan<byte> content)
        {
            int finalLength = content.Length + Constants.EndingTagBytes.Length;
            MemoryOwner<byte> finalXmlContent = MemoryOwner<byte>.Allocate(finalLength);

            content.CopyTo(finalXmlContent.Span);
            Constants.EndingTagBytes.CopyTo(finalXmlContent.Span.Slice(content.Length));
            return finalXmlContent;
        }
    }
}