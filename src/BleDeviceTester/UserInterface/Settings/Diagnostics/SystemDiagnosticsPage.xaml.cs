using System;
using System.Collections.Generic;
using PrismExtensions;
using Xamarin.Forms;

namespace RvLinkDeviceTester.UserInterface.Settings.Diagnostics
{
    [RegisterPageForNavigation(typeof(SystemDiagnosticsPageViewModel))]
    public partial class SystemDiagnosticsPage : ContentPage
    {
        public SystemDiagnosticsPage()
        {
            InitializeComponent();
        }
    }
}

