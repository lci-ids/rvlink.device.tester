using System;

namespace RvLinkDeviceTester.Services
{
    public interface IDeviceSettingsService
    {
        void NavigateToLocationSourceSettings();
        void NavigateToBluetoothSettings();
        void EnableBluetoothAdapter();
        bool IsBluetoothEnabled { get; }
        public bool AreLocationServicesEnabled { get; }
    }
}

