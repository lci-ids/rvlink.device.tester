using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using System;

namespace RvLinkDeviceTester.Connections.Sensors
{
    public interface ISensorConnection : IComparable, IJsonSerializable, IDirectConnectionSerializable
    {
    }

    public interface ISensorConnectionBle : ISensorConnection, IEndPointConnectionBle
    {
    }
}