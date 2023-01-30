using System.Windows.Input;
using Prism.Navigation;
using Xamarin.CommunityToolkit.ObjectModel;
using IDS.Portable.Common;
using IDS.UI.Interfaces;
using System;
using PrismExtensions.ViewModels;
using RvLinkDeviceTester.UserInterface.Second;
using Prism.Commands;
using RvLinkDeviceTester.Resources;
using OneControl.UserInterface.Common;
using PrismExtensions.Enums;
using System.Threading.Tasks;
using System.Threading;
using IDS.Portable.LogicalDevice;
using Xamarin.Forms;
using System.Linq;
using RvLinkDeviceTester.UserInterface.Devices;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;

namespace RvLinkDeviceTester.UserInterface.Main
{
    public class MainPageViewModel : BaseViewModel, IViewModelResumePause
    {
        public string Title => Strings.app_name;
        private const string LogTag = nameof(MainPageViewModel);

        private ICommand? _continue;
        public ICommand Continue => _continue ??= new AsyncCommand(async () => await NavigationService.NavigateAsync(nameof(SecondPage)));

        private readonly ILogicalDeviceService _logicalDeviceService;

        public MainPageViewModel(INavigationService navigationService, ILogicalDeviceService logicalDeviceService)
            : base(navigationService)
        {
            _logicalDeviceService = logicalDeviceService;
            ShowNoDevicesView = Devices.Count == 0;
        }

        #region Lifecycle
        public void OnPause(PauseReason reason)
        {
        }
        public async Task OnResumeAsync(ResumeReason reason, INavigationParameters? parameters, CancellationToken resumePauseCancellationToken)
        {
            UpdateDeviceList();
        }
        #endregion

        private void UpdateDeviceList()
        {
            if (_logicalDeviceService is null || _logicalDeviceService.DeviceManager is null)
            {
                return;
            }

            //Grab a list of all registered sensors
            var sensorList = _logicalDeviceService.DeviceManager.LogicalDevices;

            //Empty Current Device List
            Devices.Clear();

            //Generate new device list
            foreach (var (sensor, index) in sensorList.Select((value, index) => (value, index)))
            {
                Devices.Add(
                    new CellViewModel(sensor.DeviceName, sensor.Product?.ProductId.Name)
                    {
                        CellCommand = new Command(async () => await NavigateToSensor(index))
                    });
            }

            //Set variable to adjust UI
            ShowNoDevicesView = Devices.Count == 0;  
        }

        private async Task NavigateToSensor(int deviceIndex)
        {
            if (_logicalDeviceService is null || _logicalDeviceService.DeviceManager is null)
            {
                TaggedLog.Warning(LogTag, $"Unable to navigate to sensor, device manager is null");
                return;
            }

            //Grab a list of all registered sensors
            var sensorList = _logicalDeviceService.DeviceManager.LogicalDevices;
            if (sensorList.Count() < deviceIndex)
            {
                TaggedLog.Warning(LogTag, $"Unable to navigate to sensor, requested index is too large for the list");
                return;
            }

            var sensorSelected = sensorList.ToList()[deviceIndex];

            if (sensorSelected == null)
            {
                TaggedLog.Warning(LogTag, $"Unable to navigate to sensor, selected sensor is null");
                return;
            }
                

            //Launch Sensor Page
            var navigationParameter = new NavigationParameters();
            navigationParameter.Add("sensor", sensorSelected);

            await NavigationService.NavigateAsync(nameof(GenericSensorPage), navigationParameter);

        }


        private ICommand? _addCommand;

        public ICommand AddCommand => _addCommand ??= new DelegateCommand(async () =>
        {
            //Stiff arm add device page if blue tooth is not enabled
            //if (!_deviceSettingsService.IsBluetoothEnabled && DeviceInfo.Instance.Variant != DeviceVariant.Simulator)
            //{
            //    ShowBluetoothPermissionRequest = true;
            //}
            //else
            //{
            //    Navigating = true;
            //    await NavigateAsync(nameof(AddAndManageDevicesPage));
            //    Navigating = false;
            //}

            Navigating = true;
            await NavigationService.NavigateAsync(nameof(AddAndManageDevices.AddAndManageDevicesPage));
            //await NavigationService.NavigateAsync(nameof(DevicePage));
            Navigating = false;
        }, () => !Navigating).ObservesProperty(() => Navigating);

        private ICommand? _goToSettings;

        public ICommand GoToSettings => _goToSettings ??= new DelegateCommand(async () =>
        {
            Navigating = true;
            await NavigationService.NavigateAsync(nameof(Settings.SettingsPage));
            Navigating = false;
        }, () => !Navigating).ObservesProperty(() => Navigating);

        private bool _navigating;
        public bool Navigating
        {
            get => _navigating;
            set => SetProperty(ref _navigating, value);
        }

        private bool _showNoDevicesView;

        public bool ShowNoDevicesView
        {
            get => _showNoDevicesView;
            set => SetProperty(ref _showNoDevicesView, value);
        }

        /// <summary>
        /// This method allows us to sort Device Cell's by their name, expect for TankSensors that we
        /// want at the bottom of the list.
        /// </summary>
        /// <param name="cellA"></param>
        /// <param name="cellB"></param>
        /// <returns></returns>
        public static int DeviceCellViewModelSorter(ICellViewModel cellA, ICellViewModel cellB)
        {
            /*
            if (cellA is IControlCellViewModel && cellB is IControlCellViewModel)
            {
                return cellA.CompareTo(cellB);
            }

            if (cellA is IControlCellViewModel)
                return -1;
            // We put Tank Sensors at the bottom of the list.
            //
            if (cellA is TankSensorCellViewModel && !(cellB is TankSensorCellViewModel))
                return 1;

            // We put Tank Sensors at the bottom of the list.
            //
            if (cellA is not TankSensorCellViewModel && cellB is TankSensorCellViewModel)
                return -1;

            // We put Power Monitor at the bottom of the list.
            //
            if (cellA is PowerMonitorCellViewModel && cellB is not PowerMonitorCellViewModel)
                return 1;

            // We put Power Monitor at the bottom of the list.
            //
            if (cellA is not PowerMonitorCellViewModel && cellB is PowerMonitorCellViewModel)
                return -1;

            */

            return string.Compare(cellA.Text, cellB.Text, StringComparison.CurrentCulture);
        }

        private readonly ComparingObservableCollection<ICellViewModel> _devices = new ComparingObservableCollection<ICellViewModel>(DeviceCellViewModelSorter, orderAscending: true);
        public ComparingObservableCollection<ICellViewModel> Devices { get => _devices; }
        
    }
}
