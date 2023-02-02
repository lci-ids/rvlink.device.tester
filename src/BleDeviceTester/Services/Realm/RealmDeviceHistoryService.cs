using System;
using System.Linq;
using RvLinkDeviceTester.Model;
using IDS.Portable.Common;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;

namespace RvLinkDeviceTester.Services.Realm
{
	public interface IRealmDeviceHistoryService
    {
		DeviceHistory? GetDeviceHistory(Guid bleDeviceID);
		void ResetDeviceHistory(Guid bleDeviceId);
		void AddMessage(Guid bleDeviceId, IdsCanAccessoryMessageType messageType, byte[] rawMessage);
    }

	public class RealmDeviceHistoryService : Singleton<RealmDeviceHistoryService>, IRealmDeviceHistoryService
	{
		private readonly string LogTag = nameof(RealmDeviceHistoryService);
		private readonly int MessageHistory = 100;

		private Realms.Realm _realm => Realms.Realm.GetInstance();
		private IQueryable _deviceInfo => _realm.All<DeviceHistory>();

        private RealmDeviceHistoryService()
        {

        }

		public DeviceHistory? GetDeviceHistory(Guid bleDeviceId) => _realm.Find<DeviceHistory>(bleDeviceId);

		public void ResetDeviceHistory(Guid bleDeviceId)
        {
			if (GetDeviceHistory(bleDeviceId) is var removeItem && removeItem != null)
			{
				_realm.WriteAsync(() =>
				{
					_realm.Remove(removeItem);
				});
			}
        }

		public void AddMessage(Guid bleDeviceId, IdsCanAccessoryMessageType messageType, byte[] rawMessage)
		{
			DateTimeOffset timeStamp = DateTimeOffset.UtcNow;
			var newMessage = new DeviceMessage()
			{
				TimeStamp = timeStamp,
				MessageType = messageType,
				RawMesssage = rawMessage,
			};

			if (GetDeviceHistory(bleDeviceId) is var editItem)
			{
                if (editItem is null)
				{
					var deviceHistory = new DeviceHistory()
					{
						BleDeviceId = bleDeviceId,
						TotalMessagesReceived = 1,
						ShortestTimeBetweenMessagesInMilli = 0,
						LongestTimeBetweenMessagesInMilli = 0,
					};
					deviceHistory.Messages.Add(newMessage);

					_realm.WriteAsync(() =>
					{
						_realm.Add(deviceHistory);
					});
                }
                else
                {
                    using var db = _realm.BeginWrite();
                    editItem.TotalMessagesReceived++;

                    var timeBetweenInMilli = (timeStamp - editItem.Messages.Last().TimeStamp).Milliseconds;

                    if (editItem.ShortestTimeBetweenMessagesInMilli == 0 || editItem.ShortestTimeBetweenMessagesInMilli > timeBetweenInMilli)
                        editItem.ShortestTimeBetweenMessagesInMilli = timeBetweenInMilli;

                    if (editItem.LongestTimeBetweenMessagesInMilli == 0 || editItem.LongestTimeBetweenMessagesInMilli < timeBetweenInMilli)
                        editItem.LongestTimeBetweenMessagesInMilli = timeBetweenInMilli;

                    editItem.Messages.Add(newMessage);

                    if (editItem.Messages.Count > MessageHistory)
                    {
                        editItem.Messages.RemoveAt(0);
                    }

                    db.Commit();
                }

                TaggedLog.Debug(LogTag, $"{bleDeviceId} Total Messages Received = {editItem?.TotalMessagesReceived}");
                TaggedLog.Debug(LogTag, $"{bleDeviceId} Shortest Time Between Messages In Milli = {editItem?.ShortestTimeBetweenMessagesInMilli}");
                TaggedLog.Debug(LogTag, $"{bleDeviceId} Longest Time Between Messages In Milli = {editItem?.LongestTimeBetweenMessagesInMilli}");
                TaggedLog.Debug(LogTag, $"{bleDeviceId} Message Type = {messageType.ToString()}");
                TaggedLog.Debug(LogTag, $"{bleDeviceId} Raw Message = {rawMessage.ToString()}");
			}

		}
    }
}