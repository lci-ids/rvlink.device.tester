using RvLinkDeviceTester.Resources;
using IDS.Portable.BLE.Platforms.Shared;
using IDS.Portable.BLE.Platforms.Shared.BleScanner;
using IDS.Portable.Common;
using Prism.Navigation;
using PrismExtensions.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.CommunityToolkit.ObjectModel;
using PrismExtensions.Enums;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;
using Prism.Commands;
using OneControl.Direct.IdsCanAccessoryBle.GenericSensor;
using RvLinkDeviceTester.Connections.Sensors;
using Fizzler;
using OneControl.Direct.MyRvLinkBle;
using RvLinkDeviceTester.Connections.Rv;
using System.Diagnostics;
using RvLinkDeviceTester.Services;

namespace RvLinkDeviceTester.UserInterface.AddAndManageDevices.PushToPair
{
    public enum PushToPairState
    {
        WaitingForDevice,
        Connecting,
        Connected,
        Error,
    }

    public class PushToPairPageViewModel : BaseViewModel, IViewModelResumePause
    {
        private readonly string LogTag = nameof(PushToPairPageViewModel);
        private readonly IAppSettings _appSettings;

        private static readonly TimeSpan WarningTimeSpan = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan ScanDelayInterval = TimeSpan.FromSeconds(4);
        private readonly int CheckForConnectionCompletedMs = 200;
        private readonly TimeSpan MaxMyRvLinkConnectionWaitTime = TimeSpan.FromSeconds(25);
        private const int MaximumScanAttempts = 3;
        private const int MaximumWaitForPushToPairAttempts = 3;
        private const int ConnectionRetryMs = 4000;

        #region UI Bindings
        public string Title => Strings.push_to_pair_title;
        public bool ShowWaitingForDevice => (State == PushToPairState.WaitingForDevice);
        public bool ShowConnecting => State == PushToPairState.Connecting;
        public bool ShowConnected => State == PushToPairState.Connected;
        public bool ShowError => State == PushToPairState.Error;

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

        private PushToPairState _state = PushToPairState.WaitingForDevice;
        public PushToPairState State
        {
            get => _state;
            set
            {
                SetProperty(ref _state, value);
                OnPropertyChanged(nameof(ShowWaitingForDevice));
                OnPropertyChanged(nameof(ShowConnecting));
                OnPropertyChanged(nameof(ShowConnected));
                OnPropertyChanged(nameof(ShowError));
            }
        }

        private ICommand? _retry;
        public ICommand Retry => _retry ??= new DelegateCommand(async () =>
        {
            await RetryPushToPairConnection();
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
        #endregion

        public PushToPairPageViewModel(INavigationService navigationService, IAppSettings appSettings) : base(navigationService)
        {
            _appSettings = appSettings;
        }


        #region Lifecycle
        public void OnPause(PauseReason reason)
        {
        }
        public async Task OnResumeAsync(ResumeReason reason, INavigationParameters? parameters, CancellationToken resumePauseCancellationToken)
        {
            await ConnectToPushToPairDevicesAsync();
        }
        #endregion

        #region Connection Logic
        private async Task ConnectToPushToPairDevicesAsync()
        {
            // Do the connection stuff
            //
            State = PushToPairState.WaitingForDevice;

            //Add Brief delay before searching
            await TaskExtension.TryDelay(ScanDelayInterval, ResumePauseCancellationToken);

            var (newDevicesInPairingMode, existingDeviceInPairMode) = await GetDevicesInLinkModeAsync(ResumePauseCancellationToken);

            if (newDevicesInPairingMode.Count == 0)
            {
                if (existingDeviceInPairMode != null)
                {
                    ErrorReason = "Push to Pair connection failed, no device found for pairing, already paired device(s) found";
                    TaggedLog.Error(LogTag, ErrorMessage);
                    State = PushToPairState.Error;
                    return;
                }
                else
                {
                    ErrorReason = "Push to Pair connection failed, no device found for pairing. Make sure you have your phones Bluetooth turned on.";
                    TaggedLog.Error(LogTag, ErrorMessage);
                    State = PushToPairState.Error;
                    return;
                }

            }

            if (newDevicesInPairingMode.Count > 1)
            {
                ErrorReason = "Push to Pair connection failed, multiple devices found in pairing mode";
                TaggedLog.Error(LogTag, ErrorMessage);
                State = PushToPairState.Error;
                return;
            }

            // If the device has pairing enabled, then try to connect/
            //
            var bleScanResult = newDevicesInPairingMode[0];
            var scannedDevice = await BleScannerService.Instance.TryGetDeviceAsync<IPairableDeviceScanResult>(bleScanResult.DeviceId, ResumePauseCancellationToken);
            if (scannedDevice?.PairingEnabled ?? false)
            {
                if (await ConnectToDevice(bleScanResult))
                {
                    TaggedLog.Information(LogTag, "Successfully connected to RvLink device");
                    State = PushToPairState.Connected;
                }
                else
                {
                    ErrorReason = "Push to Pair connection failed, device found, but failed to connect";
                    TaggedLog.Error(LogTag, ErrorMessage);
                    State = PushToPairState.Error;
                }
            }
            else
            {
                ErrorReason = "Push to Pair connection failed, device was not in pairing mode";
                TaggedLog.Error(LogTag, ErrorMessage);
                State = PushToPairState.Error;
                return;
            }
        }

        private async Task<bool> ConnectToDevice(IBleScanResult bleScanResult)
        {
            State = PushToPairState.Connecting;

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
                return false;
            }
            else
            {
                return true;
            }
        }

        private static async Task<(IList<IBleScanResult> newDevicesInPairMode, IBleScanResult? existingDeviceInPairMode)> GetDevicesInLinkModeAsync(CancellationToken cancellationToken)
        {
            var devices = new Dictionary<Guid, IBleScanResult>();

            IBleScanResult? foundExistingDeviceInPairMode = null;

            for (var scanAttempt = 0; scanAttempt < MaximumScanAttempts && !cancellationToken.IsCancellationRequested; scanAttempt++)
            {
                devices.Clear();
                await BleScannerService.Instance.TryGetDevicesAsync<IBleScanResult>(
                    (operation, scanResult) => devices[scanResult.DeviceId] = scanResult,
                    scanResult =>
                    {
                        if (scanResult == null)
                            return BleScannerCommandControl.Skip;

                        if (scanResult is not IMyRvLinkBleGatewayScanResult)
                            return BleScannerCommandControl.Skip;

                        // We have a new sensor so go ahead and include it!
                        //
                        return BleScannerCommandControl.IncludeAndFinish;
                    }, cancellationToken);

                // If we found a device we can return now, otherwise delay and try again.
                // 
                if (devices.Count > 0)
                    return (devices.Values.ToList(), foundExistingDeviceInPairMode);

                // Arbitrary delay so that the device is not constantly scanning. There is no need to be super responsive to
                // a device entering pairing mode.
                // 
                await TaskExtension.TryDelay(ScanDelayInterval, cancellationToken);
            }

            return (devices.Values.ToList(), foundExistingDeviceInPairMode);
        }

        #endregion

        public async Task RetryPushToPairConnection()
        {
            await ConnectToPushToPairDevicesAsync();
        }


        public bool ConfirmNavigation(INavigationParameters? parameters) => CanGoBack;
    }
}
