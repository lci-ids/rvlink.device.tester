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
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Essentials;

namespace RvLinkDeviceTester.UserInterface.AddAndManageDevices.MacAddressPair
{
    public enum PairState
    {
        WaitingForMacAddress,
        Connecting,
        Connected,
        Error,
    }

    public class MacAddressPairingPageViewModel : BaseViewModel, IViewModelResumePause
    {
        private readonly string LogTag = nameof(MacAddressPairingPageViewModel);

        public string Title => Strings.mac_address_pair_title;

        public List<string> MacHistory => AppSettings.Instance.MacHistory;

        private MAC? MacAddress = null;
        private string _macAddressEntered = string.Empty;
        public string MacAddressEntered
        {
            get => _macAddressEntered;
            set => Xamarin.Essentials.MainThread.BeginInvokeOnMainThread(() => SetProperty(ref _macAddressEntered, value));
        }

        public bool ShowWaitingForMacAddress => (State == PairState.WaitingForMacAddress);
        public bool ShowConnecting => State == PairState.Connecting;
        public bool ShowConnected => State == PairState.Connected;
        public bool ShowError => State == PairState.Error;

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

        private PairState _state = PairState.WaitingForMacAddress;
        public PairState State
        {
            get => _state;
            set
            {
                SetProperty(ref _state, value);
                OnPropertyChanged(nameof(ShowWaitingForMacAddress));
                OnPropertyChanged(nameof(ShowConnecting));
                OnPropertyChanged(nameof(ShowConnected));
                OnPropertyChanged(nameof(ShowError));
            }
        }

        private ICommand? _retry;
        public ICommand Retry => _retry ??= new DelegateCommand(async () =>
        {
            State = PairState.WaitingForMacAddress;
        });

        private ICommand? _returnHome;
        public ICommand ReturnHome => _returnHome ??= new DelegateCommand(async () =>
        {
            await NavigationService.GoBackToRootAsync();
        });

        private ICommand? _pair;
        public ICommand Pair => _pair ??= new DelegateCommand(async () =>
        {
            await PairToMacAddress(ResumePauseCancellationToken);
        });

        private bool _canGoBack = true;
        public bool CanGoBack
        {
            get => _canGoBack;
            set => SetProperty(ref _canGoBack, value);
        }

        private ICommand? _name;
        public ICommand GoBack => _name ??= new AsyncCommand(async () => await NavigationService.GoBackAsync());

        public MacAddressPairingPageViewModel(INavigationService navigationService) : base(navigationService)
        {
        }

        #region Lifecycle
        public void OnPause(PauseReason reason)
        {
        }
        public async Task OnResumeAsync(ResumeReason reason, INavigationParameters? parameters, CancellationToken resumePauseCancellationToken)
        {
        }
        #endregion

