using System.CommandLine;

namespace Importer.Cli
{
    public class Command<TOption> : Command
    {
        public Command(string name, string description) : base(name, description)
        {
            Handler = CommandResolver.Resolve<TOption>();
        }
    }
}