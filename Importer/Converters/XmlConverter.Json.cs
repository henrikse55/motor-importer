using System;
using System.Xml;
using CommunityToolkit.HighPerformance.Buffers;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace Importer.Converters;

public partial class XmlConverter
{
    public static string ConvertToJson(ReadOnlySpan<byte> content)
    {
        string patchedXml = string.Empty;//PatchXmlData(content);

        XmlDocument document = new XmlDocument();
        document.LoadXml(patchedXml);
        return JsonConvert.SerializeXmlNode(document, Formatting.None, true);
    }
}