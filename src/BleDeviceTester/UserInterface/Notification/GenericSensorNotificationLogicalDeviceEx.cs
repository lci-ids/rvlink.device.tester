using IDS.Portable.LogicalDevice;
using IDS.Portable.Notifications.Platforms.Shared;
using OneControl.Devices;
using System;

namespace RvLinkDeviceTester.UserInterface.Notification
{
    public class GenericSensorNotificationLogicalDeviceEx : LogicalDeviceExBase<LogicalDeviceGenericSensor>, ILogicalDeviceExAlertChanged
    {
        protected override string LogTag => nameof(GenericSensorNotificationLogicalDeviceEx);
        public static ILogicalDeviceEx? LogicalDeviceExFactory(ILogicalDevice logicalDevice) => LogicalDeviceExFactory<GenericSensorNotificationLogicalDeviceEx>(logicalDevice, GetLogicalDeviceScope);
        protected static LogicalDeviceExScope GetLogicalDeviceScope(ILogicalDevice logicalDevice) => LogicalDeviceExScope.Shared;
        public static GenericSensorNotificationLogicalDeviceEx? SharedExtension => GetSharedExtension<GenericSensorNotificationLogicalDeviceEx>(autoCreate: true);

        public async void LogicalDeviceAlertChanged(ILogicalDevice logicalDevice, ILogicalDeviceAlert fromAlert, ILogicalDeviceAlert toAlert)
        {
            //Do not generate an alert if the user hasn't enabled them in settings
            if (AppSettings.Instance.GenerateNotifications == false)
                return;

            UserNotificationCenter.Instance.NotifyUser(new LocalUserNotification(
                "LoCap Device Alert", toAlert.Name, PriorityLevel.Max, Visibility.Public
            ));
        }
    }
}
