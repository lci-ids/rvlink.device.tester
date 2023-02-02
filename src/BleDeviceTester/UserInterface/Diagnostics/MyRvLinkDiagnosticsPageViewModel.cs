using RvLinkDeviceTester.Resources;
using RvLinkDeviceTester.Services;
using IDS.Portable.BLE.Platforms.Shared.BleScanner;
using IDS.Portable.Common;
using IDS.UI.Interfaces;
using Prism.Commands;
using Prism.Navigation;
using PrismExtensions.Enums;
using PrismExtensions.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.CommunityToolkit.ObjectModel;
using IDS.Portable.Common.Extensions;
using OneControl.Direct.MyRvLink;
using OneControl.UserInterface.Common;
using IDS.Portable.LogicalDevice;
using System.Collections.Generic;
using RvLinkDeviceTester.UserInterface.Main;

namespace RvLinkDeviceTester.UserInterface.Diagnostics
{
    public class MyRvLinkDiagnosticsPageViewModel : BaseViewModel, IViewModelConfirmNavigation, IViewModelResumePause
    {
        public string Title => "RvLink Diagnostics";
        private const string LogTag = nameof(MyRvLinkDiagnosticsPageViewModel);
        private const int PollUpdateTimeMs = 2000;
        private CallbackTimer? _updatePidTimer;

        private readonly ComparingObservableCollection<ICellViewModel> _events = new ComparingObservableCollection<ICellViewModel>(DeviceCellViewModelSorter, orderAscending: true);
        public ComparingObservableCollection<ICellViewModel> Events { get => _events; }

        #region Navigation
        private bool _canGoBack = true;
        public bool CanGoBack
        {
            get => _canGoBack;
            set => SetProperty(ref _canGoBack, value);
        }

        private ICommand? _goBack;
        public ICommand GoBack => _goBack ??= new AsyncCommand(NavigationService.GoBackAsync);

        private bool _navigating;
        public bool Navigating
        {
            get => _navigating;
            set => SetProperty(ref _navigating, value);
        }

        public bool ConfirmNavigation(INavigationParameters? parameters) => CanGoBack;
        #endregion

        public MyRvLinkDiagnosticsPageViewModel(INavigationService navigationService) : base(navigationService)
        {

            // Make Cell for each possible event
            //
            foreach (var eventType in EnumExtensions.GetValues<MyRvLinkEventType>())
            {
                if (eventType == MyRvLinkEventType.Unknown || eventType == MyRvLinkEventType.Invalid)
                    continue;

                var cell = new CellViewModel<MyRvLinkEventType>(eventType, text: eventType.ToString(), description: String.Empty);
                //cell.ToggleAction = async () => {
                //    var logicalDeviceService = Resolver<ILogicalDeviceService>.Resolve;
                //    var directManager = logicalDeviceService.DeviceSourceManager.FindFirstDeviceSource<IDirectConnectionMyRvLink>((dm) => true);
                //    if (directManager == null)
                //        return;

                //    try
                //    {
                //        var commandId = directManager.GetNextCommandId();
                //        var enable = !cell.IsToggled;
                //        var diagCommand = new MyRvLinkCommandDiagnostics(
                //            commandId,
                //            MyRvLinkCommandType.Unknown, DiagnosticState.Disable,
                //            eventType, enable ? DiagnosticState.Enable : DiagnosticState.Disable);
                //        await directManager.SendCommandAsync(diagCommand, cts.Token, MyRvLinkSendCommandOption.None);
                //        UpdateDiagCommandState(diagCommand.EnabledDiagnosticCommands);
                //        UpdateDiagEventState(diagCommand.EnabledDiagnosticEvents);
                //    }
                //    catch (Exception ex)
                //    {
                //        TaggedLog.Debug(LogTag, $"Unable to change diagnostic setting for {eventType}: {ex.Message}");
                //    }
                //};

                _events.Add(cell);
            }

        }

        private void UpdateDiagEventState(IReadOnlyList<MyRvLinkEventType>? enabledItems)
        {
            var logicalDeviceService = Resolver<ILogicalDeviceService>.Resolve;

            foreach (var item in _events)
            {
                if (!(item is CellViewModel<MyRvLinkEventType> cellItem))
                    continue;

                var directManager = logicalDeviceService.DeviceSourceManager.FindFirstDeviceSource<IDirectConnectionMyRvLink>((dm) => true);
                if (directManager is IDirectConnectionMyRvLinkMetrics directManagerMetrics)
                {
                    var metrics = directManagerMetrics.GetFrequencyMetricForEvent(cellItem.CustomData);
                    //var enabledText = (enabledItems is not null) ? "Enabled" : "Disabled";
                    cellItem.Description = $"Count: {metrics.Count}   Rate: {metrics.UpdatesPerSecond:F2}/sec   Avg: {metrics.AverageTimeMs:F2}ms";
                }
            }
        }

        private void UpdateDiagState()
        {
            //UpdateDiagCommandState(null);
            UpdateDiagEventState(null);
        }

        #region Lifecycle
        public Task OnResumeAsync(ResumeReason reason, INavigationParameters? parameters, CancellationToken resumePauseCancellationToken)
        {
            base.OnResume();

            _updatePidTimer?.TryCancelAndDispose();
            _updatePidTimer = new CallbackTimer(UpdateDiagState, PollUpdateTimeMs, repeat: true);

            Task.Run(async () =>
            {
                var logicalDeviceService = Resolver<ILogicalDeviceService>.Resolve;
                var directManager = logicalDeviceService.DeviceSourceManager.FindFirstDeviceSource<IDirectConnectionMyRvLink>((dm) => true);
                if (directManager == null)
                    return;

                try
                {
                    var commandId = directManager.GetNextCommandId();
                    var diagCommand = new MyRvLinkCommandDiagnostics(
                        commandId,
                        MyRvLinkCommandType.Unknown, DiagnosticState.Disable,
                        MyRvLinkEventType.Unknown, DiagnosticState.Disable);
                    await directManager.SendCommandAsync(diagCommand, ResumePauseCancellationToken, MyRvLinkSendCommandOption.None);
                    //UpdateDiagCommandState(diagCommand.EnabledDiagnosticCommands);
                    UpdateDiagEventState(diagCommand.EnabledDiagnosticEvents);
                }
                catch (Exception ex)
                {
                    TaggedLog.Debug(LogTag, $"Unable to update diagnostic state: {ex.Message}");
                }
            });



            return Task.CompletedTask;
        }

        public void OnPause(PauseReason reason)
        {
            _updatePidTimer?.TryCancelAndDispose();
            _updatePidTimer = null;
        }
        #endregion

        public static int DeviceCellViewModelSorter(ICellViewModel cellA, ICellViewModel cellB)
        {
            return string.Compare(cellA.Text, cellB.Text, StringComparison.CurrentCulture);
        }
    }
}
