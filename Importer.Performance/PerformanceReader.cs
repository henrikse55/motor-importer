using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Sockets;
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
    
    private readonly ObservableCounter<long> _counter;
    
    private readonly ObjectPool<ArrayPoolBufferWriter<byte>> arrayPoolBufferWriter =
        new DefaultObjectPool<ArrayPoolBufferWriter<byte>>(new DefaultPooledObjectPolicy<ArrayPoolBufferWriter<byte>>());
    
    private long _jsonEntries;
    
    /// <inheritdoc />
    public PerformanceReader(FasterLog log, CancellationToken cancellationToken) : base(cancellationToken)
    {
        _log = log;
        _counter = _meter.CreateObservableCounter("Xml Entries", () => _jsonEntries, "docs");
    }

    /// <inheritdoc />
    protected override async Task PresentEntry(XmlBatchItem entry)
    {
        var xmlEntries = entry.GetXmlItems();

        await Parallel.ForEachAsync(xmlEntries, (sequence, token) =>
        {
            ArrayPoolBufferWriter<byte> xmlBuffer = arrayPoolBufferWriter.Get();
            ArrayPoolBufferWriter<byte> jsonBuffer = arrayPoolBufferWriter.Get();
            
            StringUtility.GetXmlWithoutNamespacesStream(sequence, xmlBuffer);

            ReadOnlySpan<byte> span = xmlBuffer.WrittenSpan;
            var startingPosition = span.IndexOf("<Statistik>"u8);
            if (startingPosition == -1)
                startingPosition = 0;
        
            XmlConverter.ConvertToU8(span[startingPosition..], jsonBuffer);

            var jsonBufferWrittenSpan = jsonBuffer.WrittenSpan;
            // _log.Enqueue(jsonBufferWrittenSpan);
            //
            jsonBuffer.Clear();
            xmlBuffer.Clear();
            
            arrayPoolBufferWriter.Return(xmlBuffer);
            arrayPoolBufferWriter.Return(jsonBuffer);
            
            Interlocked.Increment(ref _jsonEntries);

            return ValueTask.CompletedTask;
        });
    }

    /// <inheritdoc />
    protected override void CommitScan()
    {
        // _log.Commit();
    }
}