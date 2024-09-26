using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Text.Json;

using CommunityToolkit.HighPerformance.Buffers;

using Microsoft.IO;

using U8Xml;

namespace Importer.Converters;

public partial class XmlConverter
{
    public static MemoryStream ConvertToU8(ReadOnlyMemory<byte> item)
    {
        ReadOnlySpan<byte> patchedXml = PatchXmlData(item.Span);
        using var parser = XmlParser.Parse(patchedXml);

        MemoryStream stream = new();
        Utf8JsonWriter writer = new (stream, new JsonWriterOptions()
        {
            SkipValidation = true
        });
            
        writer.WriteStartObject();
            
        WriteContent(parser.Root.Name.AsSpan(), parser.Root, writer);

        writer.WriteEndObject();
        writer.Flush();

        stream.Position = 0;
        return stream;
    }
    
    public static MemoryStream ConvertToU8(MemoryStream item, RecyclableMemoryStreamManager memoryStreamManager)
    {
        using var parser = XmlParser.Parse(item);

        MemoryStream stream = memoryStreamManager.GetStream();
        Utf8JsonWriter writer = new (stream, new JsonWriterOptions()
        {
            SkipValidation = true,
            Indented = false,
        });
            
        writer.WriteStartObject();
            
        WriteContent(parser.Root.Name.AsSpan(), parser.Root, writer);

        writer.WriteEndObject();
        writer.Flush();

        stream.Position = 0;
        return stream;
    }

    private static void WriteContent(ReadOnlySpan<byte> name, XmlNode content, Utf8JsonWriter writer)
    {
        writer.WriteStartObject(name);
        foreach (XmlNode childNode in content.Children)
        {
            if (childNode.HasChildren)
            {
                if (childNode.Name.EndsWith("Samling") || childNode.Name.EndsWith("Liste"))
                {
                    WriteArrayContent(writer, childNode);
                }
                else if (childNode.Name.EndsWith("Struktur"))
                {
                    WriteContent(childNode.Name.AsSpan(), childNode, writer);
                }
            }
            else
            {
                writer.WriteString(childNode.Name.AsSpan(), childNode.InnerText.AsSpan());
            }
        }
        writer.WriteEndObject();
    }

    private static void WriteArrayContent(Utf8JsonWriter writer, XmlNode childNode)
    {
        writer.WriteStartArray(childNode.Name.AsSpan());
        foreach (var arrayNode in childNode.Children)
        {
            writer.WriteStartObject();
            WriteContent(arrayNode.Name.AsSpan(), arrayNode, writer);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }
}