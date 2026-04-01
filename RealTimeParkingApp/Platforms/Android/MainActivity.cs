using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace RealTimeParkingApp;

[Activity(Theme = "@style/Maui.SplashTheme",
          MainLauncher = true,
          ConfigurationChanges = ConfigChanges.ScreenSize |
                                 ConfigChanges.Orientation |
                                 ConfigChanges.UiMode |
                                 ConfigChanges.ScreenLayout |
                                 ConfigChanges.SmallestScreenSize |
                                 ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        UpdateSystemBars(App.Current?.RequestedTheme ?? AppTheme.Light);
    }

    public void UpdateSystemBars(AppTheme theme)
    {
        if (Window == null)
            return;

        if (theme == AppTheme.Dark)
        {
            Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#0F172A"));
            Window.SetNavigationBarColor(Android.Graphics.Color.ParseColor("#0F172A"));

            var controller = WindowCompat.GetInsetsController(Window, Window.DecorView);
            if (controller != null)
            {
                controller.AppearanceLightStatusBars = false;
                controller.AppearanceLightNavigationBars = false;
            }
        }
        else
        {
            Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#F8F9FB"));
            Window.SetNavigationBarColor(Android.Graphics.Color.ParseColor("#F8F9FB"));

            var controller = WindowCompat.GetInsetsController(Window, Window.DecorView);
            if (controller != null)
            {
                controller.AppearanceLightStatusBars = true;
                controller.AppearanceLightNavigationBars = true;
            }
        }
    }
}