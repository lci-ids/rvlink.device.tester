using PrismExtensions;
using Xamarin.Forms;

namespace RvLinkDeviceTester.UserInterface.Second
{
    [RegisterPageForNavigation(typeof(SecondPageViewModel))]
    public partial class SecondPage : ContentPage
    {
        public SecondPage() => InitializeComponent();
    }
}
