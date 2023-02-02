using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(ContentPage), typeof(RvLinkDeviceTester.Droid.Renderers.PageRenderer))]

namespace RvLinkDeviceTester.Droid.Renderers
{
    /// <summary>
    /// Until we decide whether we need the RotationPage from ids.ui, we'll use this basic renderer to set the status bar color.
    /// </summary>
    public class PageRenderer: Xamarin.Forms.Platform.Android.PageRenderer
    {
        public PageRenderer(Context context) : base(context) { }

        protected override void OnElementChanged(ElementChangedEventArgs<Page> e)
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
            var activity = Context.GetActivity();
            var window = activity.Window;
            window?.SetStatusBarColor(Color.Black.ToAndroid());
            window?.SetNavigationBarColor(Color.Black.ToAndroid());
        }
    }
}