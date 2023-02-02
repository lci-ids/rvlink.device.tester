using PrismExtensions;
using Xamarin.Forms;

namespace RvLinkDeviceTester.UserInterface.Diagnostics
{
    [RegisterPageForNavigation(typeof(MyRvLinkDiagnosticsPageViewModel))]
    public partial class MyRvLinkDiagnosticsPage : ContentPage
    {
        public MyRvLinkDiagnosticsPage()
        {
            InitializeComponent();
        }
    }
}