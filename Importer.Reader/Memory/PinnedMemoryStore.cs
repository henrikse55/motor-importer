using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Importer.Reader.Memory;

public record struct MemoryItem(int Length, ReadOnlyMemory<byte> Buffer);

public class PinnedMemoryStore : IMemoryStore
{
    private readonly Meter _meter = new Meter("Motor.Store");
    private readonly ConcurrentDictionary<Guid, MemoryItem> _content = new();

    private readonly Counter<int> _getCounter;

    public PinnedMemoryStore()
    {
        _getCounter = _meter.CreateCounter<int>("Memory Store fetch", "item");
    }

    public Guid Store(ReadOnlySequence<byte> content)
    {
        Memory<byte> buffer = GC.AllocateUninitializedArray<byte>((int)content.Length, true);
        content.CopyTo(buffer.Span);

        return Store(buffer);
    }

    public Guid Store(ReadOnlyMemory<byte> buffer)
    {
        Guid id = Guid.NewGuid();
        _content[id] = new MemoryItem(buffer.Length, buffer);
        
        return id;
    }

    public ReadOnlyMemory<byte> Get(Guid item)
    {
        _getCounter.Add(1);
        _content.Remove(item, out var value);
        return value.Buffer;
    }

    public int GetSize(Guid item)
    {
        return _content[item].Length;
    }
}