using Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(ContentPage), typeof(RvLinkDeviceTester.iOS.Renderer.PageRenderer))]

namespace RvLinkDeviceTester.iOS.Renderer
{
    /// <summary>
    /// Until we decide whether we need the RotationPage from ids.ui, we'll use this basic renderer to set the status bar color.
    /// </summary>
    public class PageRenderer : Xamarin.Forms.Platform.iOS.PageRenderer
    {
        protected override void OnElementChanged(Xamarin.Forms.Platform.iOS.VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null || Element == null)
            {
                return;
            }

            try
            {
                UpdateStatusBarColor();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            }
        }

        private void UpdateStatusBarColor()
        {
            var bar = GetStatusBar();
            bar.BackgroundColor = UIColor.Black;
        }

        private UIView GetStatusBar()
        {
            UIView statusBar;
            if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            {
                int tag = 2022;
                UIWindow window = UIApplication.SharedApplication.Windows.FirstOrDefault();
                statusBar = window.ViewWithTag(tag);
                if (statusBar == null)
                {
                    statusBar = new UIView(UIApplication.SharedApplication.StatusBarFrame);
                    statusBar.Tag = tag;
                    statusBar.BackgroundColor = UIColor.Black; // Customize the view

                    window.AddSubview(statusBar);
                }
            }
            else
            {
                statusBar = UIApplication.SharedApplication.ValueForKey(new NSString("statusBar")) as UIView;
            }
            return statusBar;
        }
    }
}