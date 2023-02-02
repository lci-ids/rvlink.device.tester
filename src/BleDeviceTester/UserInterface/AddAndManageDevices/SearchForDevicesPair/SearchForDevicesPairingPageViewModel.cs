using RvLinkDeviceTester.Connections.Sensors;
using RvLinkDeviceTester.Resources;
using IDS.Core.IDS_CAN;
using IDS.Portable.BLE.Platforms.Shared.BleScanner;
using IDS.Portable.Common;
using OneControl.Direct.IdsCanAccessoryBle.GenericSensor;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;
using Prism.Commands;
using Prism.Navigation;
using PrismExtensions.Enums;
using PrismExtensions.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.CommunityToolkit.ObjectModel;
using OneControl.Direct.MyRvLinkBle;
using System.Linq;
using RvLinkDeviceTester.Connections.Rv;
using RvLinkDeviceTester.Services;
using RvLinkDeviceTester.UserInterface.AddAndManageDevices.PushToPair;
using System.Diagnostics;
using IDS.Portable.BLE.Platforms.Shared;

namespace RvLinkDeviceTester.UserInterface.AddAndManageDevices.SearchForDevicesPair
{
    public enum PairState
    {
        WaitingForDeviceSelection,
        Connecting,
        Connected,
        Error,
    }

    public class SearchForDevicesPairingPageViewModel : BaseViewModel, IViewModelResumePause
    {
        private readonly string LogTag = nameof(SearchForDevicesPairingPageViewModel);
        private readonly IAppSettings _appSettings;

        public string Title => "Device Search";

        private readonly int CheckForConnectionCompletedMs = 200;
        private readonly TimeSpan MaxMyRvLinkConnectionWaitTime = TimeSpan.FromSeconds(25);
        protected static readonly TimeSpan MaxScanResultAge = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan BleScanInterval = TimeSpan.FromSeconds(5);


        public bool ShowWaitingForDeviceSelection => (State == PairState.WaitingForDeviceSelection);
        public bool ShowConnecting => State == PairState.Connecting;
        public bool ShowConnected => State == PairState.Connected;
        public bool ShowError => State == PairState.Error;

        private bool _showSearching = true;
        public bool ShowSearching
        {
            get => _showSearching;
            set => SetProperty(ref _showSearching, value);
        }

