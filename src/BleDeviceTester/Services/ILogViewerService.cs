using DynamicData;
using System;
using System.Collections.Generic;
using System.Text;

namespace RvLinkDeviceTester.Services
{
    public interface ILogViewerService
    {
        /// <summary>
        /// Observable Collection of Log Entries. Use the "Connect" method to subscribe to changes.
        /// </summary>
        IObservableCache<ILogEntry, long> LogEntries { get; }
    }

    public interface ILogViewerFileService : ILogViewerService
    {

    }

    public interface ILogViewerRealTimeService: ILogViewerService
    {
        void ClearLogEntries();

        void ConfigureLogEntriesCache(TimeSpan cacheExpiration);

    }
}
