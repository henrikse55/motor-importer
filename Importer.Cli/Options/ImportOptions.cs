using Importer.Cli.Commands;

namespace Importer.Cli.Options
{
    public class ImportOptions
    {
        public string? DataSource { get; set; }
        
        public OutputMode Output { get; set; }

        public string? Auth { get; set; }
        public string? Mongo { get; set; }

        public bool IsRemoteFtp => DataSource?.StartsWith("ftp://") ?? false;
    }
}