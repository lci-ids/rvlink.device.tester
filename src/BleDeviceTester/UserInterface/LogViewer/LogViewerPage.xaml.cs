using PrismExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RvLinkDeviceTester.UserInterface.LogViewer
{
    [RegisterPageForNavigation(typeof(LogViewerPageViewModel))]
    public partial class LogViewerPage : ContentPage
    {
        public LogViewerPage() => InitializeComponent();
    }
}