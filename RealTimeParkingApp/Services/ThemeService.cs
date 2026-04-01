using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace RealTimeParkingApp.Services
{
    public static class ThemeService
    {
        private const string ThemePreferenceKey = "AppThemeMode";

        public static string GetSavedTheme()
        {
            return Preferences.Get(ThemePreferenceKey, "System");
        }

        public static void ApplyTheme(string themeMode)
        {
            Preferences.Set(ThemePreferenceKey, themeMode);

            AppTheme theme = themeMode switch
            {
                "Light" => AppTheme.Light,
                "Dark" => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };

            if (Application.Current != null)
                Application.Current.UserAppTheme = theme;

#if ANDROID
            var activity = Platform.CurrentActivity as MainActivity;
            if (activity != null)
            {
                var actualTheme = theme == AppTheme.Unspecified
                    ? Application.Current?.RequestedTheme ?? AppTheme.Light
                    : theme;

                //activity.UpdateSystemBars(actualTheme);
            }
#endif
        }
    }
}