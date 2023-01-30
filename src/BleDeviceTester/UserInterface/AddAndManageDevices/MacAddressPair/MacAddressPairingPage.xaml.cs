using PrismExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RvLinkDeviceTester.UserInterface.AddAndManageDevices.MacAddressPair
{
    [RegisterPageForNavigation(typeof(MacAddressPairingPageViewModel))]
    public partial class MacAddressPairingPage : ContentPage
    {
        public MacAddressPairingPage()
        {
            InitializeComponent();
        }
    }
}