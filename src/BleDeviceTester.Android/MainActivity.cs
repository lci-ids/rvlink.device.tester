using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Android.Views;
using IDS.Portable.BLE.Platforms.Android;
using IDS.Portable.Common;

namespace RvLinkDeviceTester.Droid
{
    [Activity(Label = "RvLinkDeviceTester", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = false, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            MainThread.UpdateMainThreadContext();

            base.OnCreate(savedInstanceState);

            PrismExtensions.Android.Platform.Init(this, savedInstanceState, null);

            Xamarin.Forms.Forms.Init(this, savedInstanceState);

            Syncfusion.XForms.Android.Core.Core.Init(this);

            LoadApplication(new App(new PlatformInitializer()));

            PrismExtensions.Android.Platform.SetSupportActionBar(this);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
        
        public override void OnBackPressed()
        {
            if (PrismExtensions.Android.Platform.OnBackPressed())
                return;

            base.OnBackPressed();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (PrismExtensions.Android.Platform.OnOptionsItemSelected(item))
                return true;

            return base.OnOptionsItemSelected(item);
        }
    }
}