        private string _errorReason = "";
        public string ErrorReason
        {
            get => _errorReason;
            set
            {
                SetProperty(ref _errorReason, value);
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }

        public string ErrorMessage => $"The BLE Device Tester was unable to pair to your device. Reason: {ErrorReason}";

        private PairState _state = PairState.WaitingForDeviceSelection;
        public PairState State
        {
            get => _state;
            set
            {
                SetProperty(ref _state, value);
                OnPropertyChanged(nameof(ShowWaitingForDeviceSelection));
                OnPropertyChanged(nameof(ShowConnecting));
                OnPropertyChanged(nameof(ShowConnected));
                OnPropertyChanged(nameof(ShowError));
            }
        }

        private bool _scanning;
        public bool Scanning
        {
            get => _scanning;
            set
            {
                if (SetProperty(ref _scanning, value, nameof(Scanning)) && !value)
                {
                    ScanCompleted();
                }
            }
        }

        public IBleScanResult ScanResult { get; protected set; }
        public BaseObservableCollection<ScanResultViewModel> ScanResults { get; } = new ComparingObservableCollection<ScanResultViewModel>((item1, item2) => string.Compare(item1.Name, item2.Name, StringComparison.Ordinal));


        protected virtual void ScanCompleted()
        {
        }

        private ICommand? _retry;
        public ICommand Retry => _retry ??= new DelegateCommand(async () =>
        {
            State = PairState.WaitingForDeviceSelection;
        });

        private ICommand? _returnHome;
        public ICommand ReturnHome => _returnHome ??= new DelegateCommand(async () =>
        {
            await NavigationService.GoBackToRootAsync();
        });

        private bool _canGoBack = true;
        public bool CanGoBack
        {
            get => _canGoBack;
            set => SetProperty(ref _canGoBack, value);
        }

        private ICommand? _goBack;
        public ICommand GoBack => _goBack ??= new AsyncCommand(async () => await NavigationService.GoBackAsync());

        public SearchForDevicesPairingPageViewModel(INavigationService navigationService, IAppSettings appSettings) : base(navigationService)
        {
            _appSettings = appSettings;
        }

        public ICommand SelectedCommand => new AsyncCommand(async () =>
        {
            //Pair to device selected
            await ConnectToDevice(ScanResult);
        });

        private async Task ConnectToDevice(IBleScanResult bleScanResult)
        {
            try
            {
                State = PairState.Connecting;

                var scannedDevice = await BleScannerService.Instance.TryGetDeviceAsync<IPairableDeviceScanResult>(bleScanResult.DeviceId, ResumePauseCancellationToken);
                if (scannedDevice?.PairingEnabled ?? false)
                {
                    State = PairState.Error;
                    ErrorReason = "Device is push to pair. Use the 'Push to Pair' pairing method.";
                    return;
                }

                var connection = new RvGatewayMyRvLinkConnectionBle(bleScanResult.DeviceId, bleScanResult.DeviceName);
                _appSettings.SetSelectedRvGatewayConnection(connection, false);
                var waitForConnectionTimer2 = Stopwatch.StartNew();
                var timeToWait2 = MaxMyRvLinkConnectionWaitTime.TotalMilliseconds;

                while (waitForConnectionTimer2.ElapsedMilliseconds < timeToWait2 &&
                   AppDirectConnectionService.Instance.RvConnectionStatus !=
                   ConnectionManagerStatus.Connected &&
                   !ResumePauseCancellationToken.IsCancellationRequested)
                {
                    await TaskExtension.TryDelay(CheckForConnectionCompletedMs, ResumePauseCancellationToken);
                }

                // Check the connection status now that we've completed or timed out.
                if (AppDirectConnectionService.Instance.RvConnectionStatus != ConnectionManagerStatus.Connected)
                {
                    connection = null;
                    _appSettings.SetSelectedRvGatewayConnection(AppSettings.DefaultRvDirectConnectionNone,
                        false);
                    State = PairState.Error;
                    ErrorReason = "Timed out trying to connect to device";
                    return;
                }
                else
                {
                    State = PairState.Connected;
                    return;
                }
            }
            catch (Exception ex) 
            {
                State = PairState.Error;
                ErrorReason = $"Unexpected error. If device is push to pair, use that pairing method instead. Error message: {ex.Message}";
            }

        }

        #region Lifecycle
        public void OnStartAsync()
        {
            // Stop the current RV connection in the event the user chooses the same connection.  If they choose the
            // same connection as the current, we don't wont both running at the same time.  We don't SAVE this change
            // so the use can swipe kill the app to get their old connection back.
            // 
            AppSettings.Instance.SetSelectedRvGatewayConnection(new RvGatewayCanConnectionNone(), false);
        }

        public void OnPause(PauseReason reason)
        {
        }
        public async Task OnResumeAsync(ResumeReason reason, INavigationParameters? parameters, CancellationToken resumePauseCancellationToken)
        {
            ScanResults.Clear();

            while (!resumePauseCancellationToken.IsCancellationRequested)
            {
                await ScanBleDevices(resumePauseCancellationToken);
                await TaskExtension.TryDelay(BleScanInterval, resumePauseCancellationToken);
            }
        }
        #endregion


        #region Ble
        protected async Task ScanBleDevices(CancellationToken cancellationToken)
        {
            Scanning = true;

            PurgeOldScanResults<BleScanResultViewModel>();

            await BleScannerService.Instance.TryGetDevicesAsync<IBleScanResult>((operation, scanResult) =>
            {
                // For the RV selection page, we don't want to display accessories such as the temp sensor
                if (scanResult is IdsCanAccessoryScanResult { HasAccessoryStatus: true })
                {
                    return;
                }
                AddOrUpdateScanResult(new BleScanResultViewModel(scanResult));
            }, result =>
            {
                // As this is the RV selection page, filter out anything but RV gateways
                if (result is not IMyRvLinkBleGatewayScanResult && result is not IBleGatewayScanResult)
                    return BleScannerCommandControl.Skip;

                // We don't want to show non-rv style gateways such as ABS gateways
                if (result is IMyRvLinkBleGatewayScanResult rvLinkScanResult && rvLinkScanResult.GatewayType != RvLinkGatewayType.Gateway)
                    return BleScannerCommandControl.Skip;

                if (string.IsNullOrEmpty(result.DeviceName))
                    return BleScannerCommandControl.Skip;

                if (result.HasRequiredAdvertisements != BleRequiredAdvertisements.AllExist && result.HasRequiredAdvertisements != BleRequiredAdvertisements.Unknown)
                    return BleScannerCommandControl.Skip;

                return BleScannerCommandControl.Include;
            }, cancellationToken);

            Scanning = false;
        }
        #endregion

        protected void PurgeOldScanResults<TScanResult>() where TScanResult : ScanResultViewModel
        {
            Xamarin.Essentials.MainThread.BeginInvokeOnMainThread(() =>
            {
                var now = DateTime.Now;
                var oldScanResults = ScanResults
                    .Select(sr => sr)
                    .Where(sr => sr is TScanResult tsr && now - tsr.LastUpdated > MaxScanResultAge).ToList();

                foreach (var oldScanResult in oldScanResults)
                    ScanResults.Remove(oldScanResult);
            });
        }

        protected virtual void AddOrUpdateScanResult<TScanResult>(TScanResult? scanResult) where TScanResult : ScanResultViewModel
        {
            if (scanResult is null) return;

            Xamarin.Essentials.MainThread.BeginInvokeOnMainThread(() =>
            {
                if (ScanResults.FirstOrDefault(esr => esr.Uuid == scanResult.Uuid) is TScanResult existingScanResult)
                    existingScanResult.Update(scanResult);
                else
                {
                    var now = DateTime.Now;
                    if (now - scanResult.LastUpdated < MaxScanResultAge)
                    {
                        ScanResults.Add(scanResult);
                        ShowSearching = false;
                    }
                        
                }
            });
        }

        public bool ConfirmNavigation(INavigationParameters? parameters) => CanGoBack;
    }
}
