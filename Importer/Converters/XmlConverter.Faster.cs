using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;

using FASTER.core;
using Importer.Entries;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using U8Xml;

namespace Importer.Converters;

public partial class XmlConverter : IDisposable
{
    private readonly SequentialGuidProducer _producer = new();
    private static readonly FasterKVSettings<SpanByte, SpanByte> FasterKvSettings = new(
        "/storage/motor/duplication",
        true)
    {
        LogDevice = Devices.CreateLogDevice("/storage/motor/duplication/duplication.log"),
        ObjectLogDevice = Devices.CreateLogDevice("/storage/motor/duplication/duplication.obj.log")
    };

    private readonly FasterKV<SpanByte, SpanByte> _store = new(FasterKvSettings);
    private SpanByteFunctions<object> _spanByteFunctions = new();
    private readonly ClientSession<SpanByte, SpanByte, SpanByte, SpanByteAndMemory, object, SpanByteFunctions<object>> _session;
    private static readonly SearchValues<byte> _searchValues = SearchValues.Create([(byte)' ', (byte)'\n', (byte)'<', (byte)'>', (byte)'/', (byte)':', (byte)'-']);
    
    private readonly ILogger<XmlConverter> _logger;
    
    public XmlConverter()
    {
        _logger = NullLogger<XmlConverter>.Instance;
        _session = _store.For(_spanByteFunctions).NewSession<SpanByteFunctions<object>>();
    }
    
    public XmlConverter(ILogger<XmlConverter> logger)
    {
        _logger = logger;
        _session = _store.For(_spanByteFunctions).NewSession<SpanByteFunctions<object>>();
    }

    public EntryObject ConvertToEntry(ReadOnlyMemory<byte> rawXmlEntry)
    {
        using var parser = XmlParser.Parse(rawXmlEntry.Span);
        return WriteContent(parser.Root.Name.AsSpan(), parser.Root);
    }

    private EntryObject WriteContent(ReadOnlySpan<byte> name, XmlNode content)
    {
        var spaceLessKey = ReadKey(content.AsRawString().AsSpan());
        var key = SpanByte.FromFixedSpan(spaceLessKey);
        var (lookupResult, output) = _session.Read(key);
        if (lookupResult.IsPending)
            _session.CompletePending(wait: true);
        
        if (lookupResult.Found)
        {
            EntryObject? entryObject =
                JsonSerializer.Deserialize<EntryObject>(output.Memory.Memory.Span[..output.Length]);
            return entryObject!;
        }

        
        List<EntryObject> fieldEntries = [];
        foreach (XmlNode childNode in content.Children)
        {
            if (childNode.HasChildren)
            {
                //TODO: Crawl down the tree to populate
                if (childNode.Name.EndsWith("Samling") || childNode.Name.EndsWith("Liste"))
                {
                    EntryObject e = WriteArrayContent(childNode);
                    if (e.Name is null)
                        continue;
                    fieldEntries.Add(e);
                }
                else if (childNode.Name.EndsWith("Struktur"))
                {
                    EntryObject e = WriteContent(childNode.Name.AsSpan(), childNode);
                    if (e is {Name: null} or {Children: []})
                        continue;
                    fieldEntries.Add(e);
                }
            }
            else
            {
                EntryObjectFieldValue entryObject = 
                    EntryObjectFieldValue.Create(_producer.GetNextGuid(),childNode.Name.AsSpan(), childNode.InnerText.AsSpan());
                if(entryObject is {Name: null} or {Value: null})
                    continue;
                fieldEntries.Add(entryObject);
            }
        }

        EntryObject writeContent = EntryObject.Create(_producer.GetNextGuid(), name, fieldEntries);
        
        ReadOnlySpan<byte> serializeToUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(writeContent);
        SpanByte json = SpanByte.FromFixedSpan(serializeToUtf8Bytes);
        
        var result = _session.Upsert(key, json);
        if (result.IsPending)
                _session.CompletePending();

        return writeContent;
    }

    private EntryObject WriteArrayContent(XmlNode childNode)
    {
        List<EntryObject> entries = [];
        foreach (var arrayNode in childNode.Children)
        {
            var node = WriteContent(arrayNode.Name.AsSpan(), arrayNode);
            entries.Add(node);
        }
        return EntryObject.Create(_producer.GetNextGuid(), childNode.Name.ToArray(), entries);
    }

    public ReadOnlySpan<byte> ReadKey(ReadOnlySpan<byte> content)
    {
        using ArrayPoolBufferWriter<byte> buffer = new();

        while (!content.IsEmpty)
        {
            int index = content.IndexOfAny(_searchValues);
            if (index == -1)
                break;

            if (index == 0)
            {
                int possibleRepeating = content.IndexOfAnyExcept(_searchValues);
                if (possibleRepeating > 0)
                {
                    content = content[possibleRepeating..];
                    continue;
                }
                content = content[1..];
                continue;
            }
            
            var slice = content[..index];
            
            WriteDownSize(slice, buffer);
            content = content[(index + 1)..];
        }

        return buffer.WrittenSpan;
    }

    private void WriteDownSize(in ReadOnlySpan<byte> content, in ArrayPoolBufferWriter<byte> writer)
    {
        Span<byte> buffer = stackalloc byte[content.Length];

        for (int i = 0; i < buffer.Length; i++)
        {
            byte b = content[i];
            buffer[i] = b switch
            {
                >= 65 and <= 90 => Convert.ToByte((Convert.ToChar(b) + 32)),
                _ => b
            };
        }
        
        writer.Write(buffer);
    }
    
    public void Dispose(bool disposing)
    {
        if (disposing)
        {
            _store.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }
}