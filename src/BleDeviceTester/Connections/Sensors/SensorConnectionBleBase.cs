using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;
using System;
using HashCode = IDS.Portable.Common.HashCode;

namespace RvLinkDeviceTester.Connections.Sensors
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class SensorConnectionBleBase<TSerializable> : JsonSerializable<TSerializable>, ISensorConnectionBle 
        where TSerializable : class
    {
        public const string LogTag = nameof(SensorConnectionBleBase<TSerializable>);

        [JsonProperty]
        public string SerializerClass => GetType().Name;        // Use short name because we did a TypeRegistry.Register

        [JsonProperty]
        public abstract string ConnectionNameFriendly { get; }

        [JsonIgnore]
        public string ConnectionId => ConnectionNameFriendly;

        [JsonProperty]
        public Guid ConnectionGuid { get; }

        protected SensorConnectionBleBase(Guid connectionGuid)
        {
            ConnectionGuid = connectionGuid;
        }

        public override string ToString() => $"'{ConnectionNameFriendly}'";
        
        #region Equals/Compare/Hash
        public override bool Equals(Object obj)
        {
            if( object.ReferenceEquals(this, obj) )
                return true;

            if( !(obj is SensorConnectionBleBase<TSerializable> other) )
                return false;

            return ConnectionGuid == other.ConnectionGuid && string.Equals(ConnectionId, other.ConnectionId);
        }

        public virtual int CompareTo(object obj)
        {
            if( Equals(obj) )
                return 0;

            if( !(obj is SensorConnectionBleBase<TSerializable> other) )
                return 1;

            return string.Compare(ConnectionId, other.ConnectionId, StringComparison.Ordinal);
        }

        public override int GetHashCode() => HashCode.Start.Hash(ConnectionGuid);
        #endregion
    }
}