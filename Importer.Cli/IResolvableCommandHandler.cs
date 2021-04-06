using System.Threading.Tasks;

namespace Importer.Cli
{
    interface IResolvableCommandHandler<in T>
    {
        Task<int> InvokeAsync(T options);
    }
}