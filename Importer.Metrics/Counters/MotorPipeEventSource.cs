using System;
using System.Diagnostics.Tracing;
using System.Threading;
using Meziantou.Framework;

namespace Importer.Metrics.Counters
{
    [EventSource(Name = SourceName)]
    public class MotorPipeEventSource : EventSource
    {
        private const string SourceName = "Importer.Pipe";

        public static readonly MotorPipeEventSource Log = new MotorPipeEventSource();

        private IncrementingEventCounter _scanInvocationCounter;
        private EventCounter _scanInvocationDurationCounter;
        private IncrementingEventCounter _scanEntriesFoundCounter;

        protected MotorPipeEventSource() : base(SourceName, EventSourceSettings.EtwSelfDescribingEventFormat)
        {
        }

        [NonEvent]
        public ValueStopwatch? ScanForXmlStart()
        {
            if (!IsEnabled())
                return null;
            
            ScanStart();
            
            return ValueStopwatch.StartNew();
        }

        [Event(1, Level = EventLevel.Informational, Message = "Scan Started")]
        private void ScanStart()
        {
            _scanInvocationCounter.Increment();
        }

        [NonEvent]
        public void ScanForXmlStop(ValueStopwatch stopwatch)
        {
            if (!IsEnabled())
                return;
            
            ScanStop(stopwatch.IsActive ? stopwatch.GetElapsedTime().TotalMilliseconds : -1.0);
        }
        
        [Event(2, Level = EventLevel.Informational, Message = "Scan Stopped")]
        private void ScanStop(double duration)
        {
            if (!IsEnabled())
                return;
            
            _scanInvocationDurationCounter.WriteMetric(duration);
        }

        [Event(3, Level = EventLevel.Informational, Message = "Scanned XML Entries Count")]
        public void FoundEntries(int amount)
        {
            if (!IsEnabled())
                return;
            
            _scanEntriesFoundCounter.Increment(amount);
        }
        
        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Enable)
            {
                _scanInvocationCounter = new IncrementingEventCounter("scan-invocations", this)
                {
                    DisplayName = "Scan Invocations",
                    DisplayRateTimeScale = TimeSpan.FromSeconds(1)
                };

                _scanInvocationDurationCounter = new EventCounter("scan-invocation-duration", this)
                {
                    DisplayName = "Scan Duration",
                    DisplayUnits = "ms"
                };

                _scanEntriesFoundCounter = new IncrementingEventCounter("scan-entries-count", this)
                {
                    DisplayName = "Scan XML Count",
                    DisplayUnits = "Entries",
                    DisplayRateTimeScale = TimeSpan.FromSeconds(1)
                };
            }
        }
    }
}