using System;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Importer.Cli
{
    public static class CommandResolver
    {
        public static ICommandHandler Resolve<T>() 
            => CommandHandler.Create<T, IHost>(DoResolve);

        private static Task<int> DoResolve<T>(T options, IHost host)
        {
            IServiceProvider? provider = host.Services;
            IResolvableCommandHandler<T> commandHandler = (IResolvableCommandHandler<T>?) provider.GetService(typeof(IResolvableCommandHandler<T>))
                                                                      ?? throw new NullReferenceException();
            return commandHandler.InvokeAsync(options);
        }
    }
}