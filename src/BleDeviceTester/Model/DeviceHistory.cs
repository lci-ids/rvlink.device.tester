using System;
using System.Collections.Generic;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;
using Realms;

namespace RvLinkDeviceTester.Model
{
	public class DeviceHistory : RealmObject
	{
		[PrimaryKey]
		public Guid BleDeviceId { get; set; }
		public int TotalMessagesReceived { get; set; }
		public int ShortestTimeBetweenMessagesInMilli { get; set; }
		public int LongestTimeBetweenMessagesInMilli { get; set; }
		// We will try to keep the DB manageable by keeping this list down to 100 messages at a time
		public IList<DeviceMessage> Messages { get; }
	}

	public class DeviceMessage : EmbeddedObject
	{
		public DateTimeOffset TimeStamp { get; set; }

		private int _messageType;
		public IdsCanAccessoryMessageType MessageType
		{
			get { return (IdsCanAccessoryMessageType)_messageType; }
			set { _messageType = (int)value; }
		}

		public byte[] RawMesssage { get; set; }
	}
}