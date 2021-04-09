using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Diagnostics.Tracing.Session;
using Prometheus;
using Spectre.Console;

namespace Importer.Metrics
{
    public class EventSourceMetricListener : EventListener
    {
        private Dictionary<string, Gauge> _gauges = new Dictionary<string, Gauge>();

        private readonly Regex[] _eventPatterns = new[]
        {
            new Regex("(System\\.*)", RegexOptions.Compiled),
            new Regex("(Importer\\.*)", RegexOptions.Compiled)
        };
        
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            foreach (Regex regex in _eventPatterns)
            {
                if (regex.IsMatch(eventSource.Name))
                {
                    EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All, new Dictionary<string, string>
                    {
                        {"EventCounterIntervalSec", "1"}
                    });
                }
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName != "EventCounters" || eventData.Payload?.Count <= 0)
                return;

            foreach (var payload in eventData.Payload)
            {
                if (payload is not IDictionary<string, object>)
                    continue;
                
                if (payload is IDictionary<string, object> obj)
                {
                    Process(eventData, obj);
                }
            }
        }

        private void Process(EventWrittenEventArgs eventArgs, IDictionary<string, object> dictionary)
        {
            var gauge = GetGauge(dictionary);
            
            if (dictionary.TryGetValue("CounterType", out object value))
            {
                switch (value)
                {
                    case "Mean":
                        gauge.Set(Convert.ToDouble(dictionary["Mean"]));
                        break;
                    case "Sum":
                        double result = Convert.ToDouble(dictionary["Increment"]);
                        if (result == 0)
                            return;
                        gauge.Set(result);
                        break;
                    default:
                        Table table = new Table();
                        table.AddColumns(dictionary.Keys.ToArray());
                        table.AddRow(dictionary.Values.Select(x => x.ToString()).ToArray());
                        AnsiConsole.Render(table);
                        break;
                }
            }
        }

        private Gauge GetGauge(IDictionary<string, object> dictionary)
        {
            string name = (dictionary["Name"] as string).Replace("-", "_");
            if (_gauges.ContainsKey(name))
            {
                return _gauges[name];
            }

            return Prometheus.Metrics.CreateGauge(name, (string) dictionary["DisplayName"], name);
        }
    }
}