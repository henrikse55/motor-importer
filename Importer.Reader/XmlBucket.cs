using System.Buffers;
using Importer.Reader.Memory;
using Microsoft.Toolkit.HighPerformance.Buffers;

namespace Importer.Reader;

public readonly struct XmlBucket : IDisposable
{
    private readonly List<int> _sizes = new();
    private readonly ArrayPoolBufferWriter<byte> _bufferWriter;

    public XmlBucket(int sizeHint)
    {
        _bufferWriter = new ArrayPoolBufferWriter<byte>(sizeHint);
    }

    public void WriteEntry(ReadOnlySequence<byte> entry)
    {
        int entryLength = (int)entry.Length;
        
        Span<byte> buffer = _bufferWriter.GetSpan(entryLength);
        entry.CopyTo(buffer);
        _bufferWriter.Advance(entryLength);
        
        _sizes.Add(entryLength);
    }

    public IEnumerable<Guid> WriteToStore(IMemoryStore store)
    {
        Memory<byte> largeContent = GC.AllocateUninitializedArray<byte>(_bufferWriter.WrittenCount, true);
        _bufferWriter.WrittenMemory.CopyTo(largeContent);
        foreach (int size in _sizes)
        {
            ReadOnlyMemory<byte> slice = largeContent[..size];
            yield return store.Store(slice);
            largeContent = largeContent[size..];
        }
    }

    public void Dispose()
    {
        _bufferWriter.Dispose();
    }
}