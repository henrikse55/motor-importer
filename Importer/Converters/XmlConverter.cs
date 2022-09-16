using System;
using System.IO;
using Importer.Utility;

namespace Importer.Converters
{
    public partial class XmlConverter
    {
        public static ReadOnlySpan<byte> PatchXmlData(ReadOnlySpan<byte> content)
        {
            int index = content.IndexOf(Constants.StartTagBytes);
            if (index == -1)
                index = 0;
            
            content = content.Slice(index);

            Span<byte> fixedXml = GC.AllocateUninitializedArray<byte>(content.Length + Constants.EndingTagBytes.Length);

            content.CopyTo(fixedXml);
            Constants.EndingTagBytes.CopyTo(fixedXml[content.Length..]);

            return StringUtility.GetXmlWithoutNamespacesFromBytes2(fixedXml);
        }
    }
}