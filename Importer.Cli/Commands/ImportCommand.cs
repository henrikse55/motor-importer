using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Importer.Cli.Extensions;
using Importer.Cli.Options;
using Importer.Cli.Outputs;
using Microsoft.Extensions.Logging;

namespace Importer.Cli.Commands
{
    public class ImportCommand : Command
    {
        public ImportCommand() : base("import", "import the motor data from a local or remote location")
        {
            SetupOptions();
            
            Handler = CommandHandler.Create<ImportOptions>(Handle);
        }

        private void SetupOptions()
        {
            var path = new Option<string>("--data-source")
            {
                Description = "Path to the data",
                IsRequired = true
            };
            path.AddAlias("--source");

            var output = new Option<OutputMode>("--output", () => OutputMode.Console)
            {
                Description = "Where to write the xml results to"
            };

            var auth = new Option<string>("--auth")
            {
                Description = "MongoDb credentials, ignored if output is not Mongo"
            };

            var mongo = new Option<string>("--mongo")
            {
                Description = "Comma separated list of mongo addresses"
            };

            AddOption(path);
            AddOption(output);
            AddOption(auth);
            AddOption(mongo);
        }

        private void Handle(ImportOptions options)
        {
            Stopwatch start = Stopwatch.StartNew();
            if (options.IsRemoteFtp)
            {
                HandleRemoteFetch(options);
            }
            else
            {
                HandleLocalData(options);
            }
            Console.WriteLine($"Completed in: {start.Elapsed}");
        }

        private void HandleRemoteFetch(ImportOptions options)
        {
            if (string.IsNullOrEmpty(options.DataSource))
                throw new ArgumentNullException(nameof(options.DataSource));
            
            Uri uri = new Uri(options.DataSource);

            if (uri.Scheme != "ftp")
                throw new InvalidOperationException();

            using RemoteFile remoteFile = new RemoteFile(uri);
            using Stream fileStream = remoteFile.GetStreamingFile();
            using IOutput output = GetOutputMethod(options);
            
            ReadXmlFromStream(fileStream, output);
        }
        
        private void HandleLocalData(ImportOptions options)
        {
            if (string.IsNullOrEmpty(options.DataSource))
                throw new ArgumentNullException(nameof(options.DataSource));

            if (!File.Exists(options.DataSource))
                throw new FileNotFoundException($"Unable to find {options.DataSource}");

            FileInfo info = new FileInfo(options.DataSource);
            
            using IOutput output = GetOutputMethod(options);
            using FileStream fileStream = info.OpenRead();
            if (info.IsZip())
            {
                using ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Read);
                ZipArchiveEntry xmlEntry = archive.Entries.First();
                using Stream xmlEntryStream = xmlEntry.Open();
                
                ReadXmlFromStream(xmlEntryStream, output);
            }
            else
            {
                ReadXmlFromStream(fileStream, output);
            }
        }

        private void ReadXmlFromStream(Stream stream, IOutput output)
        {
            Channel<ReaderResult> channel = Channel.CreateUnbounded<ReaderResult>();
            Task readerTask = MotorReader.ReadXmlFromStream(channel, stream);
            Task outputTask = output.Start(channel);

            readerTask.Wait();
            channel.Writer.Complete();
            outputTask.Wait();
        }

        private IOutput GetOutputMethod(ImportOptions options)
            => options.Output switch
            {
                OutputMode.Console => new ConsoleOutput(),
                OutputMode.Mongo => new MongoOutput(options),
                OutputMode.Dump => new DumpOutput(),
                _ => throw new ArgumentOutOfRangeException()
            };
    }
}