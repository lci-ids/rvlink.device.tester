using RvLinkDeviceTester.Services;
using DynamicData;
using IDS.Portable.Common;
using ReactiveUI;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Linq;
using System.Reactive.Disposables;
using LoCap = OneControl.Direct.IdsCanAccessoryBle;

namespace RvLinkDeviceTester.Services
{
    public class LogViewerRealTimeService : Singleton<LogViewerRealTimeService>, ILogViewerRealTimeService, ILogEventSink, IDisposable
    {
        private readonly string[] LOCAP_SOURCES = new string[] { nameof(LoCap.GenericSensor.GenericSensorBle),
                                                                 nameof(LoCap.AwningSensor.AwningSensorBle),
                                                                 nameof(LoCap.BatteryMonitor.BatteryMonitorBle),
                                                                 nameof(LoCap.DoorLock.DoorLockBle),
                                                                 nameof(LoCap.TemperatureSensor.TemperatureSensorBle)
                                                               };

        private readonly ISourceCache<ILogEntry, long> logCache;
        private readonly IDisposable subscriptions;
        private readonly object _lock = new object();

        private LogViewerRealTimeService()
        {
            logCache = new SourceCache<ILogEntry, long>(p => p.LoggedOn.Ticks);

            var expirer = logCache
                .ExpireAfter(t => TimeSpan.FromMinutes(10), RxApp.TaskpoolScheduler)
                .Subscribe();

            LogEntries = logCache.AsObservableCache();
            
            subscriptions = new CompositeDisposable(LogEntries, expirer);
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent is null) throw new ArgumentNullException(nameof(logEvent));

            if (logEvent.Properties.TryGetValue("SourceContext", out var value))
            {
                var tag = value.ToString("l", null);

                if (LOCAP_SOURCES.Any(p => p.Equals(tag, StringComparison.OrdinalIgnoreCase)))
                {
                    var message = logEvent.RenderMessage();

                    var entry = new LogEntryItem
                    {
                        LoggedOn = logEvent.Timestamp,
                        DeviceName = tag,
                        Severity = logEvent.Level.ToString(),
                        Message = message
                    };

                    lock (_lock)
                    {
                        logCache.AddOrUpdate(entry);
                    }
                }
            }
        }

        public void Dispose()
        {
            subscriptions?.Dispose();
            LogEntries?.TryDispose();
            logCache.TryDispose();
        }

        #region ILogViewerRealTimeService Implementation

        public IObservableCache<ILogEntry, long> LogEntries { get; }

        public void ClearLogEntries()
        {
            lock (_lock)
            {
                logCache.Clear();
            }
        }

        public void ConfigureLogEntriesCache(TimeSpan cacheExpiration)
        {
            
        }

        #endregion
    }
}

namespace Serilog
{
    public static class LogViewerRealTimeLoggerConfigurationExtensions
    {
        public static LoggerConfiguration LogViewerRealTimeEventSink(
            this LoggerSinkConfiguration loggerConfiguration)
        {
            return loggerConfiguration.Sink(LogViewerRealTimeService.Instance);
        }
    }
}
