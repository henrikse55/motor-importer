using System;
using Importer.Utility;
using Microsoft.Toolkit.HighPerformance;
using Microsoft.Toolkit.HighPerformance.Buffers;

namespace Importer.Converters
{
    public partial class XmlConverter
    {
        public string PatchXmlData(ReadOnlySpan<byte> content)
        {
            int index = content.IndexOf(Constants.StartTagBytes);
            content = content.Slice(index);

            using ArrayPoolBufferWriter<byte> fixedXml =
                new(content.Length + Constants.EndingTagBytes.Length);

            fixedXml.Write(content);
            fixedXml.Write((ReadOnlySpan<byte>) Constants.EndingTagBytes);

            return StringUtility.GetXmlWithoutNamespacesFromBytes(fixedXml.WrittenSpan);
        }
    }
}