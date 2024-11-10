using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using CommunityToolkit.HighPerformance.Buffers;
using FASTER.core;
using Importer.Converters;
using Importer.Readers;
using Importer.Utility;
using Microsoft.Extensions.Logging;
using Microsoft.IO;

namespace Importer.Performance;

public sealed class PerformanceReader : ReaderBase
{
    private readonly FasterLog _log;
    private readonly Meter _meter = new("Motor.Performance");

    private readonly IBuffer<byte> _xmlBuffer = new ArrayPoolBufferWriter<byte>();
    private readonly IBuffer<byte> _jsonBuffer = new ArrayPoolBufferWriter<byte>();
    
    private readonly ObservableCounter<long> _counter;
    
    private long _jsonEntries;
    
    /// <inheritdoc />
    public PerformanceReader(FasterLog log, CancellationToken cancellationToken) : base(cancellationToken)
    {
        _log = log;
        _counter = _meter.CreateObservableCounter("Xml Entries", () => _jsonEntries);
    }

    /// <inheritdoc />
    protected override void PresentEntry(ref ReadOnlySequence<byte> entry)
    {
        StringUtility.GetXmlWithoutNamespacesStream(ref entry, _xmlBuffer);

        var span = _xmlBuffer.WrittenSpan;
        var startingPosition = span.IndexOf("<Statistik>"u8);
        if (startingPosition == -1)
            startingPosition = 0;
        
        XmlConverter.ConvertToU8(span[startingPosition..], _jsonBuffer);

        var item = _jsonBuffer.WrittenSpan;
        _log.Enqueue(item);
        
        _jsonBuffer.Clear();
        _xmlBuffer.Clear();
        _jsonEntries++;
    }

    /// <inheritdoc />
    protected override void CommitScan()
    {
        _log.Commit();
    }
}