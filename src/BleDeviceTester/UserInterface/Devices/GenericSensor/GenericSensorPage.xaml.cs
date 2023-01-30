using IDS.Portable.LogicalDevice;
using PrismExtensions;
using Xamarin.Forms;


namespace RvLinkDeviceTester.UserInterface.Devices
{
    [RegisterPageForNavigation(typeof(GenericSensorPageViewModel))]
    public partial class GenericSensorPage : ContentPage
    {
        public GenericSensorPage()
        {
            InitializeComponent();
        }
    }
}