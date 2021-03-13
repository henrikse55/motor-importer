using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Importer.Cli.Outputs
{
    public class DumpOutput : IOutput
    {
        public async Task Start(ChannelReader<ReaderResult> reader)
        {
            await foreach (var _ in reader.ReadAllAsync())
            {
                //NOOP - Dumping
            }
        }
        
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}