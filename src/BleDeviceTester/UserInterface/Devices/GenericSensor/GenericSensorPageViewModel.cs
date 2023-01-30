using RvLinkDeviceTester.Resources;
using RvLinkDeviceTester.UserInterface.Devices.GenericSensor;
using IDS.Portable.BLE.Platforms.Shared;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Direct.IdsCanAccessoryBle;
using OneControl.Direct.IdsCanAccessoryBle.GenericSensor;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults.Statistics;
using Prism.Commands;
using Prism.Navigation;
using PrismExtensions.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Linq;
using System.Windows.Input;
using Xamarin.CommunityToolkit.ObjectModel;
using RvLinkDeviceTester.Connections.Sensors;
using System.Collections.Generic;
using IDS.UI.UserDialogs;
using System.Diagnostics;
using OneControl.Devices;

namespace RvLinkDeviceTester.UserInterface.Devices
{
    public class GenericSensorPageViewModel : BaseViewModel, IViewModelConfirmNavigation, INavigatedAware
    {
        private const string LogTag = nameof(GenericSensorPageViewModel);
        private const int GetPartNumberTimeout = 65 * 1000;

        private string _title = "";
        public string Title
        {
            get => _title;
            set
            {
                SetProperty(ref _title, value);
                OnPropertyChanged(nameof(Title));
            }
        }

        private bool _isDeviceIdVisible;
        public bool IsDeviceIdVisible
        {
            get => _isDeviceIdVisible;
            set => SetProperty(ref _isDeviceIdVisible, value);
        }

        public string HistoryDataError = string.Empty;
        //History Data Error

        //Used to hide the cell before History Data is clicked
        private bool _wasHistoryDataClicked;
        public bool WasHistoryDataClicked
        {
            get => _wasHistoryDataClicked;
            set => SetProperty(ref _wasHistoryDataClicked, value);
        }

        //Used to expand and collapse the cell
        private bool _isHistoryDataVisible;
        public bool IsHistoryDataVisible
        {
            get => _isHistoryDataVisible;
            set => SetProperty(ref _isHistoryDataVisible, value);
        }
        private ICommand? _historyDataDropdownCommand;
        public ICommand HistoryDataDropdownCommand => _historyDataDropdownCommand ??= new DelegateCommand(() => IsHistoryDataVisible = !IsHistoryDataVisible);

        private ICommand? _deviceIdDropdownCommand;
        public ICommand DeviceIdDropdownCommand => _deviceIdDropdownCommand ??= new DelegateCommand(() => IsDeviceIdVisible = !IsDeviceIdVisible);

        private bool _isDeviceStatusVisible;
        public bool IsDeviceStatusVisible
        {
            get => _isDeviceStatusVisible;
            set => SetProperty(ref _isDeviceStatusVisible, value);
        }
        private ICommand? _deviceStatusDropdownCommand;
        public ICommand DeviceStatusDropdownCommand => _deviceStatusDropdownCommand ??= new DelegateCommand(() => IsDeviceStatusVisible = !IsDeviceStatusVisible);

        private bool _isExtendedDeviceStatusVisible;
        public bool IsExtendedDeviceStatusVisible
        {
            get => _isExtendedDeviceStatusVisible;
            set => SetProperty(ref _isExtendedDeviceStatusVisible, value);
        }
        private ICommand? _versionInfoDropdownCommand;
        public ICommand VersionInfoDropdownCommand => _versionInfoDropdownCommand ??= new DelegateCommand(() => IsVersionInfoVisible = !IsVersionInfoVisible);

        private bool _isVersionInfoVisible;
        public bool IsVersionInfoVisible
        {
            get => _isVersionInfoVisible;
            set => SetProperty(ref _isVersionInfoVisible, value);
        }
        private ICommand? _extendedDeviceStatusDropdownCommand;
        public ICommand ExtendedDeviceStatusDropdownCommand => _extendedDeviceStatusDropdownCommand ??= new DelegateCommand(() => IsExtendedDeviceStatusVisible = !IsExtendedDeviceStatusVisible);

        private bool _isPid1Visible;
        public bool IsPid1Visible
        {
            get => _isPid1Visible;
            set => SetProperty(ref _isPid1Visible, value);
        }
        private ICommand? _pid1DropdownCommand;
        public ICommand Pid1DropdownCommand => _pid1DropdownCommand ??= new DelegateCommand(() => IsPid1Visible = !IsPid1Visible);

