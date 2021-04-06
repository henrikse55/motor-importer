using System.IO;
using Importer.Cli.Commands;
using Importer.Cli.Outputs;

namespace Importer.Cli.Options
{
    public class ImportOptions
    {
        public string? DataSource { get; set; }
        
        public OutputMode Output { get; set; }

        public string? Auth { get; set; }
        public string? Mongo { get; set; }

        public bool IsRemoteFtp => DataSource?.StartsWith("ftp://") ?? false;
        public FileInfo File => new FileInfo(DataSource!);
    }
}