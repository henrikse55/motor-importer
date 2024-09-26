using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using Humanizer;

namespace Importer.FasterKv;


[EventSource(Name = "Motor")]
public sealed class MotorMeter : EventSource
{
    public static readonly MotorMeter Log = new MotorMeter();

    private long count = 0;
    private readonly IncrementingEventCounter _entriesCounter;
    private readonly IncrementingEventCounter _entrySizesCounter;
    private readonly IncrementingEventCounter _patchedXmlEntriesCounter;
    private readonly IncrementingEventCounter _fasterHappyPaths;
    private readonly IncrementingEventCounter _fasterSlowPaths;
    private readonly EventCounter _averageBytes;
    
    private MotorMeter()
    {
        _entriesCounter = new IncrementingEventCounter("entries-scan-split", this)
        {
            DisplayName = "Xml Entry Split",
            DisplayUnits = "Xml Objects"
        };
        
        _entrySizesCounter = new IncrementingEventCounter("entries-size", this)
        {
            DisplayName = "Xml splits log sizes",
            DisplayUnits = "GB"
        };

        _patchedXmlEntriesCounter = new IncrementingEventCounter("entries-patched", this)
        {
            DisplayName = "Xml Entries Patched",
            DisplayUnits = "Xml Objects"
        };
        
        _fasterHappyPaths = new IncrementingEventCounter("single-segments-path", this)
        {
            DisplayName = "Faster Happy Path (No mem-copy)",
            DisplayUnits = "Xml Objects"
        };
        
        _fasterSlowPaths = new IncrementingEventCounter("single-slow-path", this)
        {
            DisplayName = "Faster Slow Path (Slow Mem-Copy)",
            DisplayUnits = "Xml Objects"
        };

        _averageBytes = new EventCounter("average-xml-bytes", this)
        {
            DisplayName = "Average Xml Size",
            DisplayUnits = "bytes",
        };
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReportScanLoop(int scanLoopCounter)
    {
        _entriesCounter.Increment(scanLoopCounter);
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReportEntryPatched(long byteSize)
    {
        _patchedXmlEntriesCounter.Increment();
        _averageBytes.WriteMetric(byteSize);
    }
    
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReportBytesWritten(long size)
    {
        _entrySizesCounter.Increment(size.Bytes().Gigabytes);
    }
    
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReportSlowPath()
    {
        _fasterSlowPaths.Increment();
    }
    
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReportFasterHappyPath()
    {
        _fasterHappyPaths.Increment();
    }
}