        private string _connectDisconnect = Strings.connect_title;
        public string ConnectDisconnect
        {
            get => _connectDisconnect;
            set
            {
                SetProperty(ref _connectDisconnect, value);
                OnPropertyChanged(nameof(ConnectDisconnect));
            }
        }

        public string KeySeed => Strings.key_seed_title;

        public ILogicalDevice Sensor { get; private set; }

        public GenericSensorBle? SensorBle { get; private set; }

        #region Navigation
        private bool _canGoBack = true;
        public bool CanGoBack
        {
            get => _canGoBack;
            set => SetProperty(ref _canGoBack, value);
        }

        private ICommand? _goBack;
        public ICommand GoBack => _goBack ??= new AsyncCommand(async () => await NavigationService.GoBackAsync());

        private bool _navigating;
        public bool Navigating
        {
            get => _navigating;
            set => SetProperty(ref _navigating, value);
        }

        public bool ConfirmNavigation(INavigationParameters? parameters) => CanGoBack;
        #endregion

        private readonly ILogicalDeviceService _logicalDeviceService;
        private readonly IUserDialogService _userDialogService;

        public GenericSensorPageViewModel(INavigationService navigationService, ILogicalDeviceService logicalDeviceService,
            IUserDialogService userDialogService)
            : base(navigationService)
        {
            _logicalDeviceService = logicalDeviceService;
            _userDialogService = userDialogService;

            RaiseAllPropertiesChanged();
        }

        public void OnNavigatedTo(INavigationParameters parameters)
        {
            Sensor = parameters.GetValue<ILogicalDevice>("sensor");
            Title = Sensor.DeviceName;

            var genericSensorDeviceSource = Resolver<IDirectGenericSensorBle>.Resolve;
            SensorBle = genericSensorDeviceSource?.GetGenericSensor(Sensor);

            GetStatusData();
            GetExtendedStatusData();
            GetVersionInfo();

            if (SensorBle?.AccessoryConnectionManager?.IsSharedConnectionActive ?? false)
                ConnectDisconnect = Strings.disconnect_title;
            else
                ConnectDisconnect = Strings.connect_title;
        }

        public ObservableCollection<ListData> DeviceIdData { get; } = new();

        public ObservableCollection<ListData> StatusData { get; } = new();

        public ObservableCollection<ListData> ExtendedStatusData { get; } = new();

        public ObservableCollection<ListData> VersionData { get; } = new();

        public ObservableCollection<ListData> Pid1Data { get; } = new();

        public ObservableCollection<ListData> HistoryData { get; } = new();


