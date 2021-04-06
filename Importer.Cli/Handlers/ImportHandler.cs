using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Importer.Cli.Extensions;
using Importer.Cli.Options;
using Importer.Zip;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Importer.Cli.Handlers
{
    public class ImportHandler : IResolvableCommandHandler<ImportOptions>
    {
        private readonly ILogger<ImportHandler> _logger;
        private readonly OutputResolver _outputResolver;
        private ImportOptions? _options;
        
        public ImportHandler(
            ILogger<ImportHandler> logger,
            OutputResolver outputResolver)
        {
            _logger = logger;
            _outputResolver = outputResolver;
        }

        public async Task<int> InvokeAsync(ImportOptions options)
        {
            ThreadPool.SetMinThreads(32, 64);
            _options = options;
            _outputResolver.Resolve(_options.Output).Configure(options);

            if (string.IsNullOrEmpty(options.DataSource))
                throw new ArgumentNullException(nameof(options.DataSource));
            
            _logger.LogInformation($"Will stream using source: {options.DataSource} | IsRemote: {options.IsRemoteFtp}");

            Channel<ReaderResult> channel = Channel.CreateBounded<ReaderResult>(8000);
            Stream xmlStream = GetSourceStream();
            Task readerTask = MotorReader.ReadXmlFromStream(channel, xmlStream);

            await foreach (var result in channel.Reader.ReadAllAsync())
            {
                ThreadPool.QueueUserWorkItem(ProcessEntry, result, true);
            }

            return 0;
        }
        
        private Stream GetSourceStream()
        {
            if (_options.IsRemoteFtp)
                return GetRemoteSourceStream();

            if (_options.File.IsZip())
                return GetStreamFromZip();

            return _options.File.OpenRead();
        }

        private void ProcessEntry(ReaderResult result)
        {
            string json = XmlConverter.ConvertToJson(result);
            BsonDocument document = BsonDocument.Parse(json);
            
            IdHash(result, document);

            var output = _outputResolver.Resolve(_options.Output);
            output.Present(document);
        }

        private static void IdHash(ReaderResult result, BsonDocument document)
        {
            document["_id"] = Guid.NewGuid();
        }

        private Stream GetRemoteSourceStream() 
            => RemoteFile.GetRemoteFileAsStream(_options.DataSource);
        
        private Stream GetStreamFromZip()
        {
            _logger.LogInformation("Opening as zip");
            ZipArchive archive = new ZipArchive(_options.File.OpenRead(), ZipArchiveMode.Read);
            ZipArchiveEntry entry = archive.Entries.First();
            return entry.Open();
        }
    }
}