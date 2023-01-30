using RvLinkDeviceTester.Resources;
using DynamicData;
using IDS.Portable.Common;
using ReactiveUI;
using Serilog;
using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace RvLinkDeviceTester.Services
{
    public class LogViewerFileService : Singleton<LogViewerFileService>, ILogViewerFileService, IDisposable
    {
        public IObservableCache<ILogEntry, long> LogEntries { get; }

        private readonly ISourceCache<ILogEntry, long> logCache = new SourceCache<ILogEntry, long>(p => p.LoggedOn.Ticks);
        private readonly IDisposable subscriptions;
        private readonly object _lock = new object();

        private LogViewerFileService()
        {
            var expirer = logCache
                .ExpireAfter(t => TimeSpan.FromMinutes(1), RxApp.TaskpoolScheduler)
                .Subscribe();

            LogEntries = logCache.AsObservableCache();

            subscriptions = new CompositeDisposable(LogEntries, expirer);

#if DEBUG
            LoadMockData();
#endif
        }

        public void Dispose()
        {
            logGenerator?.Dispose();
            subscriptions?.Dispose();
        }

        IDisposable? logGenerator = null;

        private void LoadMockData()
        {
            logCache.Clear();

            logGenerator = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(1), RxApp.TaskpoolScheduler)
                .Do(p =>
                {
                    var entry = new LogEntryItem
                    {
                        LoggedOn = DateTime.Now,
                        DeviceName = "TemperatureSensorBle",
                        Severity = p % 7 == 0 ? Strings.log_severity_error : p % 3 == 0 ? Strings.log_severity_warning : p % 2 == 0 ? Strings.log_severity_debug: Strings.log_severity_information
                    };

                    entry.Message = entry.Severity == "Information" ? $"{p} 0, Freezer Temperature" : $"{p}";

                    logCache.AddOrUpdate(entry);
                })
                .Subscribe();
        }

    }
}
