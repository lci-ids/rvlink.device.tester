using PrismExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RvLinkDeviceTester.UserInterface.AddAndManageDevices.PushToPair
{
    [RegisterPageForNavigation(typeof(PushToPairPageViewModel))]
    public partial class PushToPairPage : ContentPage
    {
        public PushToPairPage()
        {
            InitializeComponent();
        }
    }
}