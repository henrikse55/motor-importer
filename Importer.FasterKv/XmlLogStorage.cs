using System.Buffers;
using System.Runtime.CompilerServices;
using FASTER.core;

namespace Importer.FasterKv;

public sealed class XmlLogStorage
{
    private readonly FasterLog _fasterLog;

    public XmlLogStorage()
    {
        var device = Devices.CreateLogDevice("/storage/motor/xml-log/raw.log", true);
        FasterLogSettings fasterLogSettings = new(null)
        {
            LogDevice = device
        };

        _fasterLog = new FasterLog(fasterLogSettings);
    }
    
    public void Enqueue(in IEnumerable<ReadOnlySequence<byte>> content)
    {
        foreach (ReadOnlySequence<byte> sequence in content)
        {
            if (sequence.IsSingleSegment)
            {
                _fasterLog.Enqueue(sequence.FirstSpan);
                MotorMeter.Log.ReportFasterHappyPath();
                continue;
            }
        
            int xmlEntryLength = (int)sequence.Length;
            MemoryPool<byte> memoryPool = MemoryPool<byte>.Shared;

            using IMemoryOwner<byte> buffer = memoryPool.Rent(xmlEntryLength);
            sequence.CopyTo(buffer.Memory.Span);
        
            _ = _fasterLog.Enqueue(buffer.Memory.Span[..xmlEntryLength]);
            MotorMeter.Log.ReportSlowPath();
        }

        _fasterLog.Commit();
    }
    
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(in ReadOnlySequence<byte> sequence)
    {
        if (sequence.IsSingleSegment)
        {
            _fasterLog.Enqueue(sequence.FirstSpan);
            MotorMeter.Log.ReportFasterHappyPath();
            return;
        }
        
        int xmlEntryLength = (int)sequence.Length;
        MemoryPool<byte> memoryPool = MemoryPool<byte>.Shared;      
        using IMemoryOwner<byte> buffer = memoryPool.Rent(xmlEntryLength);
        sequence.CopyTo(buffer.Memory.Span);
        
        _ = _fasterLog.Enqueue(buffer.Memory.Span[..xmlEntryLength]);
        MotorMeter.Log.ReportSlowPath();
    }

    public void Commit(bool wait = false)
    {
        _fasterLog.Commit(wait);
    }

    public async IAsyncEnumerable<(byte[] entry, int entryLength)> ReadCommittedXmlContent([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using FasterLogScanIterator? iter = _fasterLog.Scan(0, long.MaxValue);
        await foreach ((byte[]? bytes, int length, long _, long _) in iter.GetAsyncEnumerable(cancellationToken).ConfigureAwait(false))
        {
            yield return (bytes, length);
        }
    }
}