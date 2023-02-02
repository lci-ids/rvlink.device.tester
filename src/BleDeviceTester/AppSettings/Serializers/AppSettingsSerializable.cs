using System;
using System.Collections.Generic;
using RvLinkDeviceTester.Connections.Sensors;
using ids.portable.common.Extensions;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace RvLinkDeviceTester
{
    [JsonObject(MemberSerialization.OptIn)]
    public class AppSettingsSerializable : JsonSerializable<AppSettingsSerializable>
    {
        public const string LogTag = nameof(AppSettingsSerializable);
        public const string AppSettingsFilename = "RvLinkDeviceTesterSettings.json";

        [JsonProperty(PropertyName = "SensorConnections", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(JsonSerializerInterfaceListConverter<ISensorConnection>))]
        private List<ISensorConnection> _sensorConnections;

        [JsonIgnore]  // Synthesized from _sensorConnections
        public IReadOnlyList<ISensorConnection> SensorConnections => _sensorConnections;

        [JsonProperty]
        public bool UseKeySeedExchange { get; }

        [JsonProperty]
        public bool GenerateNotifications { get; }

        [JsonProperty]
        public List<string> MacHistory { get; }

        [JsonConstructor]
        public AppSettingsSerializable(List<ISensorConnection>? sensorConnections, List<string> macHistory, bool useKeySeedExchange, bool generateNotifications)
        {
            _sensorConnections = new List<ISensorConnection>(sensorConnections ?? new List<ISensorConnection>());
            UseKeySeedExchange = useKeySeedExchange;
            MacHistory = macHistory;
            GenerateNotifications = generateNotifications;
        }

        public static bool TrySave(AppSettingsSerializable appSettings)
        {
            try
            {
                var json = JsonConvert.SerializeObject(appSettings, Formatting.Indented);
                AppSettingsFilename.SaveText(json);
                return true;

            }
            catch (Exception ex)
            {
                TaggedLog.Error(LogTag, $"Unable to save given appSettings {appSettings}: {ex.Message}");
                return false;
            }
        }

        public static bool TryLoad(out AppSettingsSerializable? appSettings)
        {
            try
            {
                appSettings = null;
                var json = AppSettingsFilename.LoadText();
                if (string.IsNullOrWhiteSpace(json))
                    throw new Exception("json is null or empty");

                appSettings = JsonConvert.DeserializeObject<AppSettingsSerializable>(json);
            }
            catch (Exception ex)
            {
                TaggedLog.Warning(LogTag, $"Unable to load AppSettings: {ex.Message}");
                appSettings = null;
            }

            return appSettings != null;
        }
    }
}
