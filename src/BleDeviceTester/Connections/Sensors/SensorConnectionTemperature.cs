using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;
using System;
using System.Reflection;

namespace RvLinkDeviceTester.Connections.Sensors
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SensorConnectionTemperature : SensorConnectionBleBase<SensorConnectionTemperature>
    {
        [JsonProperty]
        public override string ConnectionNameFriendly { get; }

        [JsonProperty]
        [JsonConverter(typeof(MacJsonHexStringConverter))]
        public MAC AccessoryMac { get; }

        [JsonProperty]
        public string SoftwarePartNumber { get; }

        [JsonConstructor]
        public SensorConnectionTemperature(string connectionNameFriendly, Guid connectionGuid, MAC accessoryMac, string softwarePartNumber) : base(connectionGuid)
        {
            ConnectionNameFriendly = connectionNameFriendly;
            AccessoryMac = accessoryMac;
            SoftwarePartNumber = softwarePartNumber;
        }

        // This is used to register/associate a short name with this type.  It's primary purpose is for JSON serialization/de-serialzation
        // so we can use indirect mappings as opposed to fully qualified names.  It will attempt to auto register, but should also be
        // registered in advance.
        //
        static SensorConnectionTemperature()  // static initializer
        {
            Type serializerType = MethodBase.GetCurrentMethod().DeclaringType;
            TypeRegistry.Register(serializerType.Name, serializerType);
        }
    }
}
