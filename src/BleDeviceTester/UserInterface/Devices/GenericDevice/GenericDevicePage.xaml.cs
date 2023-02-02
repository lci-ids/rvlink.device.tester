using IDS.Portable.LogicalDevice;
using PrismExtensions;
using Xamarin.Forms;


namespace RvLinkDeviceTester.UserInterface.Devices
{
    [RegisterPageForNavigation(typeof(GenericDevicePageViewModel))]
    public partial class GenericDevicePage : ContentPage
    {
        public GenericDevicePage()
        {
            InitializeComponent();
        }
    }
}