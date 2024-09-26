using System.Buffers;
using System.Diagnostics.Metrics;
using CommunityToolkit.HighPerformance.Buffers;
using Importer.Converters;
using Importer.Readers;
using Importer.Utility;
using Microsoft.IO;

namespace Importer.Performance;

public sealed class PerformanceReader : ReaderBase
{
    private readonly Meter _meter = new Meter("Motor.Performance");
    private readonly RecyclableMemoryStreamManager _memoryStreamManager = new();

    private readonly Counter<int> _counter;
    
    /// <inheritdoc />
    public PerformanceReader(CancellationToken cancellationToken) : base(cancellationToken)
    {
        _counter = _meter.CreateCounter<int>("Xml Entry");
    }

    /// <inheritdoc />
    protected override void PresentEntry(ReadOnlySequence<byte> entry)
    {
        using var util = StringUtility.GetXmlWithoutNamespacesStream(entry, _memoryStreamManager);
        using var memoryStream = XmlConverter.ConvertToU8(util, _memoryStreamManager);
        Console.WriteLine($"{memoryStream.Length} bytes");
        _counter.Add(1);
    }
}