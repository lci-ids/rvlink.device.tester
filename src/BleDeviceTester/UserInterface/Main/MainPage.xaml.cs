using PrismExtensions;
using Xamarin.Forms;

namespace RvLinkDeviceTester.UserInterface.Main
{
    [RegisterPageForNavigation(typeof(MainPageViewModel))]
    public partial class MainPage : ContentPage
    {
        public MainPage() => InitializeComponent();
    }
}
