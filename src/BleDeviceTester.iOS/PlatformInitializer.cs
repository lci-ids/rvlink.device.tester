using System;
using RvLinkDeviceTester.iOS.Services;
using RvLinkDeviceTester.Services;
using Prism;
using Prism.Ioc;

namespace RvLinkDeviceTester.iOS
{
    public class PlatformInitializer : IPlatformInitializer
    {
        public PlatformInitializer()
        {
        }

        public void RegisterTypes(IContainerRegistry container)
        {
            // Register any platform specific implementations
            container.RegisterSingleton(typeof(IDeviceSettingsService), typeof(DeviceSettingsService));
            //container.RegisterSingleton(typeof(IPushNotificationService), typeof(PushNotificationService));
            //container.RegisterSingleton(typeof(INativeBackgroundService), typeof(NativeBackgroundService));
        }
    }
}