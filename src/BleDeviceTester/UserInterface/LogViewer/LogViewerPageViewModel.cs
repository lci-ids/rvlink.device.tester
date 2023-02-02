using RvLinkDeviceTester.Resources;
using RvLinkDeviceTester.Services;
using DynamicData;
using DynamicData.Binding;
using Prism.Navigation;
using PrismExtensions.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;
using Xamarin.CommunityToolkit.ObjectModel;

namespace RvLinkDeviceTester.UserInterface.LogViewer
{
    public class LogViewerPageViewModel : BaseViewModel, IViewModelConfirmNavigation, IDisposable
    {
        public string Title => Strings.log_viewer_title;

        #region Navigation

        private bool _canGoBack = true;
        public bool CanGoBack
        {
            get => _canGoBack;
            set => SetProperty(ref _canGoBack, value);
        }

        private ICommand? _name;
        public ICommand GoBack => _name ??= new AsyncCommand(async () => await NavigationService.GoBackAsync());

        public bool ConfirmNavigation(INavigationParameters? parameters) => CanGoBack;

        #endregion

        private IDisposable? logEntrySubscription;
        private ReadOnlyObservableCollection<ILogEntry> logEntries = new ReadOnlyObservableCollection<ILogEntry>(new ObservableCollection<ILogEntry>());

        private readonly ILogViewerFileService? logViewerFileService;
        private readonly ILogViewerRealTimeService? logViewerRealTimeService;

        private readonly IObservable<Func<ILogEntry, bool>> severityFilter;
        private readonly IObservable<Func<ILogEntry, bool>> textFilter;

        public LogViewerPageViewModel(INavigationService navigationService, 
                                      ILogViewerFileService fileLogViewer, 
                                      ILogViewerRealTimeService realTimeLogViewer) : base(navigationService)
        {
            logViewerFileService = fileLogViewer;
            logViewerRealTimeService = realTimeLogViewer;

            severityFilter = this.WhenAnyValue(p => p.SelectedSeverity)
                    .Select(CreateSeverityFilter);

            textFilter = this.WhenAnyValue(p => p.FilterText)
                .Throttle(TimeSpan.FromMilliseconds(250))
                .Select(CreateTextFilter);
        }

        #region Filters

        private static Func<ILogEntry, bool> CreateSeverityFilter(SeverityLevel? severityLevel)
        {
            if (string.IsNullOrEmpty(severityLevel?.Severity)) return logEntryItem => true;

            return logEntryItem => logEntryItem.Severity?.Equals(severityLevel?.Severity, StringComparison.OrdinalIgnoreCase) ?? true;
        }

        private static Func<ILogEntry, bool> CreateTextFilter(string? text)
        {
            if (string.IsNullOrEmpty(text)) return logEntryItem => true;

            return logEntryItem => logEntryItem.Message?.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   logEntryItem.DeviceName?.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   logEntryItem.LoggedOn.ToString("HH:mm:ss fff")?.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string? filterText = string.Empty;
        public string? FilterText { get => filterText; set => SetProperty(ref filterText, value); }

        #endregion

        #region UI Bindings

        private SeverityLevel? selectedSeverity;
        public SeverityLevel? SelectedSeverity { get => selectedSeverity; set => SetProperty(ref selectedSeverity, value); }

        private bool realTimeLoggingEnabled;
        public bool RealTimeLoggingEnabled
        {
            get => realTimeLoggingEnabled;
            set
            {
                if (SetProperty(ref realTimeLoggingEnabled, value))
                {
                    ToggleRealTimeLoggingMode(value);
                }
            }
        }

        public ReadOnlyObservableCollection<ILogEntry> LogEntries => logEntries;

        public List<SeverityLevel> SeverityLevels => new List<SeverityLevel> { new SeverityLevel { Severity = Strings.log_severity_information },
                                                                               new SeverityLevel { Severity = Strings.log_severity_warning },
                                                                               new SeverityLevel { Severity = Strings.log_severity_error},
                                                                               new SeverityLevel { Severity = Strings.log_severity_debug }
                                                                             };

        #endregion

        private void ToggleRealTimeLoggingMode(bool realTimeModeEnabled)
        {
            if (realTimeModeEnabled)
                SwitchToRealTimeLogViewing();
            else
                SwitchToFileLogViewing();
        }

        private void SwitchToRealTimeLogViewing()
        {
            if (logViewerRealTimeService is not null)
            {
                logEntrySubscription?.Dispose();
                logEntrySubscription = null;

                logEntrySubscription = logViewerRealTimeService.LogEntries.Connect()
                   .Filter(severityFilter)
                   .Filter(textFilter)
                   .Sort(SortExpressionComparer<ILogEntry>.Descending(p => p.LoggedOn))
                   .ObserveOn(RxApp.MainThreadScheduler)
                   .Bind(out logEntries)
                   .DisposeMany()
                   .Subscribe();

                OnPropertyChanged(nameof(LogEntries));
            }
        }

        private void SwitchToFileLogViewing()
        {
            if (logViewerFileService is not null)
            {
                logEntrySubscription?.Dispose();
                logEntrySubscription = null;

                logEntrySubscription = logViewerFileService.LogEntries.Connect()
                   .Filter(severityFilter)
                   .Filter(textFilter)
                   .Sort(SortExpressionComparer<ILogEntry>.Descending(p => p.LoggedOn))
                   .ObserveOn(RxApp.MainThreadScheduler)
                   .Bind(out logEntries)
                   .DisposeMany()
                   .Subscribe();

                OnPropertyChanged(nameof(LogEntries));
            }
        }

        #region IDisposable
        
        public void Dispose()
        {
            logEntrySubscription?.Dispose();
        }

        #endregion
    }
}