        private void GetStatusData()
        {
            if (SensorBle == null) return;

            var statusTracker = IdsCanAccessoryScanResult.IdsCanAccessoryScanResultStatisticsTracker?.GetStatisticsForDeviceId(SensorBle.BleDeviceId);
            if (statusTracker is null) return;

            if (statusTracker.GetStatisticsForMessageType(IdsCanAccessoryMessageType.AccessoryId) is IdsCanAccessoryStatisticsId statisticsId)
            {
                DeviceIdData.Add(new ListData
                {
                    Title = "Mac Address",
                    Value = $"{statisticsId.AccessoryMacAddress}"
                });
                DeviceIdData.Add(new ListData
                {
                    Title = "Software Part Number",
                    Value = $"{statisticsId.SoftwarePartNumber}"
                });
                DeviceIdData.Add(new ListData
                {
                    Title = "Raw Data (16+ bytes)",
                    Value = $"{BitConverter.ToString((byte[])statisticsId.RawData)}"
                });
                DeviceIdData.Add(new ListData
                {
                    Title = "MS Since Last Received",
                    Value = $"{statisticsId.LastMessageReceivedTimestamp}"
                });
                DeviceIdData.Add(new ListData
                {
                    Title = "Number of Message Received",
                    Value = $"{statisticsId.FrequencyMetrics.MessagesReceived}"
                });
                DeviceIdData.Add(new ListData
                {
                    Title = "Avg Rate Received",
                    Value = $"{statisticsId.FrequencyMetrics.AverageTimeMs} ms"
                });
                DeviceIdData.Add(new ListData
                {
                    Title = "Shortest Time Received Msgs",
                    Value = $"{statisticsId.FrequencyMetrics.MinTimeMs} ms"
                });
                DeviceIdData.Add(new ListData
                {
                    Title = "Longest Time Received Msgs",
                    Value = $"{statisticsId.FrequencyMetrics.MaxTimeMs} ms"
                });
            }

            if (statusTracker.GetStatisticsForMessageType(IdsCanAccessoryMessageType.AccessoryConfigStatus) is IdsCanAccessoryStatisticsConfigStatus configStatus)
            {
                Pid1Data.Add(new ListData
                {
                    Title = "MS Since Last Received",
                    Value = $"{configStatus.LastMessageReceivedTimestamp}"
                });
                Pid1Data.Add(new ListData
                {
                    Title = "Number of Message Received",
                    Value = $"{configStatus.FrequencyMetrics.MessagesReceived}"
                });
                Pid1Data.Add(new ListData
                {
                    Title = "Avg Rate Received",
                    Value = $"{configStatus.FrequencyMetrics.AverageTimeMs} ms"
                });
                Pid1Data.Add(new ListData
                {
                    Title = "Shortest Time Received Msgs",
                    Value = $"{configStatus.FrequencyMetrics.MinTimeMs} ms"
                });
                Pid1Data.Add(new ListData
                {
                    Title = "Longest Time Received Msgs",
                    Value = $"{configStatus.FrequencyMetrics.MaxTimeMs} ms"
                });
                Pid1Data.Add(new ListData
                {
                    Title = "Raw Data (8 bytes)",
                    Value = $"{BitConverter.ToString((byte[])configStatus.RawData)}"
                });
            }

            var status = statusTracker.GetStatisticsForMessageType(IdsCanAccessoryMessageType.AccessoryStatus);
            if (status == null) return;

            StatusData.Add(new ListData
            {
                Title = "MS Since Last Received",
                Value = $"{status.LastMessageReceivedTimestamp}"
            });
            StatusData.Add(new ListData
            {
                Title = "Number of Message Received",
                Value = $"{status.FrequencyMetrics.MessagesReceived}"
            });
            StatusData.Add(new ListData
            {
                Title = "Avg Rate Received",
                Value = $"{status.FrequencyMetrics.AverageTimeMs} ms"
            });
            StatusData.Add(new ListData
            {
                Title = "Shortest Time Received Msgs",
                Value = $"{status.FrequencyMetrics.MinTimeMs} ms"
            });
            StatusData.Add(new ListData
            {
                Title = "Longest Time Received Msgs",
                Value = $"{status.FrequencyMetrics.MaxTimeMs} ms"
            });

            var decryptedStatusData = IdsCanAccessoryScanResult.GetDecodedDeviceStatus(SensorBle.AccessoryMacAddress, status.RawData);

            // There's a chance that we weren't able to successfully decrypt the data. If that's the case, we will still
            // display the full raw message below.
            if (decryptedStatusData is not null)
            {
                StatusData.Add(new ListData
                {
                    Title = "Status Decrypted",
                    Value = $"{BitConverter.ToString(decryptedStatusData)}"
                });
            }

            StatusData.Add(new ListData
            {
                Title = "Product Id",
                Value = $"{Sensor?.Product?.ProductId}"
            });
            StatusData.Add(new ListData
            {
                Title = "Device Type",
                Value = $"{Sensor?.LogicalId.DeviceType}"
            });
            StatusData.Add(new ListData
            {
                Title = "Function Name",
                Value = $"{Sensor?.LogicalId.FunctionName}"
            });
            StatusData.Add(new ListData
            {
                Title = "Function Instance",
                Value = $"{Sensor?.LogicalId.FunctionInstance}"
            });
            StatusData.Add(new ListData
            {
                Title = "Device Capabilities",
                Value = $"{Sensor?.DeviceCapabilityBasic.GetRawValue()}"
            });
            StatusData.Add(new ListData
            {
                Title = "Raw Encrypted Data (26 bytes)",
                Value = $"{BitConverter.ToString((byte[])status.RawData)}"
            });

            var decryptedRawData = IdsCanAccessoryScanResult.DecodeEncryptedRawData(SensorBle.AccessoryMacAddress, status.RawData);
            if (decryptedRawData is not null)
            {
                StatusData.Add(new ListData
                {
                    Title = "Decrypted Raw Bytes",
                    Value = $"{BitConverter.ToString(decryptedRawData)}"
                });
            }
        }


