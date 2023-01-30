using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RvLinkDeviceTester.Connections.Rv;
using RvLinkDeviceTester.Connections.Sensors;
using IDS.Portable.Common;
using IDS.Portable.Common.Utils;
using IDS.Portable.LogicalDevice;

namespace RvLinkDeviceTester
{
    public interface IAppSettingsLoadSave
    {
        void Load(bool force = false);
        void Save();
    }

    public partial class AppSettings : IAppSettingsLoadSave
    {
        public const int MaxSnapshotLoadWarningTimeMs = 250;

        private bool _settingsLoaded = false;

        public void Load(bool force = false)
        {
            if (_settingsLoaded is false || force is true)
            {
                var deviceManager = Resolver<ILogicalDeviceService>.Resolve?.DeviceManager;
                if (deviceManager != null)
                {
                    // Load the snapshot data
                    //
                    using (new PerformanceTimer(LogTag, $"Snapshot Load", TimeSpan.FromMilliseconds(MaxSnapshotLoadWarningTimeMs), PerformanceTimerOption.AutoStartOnCreate | PerformanceTimerOption.OnShowStopTotalTimeInMs))
                    {
                        if (AppSettingsDeviceSnapshotSerializable.TryLoad(out var deviceSnapshotSerializable) && deviceSnapshotSerializable != null)
                        {
                            DeviceSnapshot = deviceSnapshotSerializable.DeviceSnapshot;
                            deviceManager.AddLogicalDevices(DeviceSnapshot, (snapshotDevice) => true);
                        }
                    }
                }

                // Load the AppSettings from storage and apply them
                //
                if (AppSettingsSerializable.TryLoad(out var appSettings) && appSettings != null)
                    ApplySettings(appSettings);
                else
                    ApplyDefaultSettings(autoSave: true);

                _settingsLoaded = true;
            }
        }

        public void Save() => Save(SelectedRvGatewayConnection, SelectedBrakingSystemGatewayConnection, SensorConnectionsAll);

        private void Save(IRvGatewayConnection rvDirectConnection, IRvGatewayConnection absDirectConnection) => Save(rvDirectConnection, absDirectConnection, SensorConnectionsAll);

        private void Save(IRvGatewayConnection rvDirectConnection, IRvGatewayConnection absGatewayCanConnection, IEnumerable<ISensorConnection>? sensorConnections)
        {
            if (_settingsLoaded is false)
            {
                Debug.Assert(false, "AppSettings are being overwritten as they have not been loaded");
                TaggedLog.Error(LogTag, "AppSettings are being overwritten as they have not been loaded");
            }

            var appSettingsSerializable = new AppSettingsSerializable(sensorConnections?.ToList(), MacHistory, UseKeySeedExchange, GenerateNotifications);

            TaggedLog.Information(LogTag, $"Saving Settings");

            if (!AppSettingsSerializable.TrySave(appSettingsSerializable))
                TaggedLog.Warning(LogTag, $"Unable to save settings");

            AppSettingsSavedMessage.SendMessage(); // We tried to save the settings, let others know so they can update if needed
        }

        private void ApplySettings(AppSettingsSerializable appSettings) // No autoSave needed as we just loaded the settings!
        {
            // Setting to none because we are only doing LoCAP devices to start
            SetSelectedRvGatewayConnection(DefaultRvDirectConnectionNone, saveSelectedRv: false, notifyChanged: false);
            SetSelectedBrakingSystemGatewayConnection(DefaultRvDirectConnectionNone, saveSelectedAbs: false, notifyChanged: false);

            SetSensorConnections(appSettings.SensorConnections, autoSave: false, notifyChanged: false);

            SetUseKeySeedExchange(appSettings.UseKeySeedExchange);
            SetGenerateNotifications(appSettings.GenerateNotifications);

            SetMacHistory(appSettings.MacHistory);

            TaggedLog.Debug(LogTag, "Loaded app settings");

            AppSelectedRvUpdateMessage.SendMessage();       // Lets others know the configuration changed
        }

        private void ApplyDefaultSettings(bool autoSave = true)
        {
            SetSelectedRvGatewayConnection(DefaultRvDirectConnectionNone, saveSelectedRv: false, notifyChanged: false);
            SetSelectedBrakingSystemGatewayConnection(DefaultRvDirectConnectionNone, saveSelectedAbs: false, notifyChanged: false);
            SetSensorConnections(new List<ISensorConnection>(), autoSave: false, notifyChanged: false);
            SetMacHistory(new List<string>());

            if (autoSave)
                Save();

            AppSelectedRvUpdateMessage.SendMessage();       // Lets others know the configuration changed
        }
    }
}
