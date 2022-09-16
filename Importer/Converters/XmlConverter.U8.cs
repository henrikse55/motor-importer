using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.IO;
using U8Xml;

namespace Importer.Converters
{
    public partial class XmlConverter
    {
        private static readonly RecyclableMemoryStreamManager RecyclableMemoryStreamManager;
        static XmlConverter()
        {
            RecyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        }

        public MemoryStream ConvertToU8(ReadOnlyMemory<byte> item)
        {
            try
            {
                ReadOnlySpan<byte> patchedXml = PatchXmlData(item.Span);
                using var parser = XmlParser.Parse(patchedXml);
            
                MemoryStream stream = new RecyclableMemoryStream(RecyclableMemoryStreamManager);
                Utf8JsonWriter writer = new (stream, new JsonWriterOptions()
                {
                    SkipValidation = true
                });
            
                writer.WriteStartObject();
            
                WriteContent(parser.Root.Name.AsSpan(),parser.Root, writer);

                writer.WriteEndObject();
                writer.Flush();

                stream.Position = 0;
                return stream;
            }
            catch (Exception e)
            {
                return new MemoryStream();
            }
        }

        [SkipLocalsInit]
        private void WriteContent(ReadOnlySpan<byte> name, XmlNode content, Utf8JsonWriter writer)
        {
            writer.WriteStartObject(name);
            foreach (XmlNode childNode in content.Children)
            {
                if (childNode.HasChildren)
                {
                    if (childNode.Name.EndsWith("Samling") || childNode.Name.EndsWith("Liste"))
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
    }
}