        private void GetExtendedStatusData()
        {
            if (SensorBle == null) return;

            var statusTracker = IdsCanAccessoryScanResult.IdsCanAccessoryScanResultStatisticsTracker?.GetStatisticsForDeviceId(SensorBle.BleDeviceId);

            // Not every device has an extended status, but if we find it we can add the data below the regular status.
            var extendedStatus = statusTracker?.GetStatisticsForMessageType(IdsCanAccessoryMessageType.IdsCanExtendedStatus);
            if (extendedStatus is null) return;

            ExtendedStatusData.Add(new ListData
            {
                Title = "Last Message",
                Value = $"{extendedStatus.LastMessageReceivedTimestamp}"
            });
            ExtendedStatusData.Add(new ListData
            {
                Title = "Total Messages",
                Value = $"{extendedStatus.FrequencyMetrics.MessagesReceived}"
            });
            ExtendedStatusData.Add(new ListData
            {
                Title = "Average Time",
                Value = $"{extendedStatus.FrequencyMetrics.AverageTimeMs} ms"
            });
            ExtendedStatusData.Add(new ListData
            {
                Title = "Minimum Time",
                Value = $"{extendedStatus.FrequencyMetrics.MinTimeMs} ms"
            });
            ExtendedStatusData.Add(new ListData
            {
                Title = "Maximum Time",
                Value = $"{extendedStatus.FrequencyMetrics.MaxTimeMs} ms"
            });

            var decryptedExtendedStatusBytes = IdsCanAccessoryScanResult.GetDecodedExtendedDeviceStatus(SensorBle.AccessoryMacAddress, extendedStatus.RawData);

            // If we can decrypt the extended status we'll display it here, but if not we'll still display the raw data below.
            if (decryptedExtendedStatusBytes is not null)
            {
                ExtendedStatusData.Add(new ListData
                {
                    Title = "Extended Status Decrypted",
                    Value = $"{BitConverter.ToString(decryptedExtendedStatusBytes)}"
                });
            }

            ExtendedStatusData.Add(new ListData
            {
                Title = "Product Id",
                Value = $"{Sensor?.Product?.ProductId}"
            });

            ExtendedStatusData.Add(new ListData
            {
                Title = "Device Type",
                Value = $"{Sensor?.LogicalId.DeviceType}"
            });

            ExtendedStatusData.Add(new ListData
            {
                Title = "Function Name",
                Value = $"{Sensor?.LogicalId.FunctionName}"
            });

            ExtendedStatusData.Add(new ListData
            {
                Title = "Function Instance",
                Value = $"{Sensor?.LogicalId.FunctionInstance}"
            });

            ExtendedStatusData.Add(new ListData
            {
                Title = "Device Capabilities",
                Value = $"{Sensor?.DeviceCapabilityBasic.GetRawValue()}"
            });

            ExtendedStatusData.Add(new ListData
            {
                Title = "Encrypted Raw Extended Bytes",
                Value = $"{BitConverter.ToString((byte[])extendedStatus.RawData)}"
            });

            var decryptedExtendedRawData = IdsCanAccessoryScanResult.DecodeEncryptedRawData(SensorBle.AccessoryMacAddress, extendedStatus.RawData);
            if (decryptedExtendedRawData is not null)
            {
                ExtendedStatusData.Add(new ListData
                {
                    Title = "Decrypted Raw Extended Bytes",
                    Value = $"{BitConverter.ToString(decryptedExtendedRawData)}"
                });
            }
        }

        private void GetVersionInfo()
        {
            if (SensorBle == null) return;

            var statusTracker = IdsCanAccessoryScanResult.IdsCanAccessoryScanResultStatisticsTracker?.GetStatisticsForDeviceId(SensorBle.BleDeviceId);
            var statistics = statusTracker?.GetStatisticsForMessageType(IdsCanAccessoryMessageType.VersionInfo);
            if (statistics is null) return;

            var versionData = new IdsCanAccessoryVersionInfo(statistics.RawData.ToArray());

            VersionData.Add(new ListData
            {
                Title = "Last Message",
                Value = $"{statistics.LastMessageReceivedTimestamp}"
            });
            VersionData.Add(new ListData
            {
                Title = "Total Messages",
                Value = $"{statistics.FrequencyMetrics.MessagesReceived}"
            });
            VersionData.Add(new ListData
            {
                Title = "Average Time",
                Value = $"{statistics.FrequencyMetrics.AverageTimeMs} ms"
            });
            VersionData.Add(new ListData
            {
                Title = "Minimum Time",
                Value = $"{statistics.FrequencyMetrics.MinTimeMs} ms"
            });
            VersionData.Add(new ListData
            {
                Title = "Maximum Time",
                Value = $"{statistics.FrequencyMetrics.MaxTimeMs} ms"
            });
            VersionData.Add(new ListData
            {
                Title = "Major Version",
                Value = $"{versionData.MajorVersion}"
            });
            VersionData.Add(new ListData
            {
                Title = "Minor Version",
                Value = $"{versionData.MinorVersion}"
            });
            VersionData.Add(new ListData
            {
                Title = "Software Part Number Base",
                Value = $"{versionData.SoftwarePartNumberBase}"
            });
            VersionData.Add(new ListData
            {
                Title = "Software Part Number Rev",
                Value = $"{versionData.SoftwarePartNumberRev}"
            });
            VersionData.Add(new ListData
            {
                Title = "MicroController",
                Value = $"{versionData.MicroController}"
            });
            VersionData.Add(new ListData
            {
                Title = "Software Stack",
                Value = $"{versionData.SoftwareStack}"
            });
            VersionData.Add(new ListData
            {
                Title = "Raw Bytes",
                Value = $"{BitConverter.ToString(statistics.RawData.ToArray())}"
            });
        }

