using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Importer.Cli.Extensions;
using Importer.Cli.Options;
using Importer.Cli.Outputs;
using Importer.Converters;
using Importer.Readers;
using Importer.Zip;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Importer.Cli.Handlers
{
    public class ImportHandler : IResolvableCommandHandler<ImportOptions>
    {
        private readonly ILogger<ImportHandler> _logger;
        private readonly OutputResolver _outputResolver;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        
        private ImportOptions? _options;
        
        public ImportHandler(
            ILogger<ImportHandler> logger,
            OutputResolver outputResolver, 
            IHostApplicationLifetime applicationLifetime)
        {
            _logger = logger;
            _outputResolver = outputResolver;
            _applicationLifetime = applicationLifetime;

            applicationLifetime.ApplicationStopping.Register(() => _tokenSource.Cancel());
        }

        public async Task<int> InvokeAsync(ImportOptions options)
        {
            VerifyOptions(options);

            Channel<ReaderBatchResult> channel = Channel.CreateUnbounded<ReaderBatchResult>();
            
            Stream xmlStream = GetSourceStream();
            Task reader = MotorReader.ReadXmlFromStream(channel, xmlStream, _tokenSource.Token);
            Task readLoop = StartContentEnqueueLoop(channel);

            await Task.WhenAll(reader, readLoop);
            
            _logger.LogInformation($"Done parsing data, {channel.Reader.Count}");
            return 0;
        }

        private void VerifyOptions(ImportOptions options)
        {
            _options = options;

            if (string.IsNullOrEmpty(options.DataSource))
                throw new ArgumentNullException(nameof(options.DataSource));

            _logger.LogInformation($"Will stream using source: {options.DataSource} | IsRemote: {options.IsRemoteFtp}");
        }
        
        private async Task StartContentEnqueueLoop(Channel<ReaderBatchResult> channel)
        {
            await foreach (var result in channel.Reader.ReadAllAsync(_tokenSource.Token))
            {
                ThreadPool.QueueUserWorkItem(ProcessEntry, result, true);
            }
        }
        
        private void ProcessEntry(ReaderBatchResult batchResult)
        {
            IOutput output = _outputResolver.Resolve(_options!.Output);
            output.Configure(_options);
            
            XmlConverter converter = new XmlConverter();
            foreach (var owner in batchResult.Batch)
            {
                BsonDocument document = converter.ConvertToBson(owner);
            
                output.Present(document);
                owner.Dispose();
            }
        }

        private Stream GetSourceStream()
        {
            if (_options!.IsRemoteFtp)
                return GetRemoteSourceStream();

            if (_options.File.IsZip())
                return GetStreamFromZip();

            return _options.File.OpenRead();
        }

        private Stream GetRemoteSourceStream() 
            => RemoteFile.GetRemoteFileAsStream(_options!.DataSource);
        
        private Stream GetStreamFromZip()
        {
            _logger.LogInformation("Opening as zip");
            ZipArchive archive = new ZipArchive(_options!.File.OpenRead(), ZipArchiveMode.Read);
            ZipArchiveEntry entry = archive.Entries.First();
            return entry.Open();
        }
    }
}