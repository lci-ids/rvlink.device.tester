using System;
using IDS.Portable.Common;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;

namespace RvLinkDeviceTester.Services.Realm
{
	public class HistoryTracker : IIdsCanAccessoryScanResultHistoryTracker
    {
        public void TrackRawMessage(Guid deviceId, IdsCanAccessoryMessageType messageType, byte[] rawMessage)
        {
            var realmService = RealmDeviceHistoryService.Instance;
            realmService.AddMessage(deviceId, messageType, rawMessage);
        }
    }
}

