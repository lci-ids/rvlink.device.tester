using System;
using System.Reflection;
using System.Threading.Tasks;
using RvLinkDeviceTester.Services;
using RvLinkDeviceTester.Services.Realm;
using RvLinkDeviceTester.UserInterface.Notification;
using DryIoc;
using IDS.Portable.BLE.Platforms.Shared;
using IDS.Portable.BLE.Platforms.Shared.BleScanner;
using IDS.Portable.Common;
using IDS.Portable.Devices.TPMS;
using IDS.Portable.LogicalDevice;
using IDS.UI.UserDialogs;
using OneControl.Devices;
using OneControl.Devices.GenericSensor;
using OneControl.Direct.IdsCanAccessoryBle.AwningSensor;
using OneControl.Direct.IdsCanAccessoryBle.BatteryMonitor;
using OneControl.Direct.IdsCanAccessoryBle.GenericSensor;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;
using OneControl.Direct.IdsCanAccessoryBle.TemperatureSensor;
using OneControl.Direct.MyRvLinkBle;
using Prism;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Plugin.Popups;
using PrismExtensions.Extensions;
using Xamarin.Forms;

namespace RvLinkDeviceTester
{
    public partial class App : PrismApplication
    {
        private const string LogTag = nameof(App);

        private static readonly object _lock = new object();

        /// <summary>
        /// Actual Dry Ioc Container which can be called and used for manual registering and resolving dependencies.
        /// </summary>
        public static IContainer? AppContainer { get; set; }

        public App() : this(null)
        {
        }

        public App(IPlatformInitializer initializer) : base(initializer)
        {
        }

        protected override async void OnInitialized()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(
                "NjI3MDU5QDMyMzAyZTMxMmUzMGQ3V1c3RWcvZTVySGZMV1JRcDhraDhHcWkvdmhjR0dqWVV5MHhaalcyS3c9");

            InitializeComponent();

            // Load settings now that our base is setup
            //
            AppSettings.Instance.Load();

            await NavigationService.NavigateAsync("NavigationPage/MainPage");
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            RegisterServices();

            // Rg.Plugins.Popup and Prism.Plugins.Popup
            // 
            containerRegistry.RegisterPopupNavigationService();
            containerRegistry.RegisterPopupDialogService();

            containerRegistry.RegisterPrismExtensions();
            
            containerRegistry.RegisterInstance(typeof(IAppSettings), AppSettings.Instance);
            containerRegistry.RegisterInstance(typeof(ILogicalDeviceService), Resolver<ILogicalDeviceService>.Resolve);
            containerRegistry.RegisterInstance(typeof(ILogicalDeviceManager), Resolver<ILogicalDeviceService>.Resolve.DeviceManager!);
            containerRegistry.RegisterInstance(typeof(ILogicalDeviceTagManager), Resolver<ILogicalDeviceService>.Resolve.DeviceManager!.TagManager);
            containerRegistry.RegisterInstance(typeof(IUserDialogService), UserDialogService.Instance);
            containerRegistry.RegisterInstance(typeof(ILogViewerFileService), LogViewerFileService.Instance);
            containerRegistry.RegisterInstance(typeof(ILogViewerRealTimeService), LogViewerRealTimeService.Instance);

            containerRegistry.RegisterInstance(typeof(IRealmDeviceHistoryService), RealmDeviceHistoryService.Instance);

            containerRegistry.RegisterForNavigation<NavigationPage>();

            AppContainer = containerRegistry.GetContainer();
        }

        protected override void OnStart()
        {
            BleScannerService.Instance.Start(filterUsingExplicitServiceUuids: false);
        }

        protected override void OnResume()
        {
            this.PrismExtensionsOnResume();

            BleScannerService.Instance.Start(filterUsingExplicitServiceUuids: false);
        }

        protected override void OnSleep()
        {
            this.PrismExtensionsOnSleep();
        }

