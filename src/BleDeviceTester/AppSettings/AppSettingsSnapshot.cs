#nullable enable
using RvLinkDeviceTester.Services;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using System.Collections.Generic;

namespace RvLinkDeviceTester
{
    public interface IAppSettingsSnapshot
    {
        LogicalDeviceSnapshot DeviceSnapshot { get; }
        void TakeSnapshot();
        void SetDeviceSnapshot(LogicalDeviceSnapshot rvSnapshot, bool autoSave = true);
    }

    public partial class AppSettings : IAppSettingsSnapshot
    {
        public LogicalDeviceSnapshot DeviceSnapshot { get; private set; }

        public void TakeSnapshot()
        {
            var logicalDeviceService = Resolver<ILogicalDeviceService>.Resolve;
            var snapshot = logicalDeviceService.DeviceManager?.TakeSnapshot(AppDirectServices.SnapshotFilter);
            if (snapshot == null)
            {
                TaggedLog.Warning(LogTag, $"Snapshot unable to be generated!");
                return;
            }
            AppSettings.Instance.SetDeviceSnapshot(snapshot, autoSave: true);
        }

        public void SetDeviceSnapshot(LogicalDeviceSnapshot? deviceSnapshot, bool autoSave = true)
        {
            deviceSnapshot ??= new LogicalDeviceSnapshot(new List<ILogicalDevice>());

            if( deviceSnapshot.HasSameDevices(DeviceSnapshot, withSameStatusData: false) )
                return;

            DeviceSnapshot = deviceSnapshot;
            if( autoSave )
            {
                TaggedLog.Debug(LogTag, $"Saved RV Snapshot");
                var deviceSnapshotSerializable = new AppSettingsDeviceSnapshotSerializable(DeviceSnapshot);
                AppSettingsDeviceSnapshotSerializable.TrySave(deviceSnapshotSerializable);
            }
        }
    }
}