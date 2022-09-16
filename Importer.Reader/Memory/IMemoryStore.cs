using System.Buffers;

namespace Importer.Reader.Memory;

public interface IMemoryStore
{
    public Guid Store(ReadOnlySequence<byte> content);

    public Guid Store(ReadOnlyMemory<byte> buffer);
    
    public ReadOnlyMemory<byte> Get(Guid item);

    public int GetSize(Guid item);
}