        #region Registration
        private static bool _registered = false;
        public static void RegisterServices()
        {
            lock (_lock)
            {
                // Register services that can't be auto registered. These are app wide services that should be registered ASAP.
                // 

                // This protection if here for Android. If the user exits the App by pressing the back button on the
                // system navigation bar, the App is not backgrounded, the App is actually finalized. If the App is
                // resumed from the finalized state before Android destroys the App (to free up system memory) the
                // Resolver will throw an exception if the service are registered again. The Resolver is static and
                // it will not be cleared from memory unless the App is destroyed.
                //
                if( _registered )
                    return;
                _registered = true;

                // Setup UnhandledException Handler
                //
                void UnhandledException(object? sender, Exception? ex)
                {
                    TaggedLog.Error("!!! UNCAUGHT APP EXCEPTION !!!",
                        $"{ex?.GetType().Name ?? "<null-exception>"}: " +
                        $"{ex?.Message ?? "<null-message>"}\n" +
                        $"{ex?.StackTrace ?? "<null-stacktrace>"}");
                }

                AppDomain.CurrentDomain.FirstChanceException += (sender, args) => UnhandledException(sender, args.Exception);
                AppDomain.CurrentDomain.UnhandledException += (sender, args) => UnhandledException(sender, args.ExceptionObject as Exception);

                // DO NOT Lazy Register ILogicalDeviceServiceIdsCan.  There needs to be AutoRegisterJsonSerializersFromAssembly called for both
                // this assembly and the LogicalDevice assembly to make sure we can de-serialize the settings.
                //
                Resolver<ILogicalDeviceServiceIdsCan>.Register(() =>
                {
                    JsonSerializer.AutoRegisterJsonSerializersFromAssembly(Assembly.GetExecutingAssembly());

                    var options = LogicalDeviceServiceOptions.AutoFavoriteAccessoryDevices | LogicalDeviceServiceOptions.AllowFastPids | LogicalDeviceServiceOptions.AutoRegisterReactiveStatusChangedExtension | LogicalDeviceServiceOptions.SingletonMode;

                    var logicalDeviceService = new LogicalDeviceService(options);

                    logicalDeviceService.RegisterAllLogicalDeviceFactories();

                    RegisterLogicalDeviceExtensions(logicalDeviceService);

                    //logicalDeviceService.RegisterDirectIdsCanAccessoryBleLogicalDeviceFactories(() => new LPSettingsRepository(AppDirectServices.Instance, logicalDeviceService.DeviceManager.TagManager));

                    logicalDeviceService.EnableRealTimeClockUpdates(LogicalDeviceService.DefaultRealTimeClockUpdateInterval);

                    return logicalDeviceService;
                });

                Resolver<ILogicalDeviceService>.LazyConstructAndRegister(() => Resolver<ILogicalDeviceServiceIdsCan>.Resolve);

                RegisterBleServices();
            }
        }

        private static void RegisterLogicalDeviceExtensions(ILogicalDeviceService logicalDeviceService)
        {
            //Generic Sensor
            logicalDeviceService.RegisterLogicalDeviceExFactory(GenericSensorNotificationLogicalDeviceEx.LogicalDeviceExFactory);

            //Temperature Sensor
            //logicalDeviceService.RegisterLogicalDeviceExFactory(TemperatureSensorNotificationLogicalDeviceEx.LogicalDeviceExFactory);

            //Battery Monitor
            //logicalDeviceService.RegisterLogicalDeviceExFactory(BatteryMonitorNotificationLogicalDeviceEx.LogicalDeviceExFactory);

            //Awning Sensor
            //logicalDeviceService.RegisterLogicalDeviceExFactory(AwningSensorNotificationLogicalDeviceEx.LogicalDeviceExFactory);
        }

        private static void RegisterBleServices()
        {
            var runningIniOSSimulator = false;

#if DEBUG
            // this seems to be detecting the iOS simulator correctly
            runningIniOSSimulator = Xamarin.Essentials.DeviceInfo.DeviceType == Xamarin.Essentials.DeviceType.Virtual &&
                                    Xamarin.Essentials.DeviceInfo.Platform == Xamarin.Essentials.DevicePlatform.iOS;
#endif

            if (!runningIniOSSimulator)
            {
                BleScannerService.Instance.FactoryRegistry.Register(new BleGatewayScanResultPrimaryServiceFactory());
                BleScannerService.Instance.FactoryRegistry.Register(new TirePressureMonitorBleScanResultPrimaryServiceFactory());
                BleScannerService.Instance.FactoryRegistry.Register(new MyRvLinkGatewayBleScanResultPrimaryServiceFactory());
                BleScannerService.Instance.FactoryRegistry.Register(new X180TGatewayBleScanResultPrimaryServiceFactory());
                BleScannerService.Instance.FactoryRegistry.Register(new IdsCanAccessoryBleScanResultPrimaryServiceFactory());
                //BleScannerService.Instance.FactoryRegistry.Register(new MopekaBleScanResultFactory());
            }

            Resolver<IDirectTemperatureSensorBle>.LazyConstructAndRegister(() => new DirectTemperatureSensorBle(Resolver<ILogicalDeviceService>.Resolve));
            Resolver<IDirectAwningSensorBle>.LazyConstructAndRegister(() => new DirectAwningSensorBle(Resolver<ILogicalDeviceService>.Resolve));
            Resolver<IDirectBatteryMonitorBle>.LazyConstructAndRegister(() => new DirectBatteryMonitorBle(Resolver<ILogicalDeviceService>.Resolve));
            Resolver<IDirectGenericSensorBle>.LazyConstructAndRegister(() => new DirectGenericSensorBle(Resolver<ILogicalDeviceService>.Resolve));
        }
        #endregion
    }
}
