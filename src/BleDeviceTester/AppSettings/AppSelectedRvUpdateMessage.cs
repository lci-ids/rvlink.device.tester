using RvLinkDeviceTester.Extensions;
using Xamarin.Forms;

namespace RvLinkDeviceTester
{
    public class AppSelectedRvUpdateMessage : MessagingCenterMessage<AppSelectedRvUpdateMessage>
    {
        public static AppSelectedRvUpdateMessage DefaultMessage { get; } = new AppSelectedRvUpdateMessage();
        public static void SendMessage() => MessagingCenter.Instance.Send(DefaultMessage);
    }
}



