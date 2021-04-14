using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Importer.Metrics.Counters;
using Microsoft.Toolkit.HighPerformance.Buffers;

namespace Importer.Readers
{
    public class MotorReader : ReaderBase
    {
        private readonly ChannelWriter<ReaderBatchResult> _resultChannel;
        private List<MemoryOwner<byte>> _availableEntries = new List<MemoryOwner<byte>>(250);
        
        public static Task ReadXmlFromStream(ChannelWriter<ReaderBatchResult> resultChannel, Stream xmlStream, CancellationToken token = default) 
            => new MotorReader(resultChannel, token).Read(xmlStream);

        public MotorReader(ChannelWriter<ReaderBatchResult> resultChannel, CancellationToken cancellationToken) : base(cancellationToken)
        {
            _resultChannel = resultChannel;
        }

        protected override void PresentEntry(MemoryOwner<byte> entry)
        {
            _availableEntries.Add(entry);
        }

        protected override void ScanComplete()
        {
            PresentReaderBatch(new ReaderBatchResult(_availableEntries));
            _availableEntries = new List<MemoryOwner<byte>>();
        }

        protected override void ReaderComplete()
        {
            _resultChannel.Complete();
        }

        private void PresentReaderBatch(ReaderBatchResult batchResult)
        {
            MotorPipeEventSource.Log.FoundEntries(batchResult.Batch.Count);
            var task = _resultChannel.WriteAsync(batchResult, _cancellationToken);
            if (!task.IsCompleted)
            {
                task.AsTask().Wait(_cancellationToken);
            }
        }
    }
}