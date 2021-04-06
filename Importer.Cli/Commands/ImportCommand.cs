using System;
using System.CommandLine;
using Importer.Cli.Options;
using Importer.Cli.Outputs;

namespace Importer.Cli.Commands
{
    public class ImportCommand : Command<ImportOptions>
    {
        public ImportCommand() : base("import", "import the motor data from a local or remote location")
        {
            SetupOptions();
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
    }
}