        private ICommand? _onConnectDisconnectClicked;
        public ICommand OnConnectDisconnectClicked => _onConnectDisconnectClicked ??= new AsyncCommand(async () =>
        {
            if (ConnectDisconnect == Strings.disconnect_title)
            {
                SensorBle?.AccessoryConnectionManager?.CloseSharedConnectionAsync(true);
                ConnectDisconnect = Strings.connect_title;
                return;
            }

            try
            {
                await SensorBle?.AccessoryConnectionManager?.GetSharedConnectionAsync(CancellationToken.None, AccessorySharedConnectionOption.ManualClose);
                ConnectDisconnect = Strings.disconnect_title;
            }
            catch (Exception ex)
            {
                TaggedLog.Error(LogTag, $"Connected to Accessory {SensorBle?.BleDeviceName} failed: {ex.Message}");
            }
        });

        public double HistoryBlockExecutionTimeMs = 0;
        public List<List<byte>>? HistoryBlockData = null;
        public const int HistoryDataMaxBlocks = 100;

        private ICommand? _onHistoryDataClicked;
        public ICommand OnHistoryDataClicked => _onHistoryDataClicked ??= new AsyncCommand(async () =>
        {
            //Reset fields
            HistoryDataError = "None";
            HistoryBlockData = new List<List<byte>>();
            WasHistoryDataClicked = true;

            //Record the time it takes to execute
            Stopwatch timer = new Stopwatch();
            timer.Start();

            try
            {
                var genericSensorBle = Resolver<IDirectGenericSensorBle>.Resolve;

                for (var i = 0; i < HistoryDataMaxBlocks; i++)
                {
                    var result = await genericSensorBle.GetAccessoryHistoryDataAsync(Sensor, (byte)i, 0x00, 0xFF, CancellationToken.None);

                    if (result.Any())
                    {
                        HistoryBlockData.Add(result.ToList());
                        continue;
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                HistoryDataError = ex.Message;
                TaggedLog.Error(LogTag, $"Pulling history data has failed: {ex.Message}");
            }

            timer.Stop();
            HistoryBlockExecutionTimeMs = timer.Elapsed.TotalMilliseconds;

            FormatHistoryData();
        });

        public void FormatHistoryData()
        {
            HistoryData.Clear();

            HistoryData.Add(new ListData
            {
                Title = "Error Message",
                Value = $"{HistoryDataError}"
            });
            HistoryData.Add(new ListData
            {
                Title = "Completion Time",
                Value = $"{HistoryBlockExecutionTimeMs} ms"
            });

            if (HistoryBlockData?.Count > 0)
            {
                var index = 0;
                foreach (var block in HistoryBlockData)
                {
                    HistoryData.Add(new ListData
                    {
                        Title = $"Block {index}",
                        Value = $"{BitConverter.ToString(block.ToArray()).Trim()}"
                    });
                    index++;
                }
            }

        }

        private ICommand? _onGetPartNumberClicked;
        public ICommand OnGetPartNumberClicked => _onGetPartNumberClicked ??= new AsyncCommand(async () =>
        {
            var tokenSource = new CancellationTokenSource(GetPartNumberTimeout);
            try
            {
                _userDialogService.ShowSpinnerAsync(tokenSource.Token);
                var partNumber = await Sensor.GetSoftwarePartNumberAsync(tokenSource.Token);
                tokenSource.TryCancelAndDispose();
                await _userDialogService.AlertAsync($"Part Number: {partNumber}", "Close", CancellationToken.None);
            }
            catch (Exception ex)
            {
                TaggedLog.Error(LogTag, $"Exception getting software part number: {ex}");
                tokenSource.TryCancelAndDispose();
                await _userDialogService.AlertAsync($"Problem reading part number: {ex.Message}", "Close", CancellationToken.None);
            }
        });

        private ICommand? _onKeySeedClicked;
        public ICommand OnKeySeedClicked => _onKeySeedClicked ??= new AsyncCommand(async () =>
        {
            var accessoryDescription = Sensor.LogicalId.ToString(LogicalDeviceIdFormat.Debug);

            try
            {
                var bleDevice = await SensorBle?.AccessoryConnectionManager?.GetSharedConnectionAsync(CancellationToken.None, AccessorySharedConnectionOption.DontConnect);

                TaggedLog.Information(LogTag, $"Connected to Accessory {accessoryDescription} attempting unlock");

                BleDeviceUnlockManager bleDeviceUnlockManager = new(AccessoryConnectionManager.KeySeedExchangeServiceGuidDefault, AccessoryConnectionManager.SeedCharacteristicGuidDefault, AccessoryConnectionManager.KeyCharacteristicGuidDefault);
                var keySeedResult = await bleDeviceUnlockManager.PerformKeySeedExchange(bleDevice, AccessoryConnectionManager.KeySeedExchangeCypher, CancellationToken.None);
                if (keySeedResult != BleDeviceKeySeedExchangeResult.Succeeded)
                    TaggedLog.Information(LogTag, $"Failed KeySeed exchange.");
            }
            catch (Exception ex)
            {
                TaggedLog.Error(LogTag, $"Connected to Accessory {accessoryDescription} KeySeed exchange failed: {ex.Message}");
            }
        });

        private ICommand? _onRemoveAccessoryClicked;
        public ICommand OnRemoveAccessoryClicked => _onRemoveAccessoryClicked ??= new AsyncCommand(async () =>
        {
            var accessoryDescription = Sensor.LogicalId.ToString(LogicalDeviceIdFormat.Debug);
            try
            {
                bool isRemoveConfirmd = await _userDialogService
                    .ConfirmAsync(Strings.remove_accessory_question, Strings.remove_accessory, Strings.cancel, CancellationToken.None);
                if (!isRemoveConfirmd)
                    return;

                var genericSensorBle = Resolver<IDirectGenericSensorBle>.Resolve;

                RemoveGenericAccessory(SensorBle, _logicalDeviceService, genericSensorBle);

                await NavigationService.GoBackToRootAsync();
            }
            catch (Exception ex)
            {
                TaggedLog.Error(LogTag, $"Remove accessory {accessoryDescription} failed: {ex.Message}");
            }
        });

        public static void RemoveGenericAccessory(GenericSensorBle? sensorBle, ILogicalDeviceService logicalDeviceService, IDirectGenericSensorBle? genericSensorBle)
        {
            if (sensorBle is null)
            {
                throw new ArgumentNullException(nameof(sensorBle));
            }

            if (logicalDeviceService is null)
            {
                throw new ArgumentNullException(nameof(logicalDeviceService));
            }

            if (genericSensorBle is null)
            {
                throw new ArgumentNullException(nameof(genericSensorBle));
            }

            SensorConnectionGeneric sensorConnection = new SensorConnectionGeneric(sensorBle.BleDeviceName, sensorBle.BleDeviceId, sensorBle.AccessoryMacAddress, sensorBle.SoftwarePartNumber);
            bool isSensorConnectionRemoved = AppSettings.Instance.RemoveSensorConnection(sensorConnection);
            if (!isSensorConnectionRemoved)
            {
                throw new InvalidOperationException("Cannot remove sensor connection");
            }

            genericSensorBle.UnRegisterGenericSensor(sensorBle.BleDeviceId);

            logicalDeviceService.DeviceManager?.RemoveLogicalDevice(x => x.LogicalId.ImmutableUniqueId.Equals(sensorBle.LogicalDevice?.LogicalId.ImmutableUniqueId));

            var macHistory = AppSettings.Instance.MacHistory.Where(x => !x.Equals(sensorBle.AccessoryMacAddress.ToString()))?.ToList() ?? new List<string>();
            AppSettings.Instance.SetMacHistory(macHistory);
            AppSettings.Instance.TakeSnapshot();
        }
    }
}
