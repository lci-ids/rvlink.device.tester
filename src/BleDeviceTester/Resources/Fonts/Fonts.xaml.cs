using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RvLinkDeviceTester.Resources.Fonts
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Fonts : ResourceDictionary
    {
        public Fonts() => InitializeComponent();

        // This just allows the OCM font to be referenced from C#.
        public static readonly string OCM = "ocm";
    }
}