using System;
using System.Text.Json;

using CommunityToolkit.HighPerformance.Buffers;

using Microsoft.Extensions.ObjectPool;

using U8Xml;

namespace Importer.Converters;


public sealed class XmlConverter : IResettable
{
    private static readonly JsonWriterOptions JsonWriterOptions = new()
    {
        SkipValidation = true,
        Indented = false,
    };
    
    private readonly ArrayPoolBufferWriter<byte> _buffer = new();
    private readonly Utf8JsonWriter _writer;

    public XmlConverter()
    {
        _writer = new Utf8JsonWriter(_buffer, JsonWriterOptions);
    }

    public object WrittenMemory => _buffer.WrittenMemory;

    public void ConvertToU8(ReadOnlySpan<byte> item)
    {
        using var parser = XmlParser.Parse(item);
        
        _writer.WriteStartObject();

        XmlNode root = parser.Root;
        WriteContent(ref root, _writer);

        _writer.WriteEndObject();
        _writer.Flush();
    }

    private void WriteContent(ref XmlNode content, Utf8JsonWriter writer)
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
    
    private void WriteArrayContent(ref XmlNode childNode, Utf8JsonWriter writer)
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

    public bool TryReset()
    {
        _buffer.Clear();
        _writer.Reset();
        
        return true;
    }
}