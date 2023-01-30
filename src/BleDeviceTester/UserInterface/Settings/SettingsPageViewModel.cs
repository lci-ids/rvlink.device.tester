using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.Portable.Platform;
using IDS.UI.Interfaces;
using IDS.UI.UserDialogs;
using OneControl.Direct.IdsCanAccessoryBle.GenericSensor;
using OneControl.UserInterface.Common;
using Prism.Commands;
using Prism.Navigation;
using PrismExtensions.Enums;
using PrismExtensions.ViewModels;
using RvLinkDeviceTester.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.CommunityToolkit.ObjectModel;

namespace RvLinkDeviceTester.UserInterface.Settings
{
    public class SettingsPageViewModel : BaseViewModel, IViewModelConfirmNavigation
    {
        private const string LogTag = nameof(SettingsPageViewModel);

        private readonly IUserDialogService _userDialogService;
        private readonly ILogicalDeviceService _logicalDeviceService;

        public string Title => Strings.settings_title;

        public ObservableCollection<ICellViewModel>? SettingCellViewModels { get; private set; }

        private bool _canGoBack = true;
        public bool CanGoBack
        {
            get => _canGoBack;
            set => SetProperty(ref _canGoBack, value);
        }

        private ICommand? _name;
        public ICommand GoBack => _name ??= new AsyncCommand(async () => await NavigationService.GoBackAsync());

        private CellViewModel _removeAllAccessoriesCellViewModel;

        public SettingsPageViewModel(INavigationService navigationService, IUserDialogService userDialogService,
            ILogicalDeviceService logicalDeviceService)
            : base(navigationService)
        {
            _userDialogService = userDialogService;
            _logicalDeviceService = logicalDeviceService;

            SettingCellViewModels = new ObservableCollection<ICellViewModel>();

            SettingCellViewModels.Add(new CellViewModel(text: "GO TO LOG VIEWER")
            {
                CellCommand = GoToLogViewer
            });

            SettingCellViewModels.Add(new CellViewModel(text: Strings.diagnostics_title, description: $"App Version: {AppInfo.Instance.VersionInfo}")
            {
                CellCommand = GoToDiagnostics
            });

            _removeAllAccessoriesCellViewModel = new CellViewModel(text: Strings.remove_all_accessories)
            {
                CellCommand = RemoveAllAccessories
            };
            SettingCellViewModels.Add(_removeAllAccessoriesCellViewModel);
        }

        public bool ConfirmNavigation(INavigationParameters? parameters) => CanGoBack;

        private ICommand? _goToLogViewer;

        public ICommand GoToLogViewer => _goToLogViewer ??= new DelegateCommand(async () =>
        {
            Navigating = true;
            await NavigationService.NavigateAsync(nameof(LogViewer.LogViewerPage));
            Navigating = false;
        }, () => !Navigating).ObservesProperty(() => Navigating);

        private ICommand? _goToDiagnostics;

        public ICommand GoToDiagnostics => _goToDiagnostics ??= new DelegateCommand(async () =>
        {
            Navigating = true;
            await NavigationService.NavigateAsync(nameof(Diagnostics.SystemDiagnosticsPage));
            Navigating = false;
        }, () => !Navigating).ObservesProperty(() => Navigating);

        private bool _navigating;
        public bool Navigating
        {
            get => _navigating;
            set => SetProperty(ref _navigating, value);
        }

        private bool _useKeySeedChecked = AppSettings.Instance.UseKeySeedExchange;
        public bool UseKeySeedChecked
        {
            get => _useKeySeedChecked;
            set
            {
                AppSettings.Instance.SetUseKeySeedExchange(value);
                SetProperty(ref _useKeySeedChecked, value);
            }
        }

        private bool _generateNotificationsChecked = AppSettings.Instance.GenerateNotifications;
        public bool GenerateNotificationsChecked
        {
            get => _generateNotificationsChecked;
            set
            {
                AppSettings.Instance.SetGenerateNotifications(value);
                SetProperty(ref _generateNotificationsChecked, value);
            }
        }

        private ICommand? _removeAllAccessories;
        public ICommand RemoveAllAccessories => _removeAllAccessories ??= new DelegateCommand(async () =>
        {
            Navigating = true;
            try
            {
                bool isRemoveConfirmd = await _userDialogService
                    .ConfirmAsync(Strings.remove_all_accessories_question, Strings.remove_all, Strings.cancel, CancellationToken.None);
                if (!isRemoveConfirmd)
                    return;

                var genericSensorDeviceSource = Resolver<IDirectGenericSensorBle>.Resolve;
                
                var sensorList = _logicalDeviceService.DeviceManager?.LogicalDevices ?? new List<ILogicalDevice>();

                int count = sensorList.Count();
                int index = 1;
                int errorCount = 0;
                foreach (var sensor in sensorList)
                {
                    _removeAllAccessoriesCellViewModel.Description = $"{index} of {count}";
                    if(!TryRemoveGenericAccessory(sensor, _logicalDeviceService, genericSensorDeviceSource))
                    {
                        errorCount++;
                    }
                    index++;
                    
                }
                _removeAllAccessoriesCellViewModel.Description = errorCount > 0 ? $"Error: only {count-errorCount} out of {count} removed." : $"All removed successfully.";

            }
            finally
            {
                Navigating = false;
            }
        }, () => !Navigating).ObservesProperty(() => Navigating);

        private bool TryRemoveGenericAccessory(ILogicalDevice? sensor, ILogicalDeviceService logicalDeviceService, IDirectGenericSensorBle? genericSensorBle)
        {
            try
            {
                var sensorBle = genericSensorBle?.GetGenericSensor(sensor);
                Devices.GenericSensorPageViewModel.RemoveGenericAccessory(sensorBle, _logicalDeviceService, genericSensorBle);
                return true;
            }
            catch (Exception ex)
            {
                TaggedLog.Error(LogTag, $"Cannot remove accessory {sensor?.DeviceName}. Error: {ex.Message}");
                return false;
            }
        }
    }
}
