using RvLinkDeviceTester.Extensions;
using Xamarin.Forms;

namespace RvLinkDeviceTester
{
    public class AppSettingsSavedMessage : MessagingCenterMessage<AppSettingsSavedMessage>
    {
        public static AppSettingsSavedMessage DefaultMessage { get; } = new AppSettingsSavedMessage();
        public static void SendMessage() => MessagingCenter.Instance.Send(DefaultMessage);
    }
}
