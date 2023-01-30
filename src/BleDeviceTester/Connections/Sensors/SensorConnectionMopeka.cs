using IDS.Core.IDS_CAN;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;
using System;
using IDS.Portable.LogicalDevice;
using Newtonsoft.Json.Converters;
using OneControl.Devices.TankSensor.Mopeka;
using System.Reflection;
using IDS.Portable.Common;

namespace RvLinkDeviceTester.Connections.Sensors
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SensorConnectionMopeka : SensorConnectionBleBase<SensorConnectionMopeka>
    {
        [JsonProperty]
        public override string ConnectionNameFriendly { get; }

        [JsonProperty]
        [JsonConverter(typeof(MacJsonHexStringConverter))]
        public MAC MacAddress { get; }

        [JsonProperty]
        public FunctionName DefaultFunctionName { get; }

        [JsonProperty]
        public byte DefaultFunctionInstance { get; }

        [JsonProperty]
        public int DefaultTankSizeId { get; }

        [JsonProperty]
        public float DefaultTankHeightInMm { get; }

        [JsonProperty]
        public bool DefaultIsNotificationEnabled { get; }

        [JsonProperty]
        public int DefaultNotificationThreshold { get; }

        [JsonProperty]
        public float DefaultAccelXOffset { get; }

        [JsonProperty]
        public float DefaultAccelYOffset { get; }

        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public TankHeightUnits DefaultPreferredUnits { get; }

        [JsonConstructor]
        public SensorConnectionMopeka(string connectionNameFriendly, Guid connectionGuid, MAC macAddress, FunctionName defaultFunctionName, byte defaultFunctionInstance, int defaultTankSizeId, float defaultTankHeightInMm, bool defaultIsNotificationEnabled, int defaultNotificationThreshold, float defaultAccelXOffset = 0.0f, float defaultAccelYOffset = 0.0f, TankHeightUnits defaultPreferredUnits = TankHeightUnits.Centimeters) : base(connectionGuid)
        {
            ConnectionNameFriendly = connectionNameFriendly;
            MacAddress = macAddress;
            DefaultFunctionName = defaultFunctionName;
            DefaultFunctionInstance = defaultFunctionInstance;
            DefaultTankSizeId = defaultTankSizeId;
            DefaultTankHeightInMm = defaultTankHeightInMm;
            DefaultIsNotificationEnabled = defaultIsNotificationEnabled;
            DefaultNotificationThreshold = defaultNotificationThreshold;
            DefaultAccelXOffset = defaultAccelXOffset;
            DefaultAccelYOffset = defaultAccelXOffset;
            DefaultPreferredUnits = defaultPreferredUnits;
        }

        // This is used to register/associate a short name with this type.  It's primary purpose is for JSON serialization/de-serialzation
        // so we can use indirect mappings as opposed to fully qualified names.  It will attempt to auto register, but should also be
        // registered in advance.
        //
        static SensorConnectionMopeka()  // static initializer
        {
            Type serializerType = MethodBase.GetCurrentMethod().DeclaringType;
            TypeRegistry.Register(serializerType.Name, serializerType);
        }
    }
}
