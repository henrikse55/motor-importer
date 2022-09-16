using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using Importer.Extensions;
using Microsoft.Toolkit.HighPerformance.Buffers;
using MongoDB.Bson;

namespace Importer.Converters
{
    public partial class XmlConverter
    {
        private readonly ReadOnlyMemory<char> _samling = "Samling".AsMemory();

        private readonly XmlReaderSettings _settings = new()
        {
            Async = false,
            ValidationType = ValidationType.None,
            ValidationFlags = XmlSchemaValidationFlags.None,
            IgnoreWhitespace = true,
            IgnoreComments = true,
            CheckCharacters = false,
            CloseInput = true
        };

        private readonly ReadOnlyMemory<char> _struktur = "Struktur".AsMemory();

        public BsonDocument ConvertToBson(MemoryOwner<byte> item)
        {
            string patchedXml = string.Empty;//PatchXmlData(item.Span);
            TextReader textReader = new StringReader(patchedXml);

            string hashId = item.GetByteHash();

            XmlReader reader = XmlReader.Create(textReader, _settings);
            BsonDocument document = new()
            {
                ["_id"] = hashId
            };
            while (reader.Read())
            {
                string nodeName = StringPool.Shared.GetOrAdd(reader.Name);
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        XmlReader xmlSubtree = reader.ReadSubtree();
                        CopyContentToBson(nodeName, document, xmlSubtree);
                        break;
                }
            }

            return document;
        }

        //Could a non-recursive method be used here?
        //Possibly using a stack?
        private void CopyContentToBson(string name, BsonDocument document, XmlReader reader)
        {
            reader.Read(); //Skip the root element
            while (reader.Read())
            {
                string nodeName = StringPool.Shared.GetOrAdd(reader.Name);
                ReadOnlySpan<char> nodeNameSpan = nodeName.AsSpan();
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        BsonDocument nodeDocument = document;
                        if (nodeNameSpan.EndsWith(_struktur.Span))
                        {
                            nodeDocument = new BsonDocument();
                            document[nodeName] = nodeDocument;
                            CopyContentToBson(nodeName, nodeDocument, reader.ReadSubtree());
                        }
                        else if (nodeNameSpan.EndsWith(_samling.Span))
                        {
                            document[nodeName] = CopyArray(reader.ReadSubtree());
                        }
                        else
                        {
                            CopyContentToBson(nodeName, nodeDocument, reader.ReadSubtree());
                        }

                        break;
                    case XmlNodeType.Text:
                        document[name] = StringPool.Shared.GetOrAdd(reader.ReadString());
                        break;
                }
            }
        }

        private BsonArray CopyArray(XmlReader reader)
        {
            BsonArray array = new();
            while (reader.Read())
            {
                string nodeName = StringPool.Shared.GetOrAdd(reader.Name);
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        BsonDocument document = new();
                        CopyContentToBson(nodeName, document, reader.ReadSubtree());
                        array.Add(document);
                        break;
                }
            }

            return array;
        }
    }
}