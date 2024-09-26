using System;

namespace Importer;

public sealed class SequentialGuidProducer
{
    private ulong first = 0;
    private readonly byte[] zeros = new byte[8];
    public Guid GetNextGuid()
    {
        lock (this)
        {
            first++;
            byte[] content = BitConverter.GetBytes(first);
            return new Guid([..content, ..zeros]);   
        }
    }
}