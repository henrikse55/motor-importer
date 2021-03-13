using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Importer.Cli.Outputs
{
    public class ConsoleOutput : IOutput
    {
        private readonly List<ReaderResult> _outputBag = new List<ReaderResult>();
        private Timer? _timer;

        public async Task Start(ChannelReader<ReaderResult> reader)
        {
            _timer = new Timer(PrintTimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

            await foreach (var result in reader.ReadAllAsync())
            {
                lock (_outputBag)
                {
                    _outputBag.Add(result);
                }
            }
        }

        private void PrintTimerCallback(object? state)
        {
            lock (_outputBag)
            {
                if (_outputBag.Count <= 0)
                    return;
                
                int textLength = _outputBag.Sum(x => x.Content.Length);
                string largeText = string.Create(textLength, _outputBag, (span, list) =>
                {
                    foreach (ReaderResult readerResult in list)
                    {
                        Encoding.UTF8.GetChars(readerResult.Content, span);
                        span = span.Slice(readerResult.Content.Length);
                    }
                });
                
                Console.WriteLine(largeText);
                _outputBag.Clear();
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}