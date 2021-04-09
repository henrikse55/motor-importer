using Microsoft.Extensions.DependencyInjection;
using Prometheus;

namespace Importer.Metrics.Extensions
{
    public static class ServiceMetricsExtensions
    {
        public static void AddMetricServer(this IServiceCollection collection)
        {
            collection.AddSingleton<IMetricServer>(provider => new MetricServer(8080));
            collection.AddHostedService<MetricService>();
            collection.AddEventSourceListener();
        }

        public static void AddEventSourceListener(this IServiceCollection collection)
        {
            EventSourceMetricListener eventSourceMetricListener = new EventSourceMetricListener();
            collection.AddSingleton(provider => eventSourceMetricListener);
        }
    }
}