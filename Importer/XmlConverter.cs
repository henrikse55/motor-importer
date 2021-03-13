using System;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml;
using Importer.Extensions;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace Importer
{
    public static class XmlConverter
    {
        public static string ConvertToJson(ReaderResult item)
        {
            string patchedXml = PatchXmlData(item.Content);
            
            XmlDocument document = new XmlDocument();
            document.LoadXml(patchedXml);
            return JsonConvert.SerializeXmlNode(document, Formatting.None, true);
        }

        private static string PatchXmlData(Span<byte> content)
        {
            content = FindStartOfXml(content);
            if(content.IsEmpty)
                return string.Empty;

            Span<byte> expanded = AddEndTag(content);

            string xmlText = Encoding.UTF8.GetString(expanded);
            return xmlText.Replace("ns:", "");
        }

        private static Span<byte> AddEndTag(ReadOnlySpan<byte> content)
        {
            int finalLength = content.Length + Constants.EndingTagBytes.Length;
            Span<byte> finalXmlContent = GC.AllocateUninitializedArray<byte>(finalLength);
            
            content.CopyTo(finalXmlContent);
            Constants.EndingTagBytes.CopyTo(finalXmlContent.Slice(content.Length));
            return finalXmlContent;
        }

        private static Span<byte> FindStartOfXml(Span<byte> content)
        {
            int index = content.IndexOf(Constants.StartTagBytes);
            if (index == -1)
                return Span<byte>.Empty;

            return content.Slice(index);
        }
    }
}