using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Sockets;
using System.Threading.Channels;
using CommunityToolkit.HighPerformance.Buffers;
using FASTER.core;
using Importer.Converters;
using Importer.Readers;
using Importer.Utility;
using Microsoft.Extensions.ObjectPool;

namespace Importer.Performance;

public sealed class PerformanceReader : ReaderBase
{
    private readonly FasterLog _log;
    private readonly Meter _meter = new("Motor.Performance");
    
    private readonly Counter<long> _counter;
    
    private readonly ObjectPool<ArrayPoolBufferWriter<byte>> arrayPoolBufferWriter =
        new DefaultObjectPool<ArrayPoolBufferWriter<byte>>(new DefaultPooledObjectPolicy<ArrayPoolBufferWriter<byte>>());

    private readonly ObjectPool<XmlConverter> _converterPool =
        new DefaultObjectPool<XmlConverter>(new DefaultPooledObjectPolicy<XmlConverter>());
    
    private long _jsonEntries;
    
    /// <inheritdoc />
    public PerformanceReader(FasterLog log, CancellationToken cancellationToken) : base(cancellationToken)
    {
        _log = log;
        _counter = _meter.CreateCounter<long>("Xml Entries");
    }

    /// <inheritdoc />
    protected override async Task PresentEntry(XmlBatchItem entry)
    {
        var xmlEntries = entry.GetXmlItems();
        _counter.Add(entry.GetWrittenCount());

        await Parallel.ForEachAsync(xmlEntries, (sequence, token) =>
        {
            ArrayPoolBufferWriter<byte> xmlBuffer = arrayPoolBufferWriter.Get();
            XmlConverter converter = _converterPool.Get();
            
            StringUtility.GetXmlWithoutNamespacesStream(sequence, xmlBuffer);

            ReadOnlySpan<byte> span = xmlBuffer.WrittenSpan;
            var startingPosition = span.IndexOf("<Statistik>"u8);
            if (startingPosition == -1)
                startingPosition = 0;
        
            converter.ConvertToU8(span[startingPosition..]);

            var jsonBufferWrittenSpan = converter.WrittenMemory;
            
            xmlBuffer.Clear();
            
            arrayPoolBufferWriter.Return(xmlBuffer);
            _converterPool.Return(converter);
            
            return ValueTask.CompletedTask;
        });
    }

    /// <inheritdoc />
    protected override void CommitScan()
    {
        // _log.Commit();
    }
}