        public async Task<bool> PairToMacAddress(CancellationToken cancellationToken)
        {
            if (!await CheckEnteredMacAddress(MacAddressEntered))
                return false;

            AppSettings.Instance.AddToMacHistory(MacAddress.ToString());

            State = PairState.Connecting;

            // We can't tell if we have the right device until we decode it with the mac address from the user, so we just
            // take the first IdsCanAccessoryScan result with all the required advertisements and finish.
            IdsCanAccessoryScanResult? idsAccessoryScanResult = null;
            await BleScannerService.Instance.TryGetDevicesAsync<IdsCanAccessoryScanResult>(
                (_, scanResult) => idsAccessoryScanResult = scanResult,
                scanResult =>
                {
                    switch (scanResult.HasRequiredAdvertisements)
                    {
                        case BleRequiredAdvertisements.AllExist:
                            // Try to parse the status to see if we found the right device
                            var accessoryStatus = scanResult.GetAccessoryStatus(MacAddress);
                            if (accessoryStatus is null)
                            {
                                TaggedLog.Information(LogTag, $"Skipping device with name `{scanResult.DeviceName}` as missing accessory status");
                                return BleScannerCommandControl.Skip;
                            }

                            if (scanResult.AccessoryMacAddress != MacAddress)
                            {
                                TaggedLog.Information(LogTag, $"Skipping device with name `{scanResult.DeviceName}` as mac address doesn't match");
                                return BleScannerCommandControl.Skip;
                            }

                            return BleScannerCommandControl.IncludeAndFinish;

                        case BleRequiredAdvertisements.Unknown:
                        case BleRequiredAdvertisements.SomeExist:
                        case BleRequiredAdvertisements.NoneExist:
                        default:
                            return BleScannerCommandControl.Skip;
                    }
                }, ResumePauseCancellationToken);

            // Report a problem if we weren't able to find the device.
            //
            if (idsAccessoryScanResult == null)
            {
                ErrorReason = "Unable to find a device with a matching mac address. Ensure your phones bluetooth is turned on.";
                TaggedLog.Error(LogTag, ErrorMessage);
                State = PairState.Error;
                return false;
            }

            TaggedLog.Information(LogTag, $"Found Device `{idsAccessoryScanResult.DeviceName}`");

            //REGISTER GENERIC DEVICE
            var genericSensorBle = Resolver<IDirectGenericSensorBle>.Resolve;
            if (genericSensorBle is null)
            {
                ErrorReason = "Mac pairing connection failed, unable to resolve IDirectGenericSensorBle";
                TaggedLog.Error(LogTag, ErrorMessage);
                State = PairState.Error;
                return false;
            }

            genericSensorBle.RegisterGenericSensor(idsAccessoryScanResult.DeviceId, MacAddress, idsAccessoryScanResult.SoftwarePartNumber ?? string.Empty, idsAccessoryScanResult.DeviceName);
            AppSettings.Instance.TakeSnapshot();

            AppSettings.Instance.TryAddSensorConnection(new SensorConnectionGeneric(idsAccessoryScanResult.DeviceName, idsAccessoryScanResult.DeviceId,
                     MacAddress, idsAccessoryScanResult.SoftwarePartNumber));

            State = PairState.Connected;
            return true;
        }

        private async Task<bool> CheckEnteredMacAddress(string macAddress)
        {
            macAddress = macAddress.Replace(":", string.Empty).Replace(" ", string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(MacAddressEntered))
            {
                ErrorReason = "No mac address was entered. Enter a mac address before pressing pair.";
                TaggedLog.Error(LogTag, ErrorMessage);
                State = PairState.Error;
                return false;
            }
            else if (MacAddressEntered.Length < 12)
            {
                ErrorReason = "Mac address entered is too small. Enter a valid mac address before pressing pair.";
                TaggedLog.Error(LogTag, ErrorMessage);
                State = PairState.Error;
                return false;
            }
            else if (MacAddressEntered.Length > 12)
            {
                ErrorReason = "Mac address entered is too large. Enter a valid mac address before pressing pair.";
                TaggedLog.Error(LogTag, ErrorMessage);
                State = PairState.Error;
                return false;
            }

            try
            {
                var physicalAddress = PhysicalAddress.Parse(macAddress.ToUpper());
                TaggedLog.Information(LogTag, $"Looking for device with MAC Address {physicalAddress}");
                MacAddress = new MAC(physicalAddress.GetAddressBytes());
            }
            catch (Exception ex)
            {
                ErrorReason = $"Mac address entered failed. Ensure your input only contains 12 hexadecimal numbers. Exception: {ex.Message}";
                TaggedLog.Error(LogTag, ErrorMessage);
                State = PairState.Error;
                return false;
            }

            if (MacAddress is null)
            {
                ErrorReason = $"Mac address entered failed. Failed to convert string into a valid Mac Address.";
                TaggedLog.Error(LogTag, ErrorMessage);
                State = PairState.Error;
                return false;
            }

            return true;
        }

        public bool ConfirmNavigation(INavigationParameters? parameters) => CanGoBack;
    }
}
