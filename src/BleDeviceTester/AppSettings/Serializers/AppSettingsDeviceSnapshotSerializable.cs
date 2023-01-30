#nullable enable

using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.Portable.Platform.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using ids.portable.common.Extensions;

namespace RvLinkDeviceTester
{
    [JsonObject(MemberSerialization.OptIn)]
    public class AppSettingsDeviceSnapshotSerializable
    {
        public const string LogTag = nameof(AppSettingsDeviceSnapshotSerializable);
        public const string DeviceSnapshotFilename = "DeviceSnapshotV1.json";
        public static readonly Version SettingsVersion = new Version(major: 1, minor: 0);

        [JsonProperty]
        [JsonConverter(typeof(VersionConverter))]
        public Version AppSettingsVersion { get; }

        [JsonProperty]
        public LogicalDeviceSnapshot DeviceSnapshot { get; }

        [JsonConstructor]
        public AppSettingsDeviceSnapshotSerializable(LogicalDeviceSnapshot deviceSnapshot, Version? appSettingsVersion = null)
        {
            AppSettingsVersion = appSettingsVersion ?? SettingsVersion;
            DeviceSnapshot = deviceSnapshot;
        }

        public static bool TrySave(AppSettingsDeviceSnapshotSerializable deviceSnapshotSerializable)
        {
            try
            {
                var json = JsonConvert.SerializeObject(deviceSnapshotSerializable, Formatting.Indented);
                DeviceSnapshotFilename.SaveText(json);
                return true;
            }
            catch( Exception ex )
            {
                TaggedLog.Error(LogTag, $"Unable to save Snapshot {deviceSnapshotSerializable}: {ex.Message}");
                return false;
            }
        }

        public static bool TryLoad(out AppSettingsDeviceSnapshotSerializable? deviceSnapshotSerializable)
        {
            try
            {
                deviceSnapshotSerializable = null;
                var json = FileIoManager.LoadText(DeviceSnapshotFilename);
                if( string.IsNullOrWhiteSpace(json) )
                    throw new Exception("json is null or empty");

                deviceSnapshotSerializable = JsonConvert.DeserializeObject<AppSettingsDeviceSnapshotSerializable>(json);
            }
            catch( Exception ex )
            {
                TaggedLog.Warning(LogTag, $"Unable to load Snapshot: {ex.Message}");
                deviceSnapshotSerializable = null;
            }

            return deviceSnapshotSerializable != null;
        }
    }
}
