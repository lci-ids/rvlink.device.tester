using RvLinkDeviceTester.UserInterface.Devices.GenericSensor;
using IDS.Portable.LogicalDevice;
using OneControl.Direct.IdsCanAccessoryBle.GenericSensor;
using Prism.Commands;
using Prism.Navigation;
using PrismExtensions.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Xamarin.CommunityToolkit.ObjectModel;
using RvLinkDeviceTester.Connections.Sensors;
using System.Collections.Generic;
using IDS.UI.UserDialogs;
using IDS.Portable.Common;
using OneControl.Direct.IdsCanAccessoryBle;
using RvLinkDeviceTester.Resources;
using System.Threading;
using OneControl.Direct.MyRvLink;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;
using IDS.Portable.Common.Extensions;

namespace RvLinkDeviceTester.UserInterface.Devices
{
    public class GenericDevicePageViewModel : BaseViewModel, IViewModelConfirmNavigation, INavigatedAware
    {
        public ILogicalDevice Device { get; private set; }
        private const string LogTag = nameof(GenericDevicePageViewModel);
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

        //DeviceId
        private bool _isDeviceIdVisible;
        public bool IsDeviceIdVisible
        {
            get => _isDeviceIdVisible;
            set => SetProperty(ref _isDeviceIdVisible, value);
        }
        private ICommand? _deviceIdDropdownCommand;
        public ICommand DeviceIdDropdownCommand => _deviceIdDropdownCommand ??= new DelegateCommand(() => IsDeviceIdVisible = !IsDeviceIdVisible);

        //DeviceStatus
        private bool _isDeviceStatusVisible;
        public bool IsDeviceStatusVisible
        {
            get => _isDeviceStatusVisible;
            set => SetProperty(ref _isDeviceStatusVisible, value);
        }
        private ICommand? _deviceStatusDropdownCommand;
        public ICommand DeviceStatusDropdownCommand => _deviceStatusDropdownCommand ??= new DelegateCommand(() => IsDeviceStatusVisible = !IsDeviceStatusVisible);

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

        public GenericDevicePageViewModel(INavigationService navigationService, ILogicalDeviceService logicalDeviceService,
            IUserDialogService userDialogService)
            : base(navigationService)
        {
            _logicalDeviceService = logicalDeviceService;
            _userDialogService = userDialogService;

            RaiseAllPropertiesChanged();
        }

        public void OnNavigatedTo(INavigationParameters parameters)
        {
            Device = parameters.GetValue<ILogicalDevice>("device");
            Title = Device.DeviceName;

            //Automatically Expand these cells as there isn't much data yet
            IsDeviceIdVisible = true;
            IsDeviceStatusVisible = true;

            GetDeviceData();
        }

        public ObservableCollection<ListData> DeviceData { get; } = new();

        public ObservableCollection<ListData> StatusData { get; } = new();

        private void GetDeviceData()
        {
            //Clear existing data
            DeviceData.Clear();
            StatusData.Clear();

            //DeviceData
            DeviceData.Add(new ListData
            {
                Title = "Mac",
                Value = Device.LogicalId.ProductMacAddress?.ToString()
            });
            DeviceData.Add(new ListData
            {
                Title = "Id",
                Value = Device.LogicalId.ImmutableUniqueId.ToString()
            });
            DeviceData.Add(new ListData
            {
                Title = "Instance",
                Value = Device.LogicalId.DeviceInstance.ToString()
            });
            DeviceData.Add(new ListData
            {
                Title = "Type",
                Value = Device.LogicalId.DeviceType.ToString()
            });
            DeviceData.Add(new ListData
            {
                Title = "Function Name",
                Value = Device.LogicalId.FunctionName.ToString()
            });
            DeviceData.Add(new ListData
            {
                Title = "Capability",
                Value = Device.DeviceCapabilityBasic.GetRawValue().ToString()
            });
            DeviceData.Add(new ListData
            {
                Title = "Logical Device Info",
                Value = Device.ToString()
            });

            //StatusData
            if (Device is ILogicalDeviceWithStatus logicalDeviceWithStatus)
            {
                var statusData = logicalDeviceWithStatus.RawDeviceStatus.CopyCurrentData();
                StatusData.Add(new ListData
                {
                    Title = "Status Data",
                    Value = statusData.Count() > 0 ? statusData.DebugDump() : "Empty"
                });
                StatusData.Add(new ListData
                {
                    Title = "Last Updated",
                    Value = logicalDeviceWithStatus.LastUpdatedTimestamp.Year > 2000 ? logicalDeviceWithStatus.LastUpdatedTimestamp.ToString() : "Unknown" 
                });
            }
            else
            {
                IsDeviceStatusVisible = false;
                StatusData.Add(new ListData
                {
                    Title = "Device isn't a logical device with status",
                    Value = "No status data to display"
                });

            }
        }

        private ICommand? _onRefreshDataClicked;
        public ICommand OnRefreshDataClicked => _onRefreshDataClicked ??= new AsyncCommand(async () =>
        {
            GetDeviceData();
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
