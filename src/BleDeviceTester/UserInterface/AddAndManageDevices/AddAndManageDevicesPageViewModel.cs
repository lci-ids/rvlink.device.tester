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
using Xamarin.Essentials;
using MainThread = Xamarin.Essentials.MainThread;

namespace RvLinkDeviceTester.UserInterface.AddAndManageDevices
{
    public class AddAndManageDevicesPageViewModel : BaseViewModel, IViewModelConfirmNavigation, IViewModelResumePause
    {
        public string Title => Strings.add_and_manage;

        #region Navigation
        private bool _canGoBack = true;
        public bool CanGoBack
        {
            get => _canGoBack;
            set => SetProperty(ref _canGoBack, value);
        }

        private ICommand? _goBack;
        public ICommand GoBack => _goBack ??= new AsyncCommand(async () => await NavigationService.GoBackAsync());

        private ICommand? _goToPushToPair;

        public ICommand GoToPushToPair => _goToPushToPair ??= new DelegateCommand(async () =>
        {
            Navigating = true;
            await NavigationService.NavigateAsync(nameof(PushToPair.PushToPairPage));
            Navigating = false;
        }, () => !Navigating).ObservesProperty(() => Navigating);

        private ICommand? _goToMacConnection;

        public ICommand GoToMacConnection => _goToMacConnection ??= new DelegateCommand(async () =>
        {
            Navigating = true;
            await NavigationService.NavigateAsync(nameof(MacAddressPair.MacAddressPairingPage));
            Navigating = false;
        }, () => !Navigating).ObservesProperty(() => Navigating);

        private bool _navigating;
        public bool Navigating
        {
            get => _navigating;
            set => SetProperty(ref _navigating, value);
        }
        #endregion

        private IDeviceSettingsService _deviceSettingsService;

        public AddAndManageDevicesPageViewModel(INavigationService navigationService, IDeviceSettingsService deviceSettingsService) : base(navigationService)
        {
            _deviceSettingsService = deviceSettingsService;

            //Start BLE Scanner
            BleScannerService.Instance.Start(false);
        }

        public Task OnResumeAsync(ResumeReason reason, INavigationParameters? parameters, CancellationToken resumePauseCancellationToken)
        {
            CheckPermissions();
            return Task.CompletedTask;
        }

        public void OnPause(PauseReason reason)
        {
        }

        private void CheckPermissions()
        {
            //Request permissions 
            MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                // Theses will bump you out to settings, so only do 1 or the other 
                if (!_deviceSettingsService.AreLocationServicesEnabled)
                    _deviceSettingsService.NavigateToLocationSourceSettings();
                else if (!_deviceSettingsService.IsBluetoothEnabled)
                    _deviceSettingsService.EnableBluetoothAdapter();
            });
        }

        public static int DeviceCellViewModelSorter(ICellViewModel cellA, ICellViewModel cellB)
        {
            return string.Compare(cellA.Text, cellB.Text, StringComparison.CurrentCulture);
        }

        public bool ConfirmNavigation(INavigationParameters? parameters) => CanGoBack;
    }
}
