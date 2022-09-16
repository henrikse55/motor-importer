using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Importer.Client.Grains.Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Storage.Net.Blobs;
using U8Xml;

namespace Importer.Client.Grains;

public class EntryParserGrain : Grain, IEntryParser, IDisposable
{
    private readonly ILogger<EntryParserGrain> _logger;
    private readonly IBlobStorage _storage;
    private readonly MemoryStream _contentStream = new();

    public EntryParserGrain(
        IBlobStorage storage,
        ILogger<EntryParserGrain> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task Initialize()
    {
        await _storage.ReadToStreamAsync($"raw/{this.GetPrimaryKey()}", _contentStream);
        _contentStream.Position = 0;

        var parser = XmlParser.Parse(_contentStream);
        using MemoryStream stream = new MemoryStream();
        Utf8JsonWriter writer = new Utf8JsonWriter(stream, new JsonWriterOptions()
        {
            Indented = true
        });
        writer.WriteStartObject();
            
        await WriteContent(parser.Root.Name.ToString(),parser.Root, writer);

        writer.WriteEndObject();
        await writer.FlushAsync();

        stream.Position = 0;
        await _storage.WriteAsync($"parsed/{this.GetPrimaryKey()}", stream);
    }
    
    private async Task WriteContent(string name, XmlNode content, Utf8JsonWriter writer)
    {
        writer.WriteStartObject(name);
        foreach (XmlNode childNode in content.Children)
        {
            if (childNode.HasChildren)
            {
                if (childNode.Name.EndsWith("Samling") || childNode.Name.EndsWith("Liste"))
                {
                    //TODO
                }
                else if (childNode.Name.EndsWith("Struktur"))
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var b in MD5.HashData(childNode.AsRawString().AsSpan())) 
                        sb.Append(b.ToString("X2"));

                    var holderGrain = GrainFactory.GetGrain<IObjectHolder>(sb.ToString());
                    Guid? id = await holderGrain.GetObjectId();
                    if (id is null)
                    {
                        string s = childNode.AsRawString().ToString();
                        id = await holderGrain.Parse(s.AsImmutable());
                    }
                    // WriteContent(childNode.Name.ToString(), childNode, writer);
                    writer.WriteString(childNode.Name.AsSpan(), id.ToString());

                }
            }
            else
            {
                writer.WriteString(childNode.Name.AsSpan(), childNode.InnerText.AsSpan());
            }
        }
        writer.WriteEndObject();
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _contentStream.Dispose();
        GC.SuppressFinalize(this);
    }
}