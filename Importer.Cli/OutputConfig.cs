using Importer.Cli.Options;

namespace Importer.Cli
{
    public struct OutputConfig
    {
        public string Mongo { get; set; }
        public string? Auth { get; set; }

        public static implicit operator OutputConfig(ImportOptions options) => new OutputConfig()
        {
            Auth = options.Auth,
            Mongo = options.Mongo!
        };
    }
}