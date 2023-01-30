using PrismExtensions;
using Xamarin.Forms;

namespace RvLinkDeviceTester.UserInterface.AddAndManageDevices
{
    [RegisterPageForNavigation(typeof(AddAndManageDevicesPageViewModel))]
    public partial class AddAndManageDevicesPage : ContentPage
    {
        public AddAndManageDevicesPage()
        {
            InitializeComponent();
        }
    }
}