using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Importer.Cli.Outputs
{
    public interface IOutput : IDisposable
    {
        Task Start(ChannelReader<ReaderResult> reader);
    }
}