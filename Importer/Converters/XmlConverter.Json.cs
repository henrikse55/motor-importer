using System;
using System.Buffers;
using System.Xml;
using Microsoft.Toolkit.HighPerformance.Buffers;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace Importer.Converters
{
    public partial class XmlConverter
    {
        public static string ConvertToJson(MemoryOwner<byte> content)
            => new XmlConverter().ConvertToJson(content.Span);

        private string ConvertToJson(ReadOnlySpan<byte> content)
        {
            string patchedXml = PatchXmlData(content);

            XmlDocument document = new XmlDocument();
            document.LoadXml(patchedXml);
            return JsonConvert.SerializeXmlNode(document, Formatting.None, true);
        }
    }
}