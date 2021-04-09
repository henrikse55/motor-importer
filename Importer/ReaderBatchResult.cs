using System;
using System.Collections.Generic;
using Microsoft.Toolkit.HighPerformance.Buffers;

namespace Importer
{
    public readonly struct ReaderBatchResult
    {
        public readonly List<MemoryOwner<byte>> Batch;

        public ReaderBatchResult(List<MemoryOwner<byte>> batch)
        {
            Batch = batch;
        }
    }
}