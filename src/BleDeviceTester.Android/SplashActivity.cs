using Android.App;
using Android.Content;
using Android.Content.PM;

namespace RvLinkDeviceTester.Droid
{
    [Activity(
        Label = "RvLinkDeviceTester",
        Name = "RvLinkDeviceTester.android." + nameof(SplashActivity),
        MainLauncher = true,
        ScreenOrientation = ScreenOrientation.Locked,
        Theme = "@style/MainTheme.Splash",
        NoHistory = true
    )]
    public class SplashActivity : Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnResume()
        {
            base.OnResume();
            StartActivity(new Intent(Application.Context, typeof(MainActivity)));
        }

        public override void OnBackPressed() { }
    }
}