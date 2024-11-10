using System;
using System.Buffers;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using CommunityToolkit.HighPerformance.Buffers;

using Microsoft.IO;

using U8Xml;

namespace Importer.Converters;

public partial class XmlConverter
{
    private static readonly JsonWriterOptions JsonWriterOptions = new()
    {
        SkipValidation = true,
        Indented = false,
    };

    public static void ConvertToU8(ReadOnlySpan<byte> item, IBuffer<byte> jsonBuffer)
    {
        using var parser = XmlParser.Parse(item);

        Utf8JsonWriter writer = new(jsonBuffer, JsonWriterOptions);
            
        writer.WriteStartObject();

        XmlNode root = parser.Root;
        WriteContent(ref root, writer);

        writer.WriteEndObject();
        writer.Flush();
    }

    private static void WriteContent(ref XmlNode content, Utf8JsonWriter writer)
    {
        writer.WriteStartObject(content.Name.AsSpan());
        foreach (XmlNode childNode in content.Children)
        {
            XmlNode child = childNode;
            RawString childNodeName = childNode.Name;
            if (childNode.HasChildren)
            {
                if (childNodeName.EndsWith("Samling"u8) || childNodeName.EndsWith("Liste"u8))
                {
                    WriteArrayContent(ref child, writer);
                }
                else if (childNodeName.EndsWith("Struktur"u8))
                {
                    WriteContent(ref child, writer);
                }
            }
            else
            {
                writer.WriteString(childNode.Name.AsSpan(), childNode.InnerText.AsSpan());
            }
        }
        writer.WriteEndObject();
    }
    
    private static void WriteArrayContent(ref XmlNode childNode, Utf8JsonWriter writer)
    {
        writer.WriteStartArray(childNode.Name.AsSpan());
        foreach (var arrayNode in childNode.Children)
        {
            XmlNode content = arrayNode;
            
            writer.WriteStartObject();
            WriteContent(ref content, writer);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }
}