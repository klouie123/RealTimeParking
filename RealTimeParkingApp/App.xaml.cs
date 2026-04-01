using Microsoft.Maui.ApplicationModel;
using RealTimeParkingApp.Services;
using RealTimeParkingApp.Views;

namespace RealTimeParkingApp
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; }

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            Services = serviceProvider;

            ThemeService.ApplyTheme(ThemeService.GetSavedTheme());

            if (Application.Current != null)
                Application.Current.RequestedThemeChanged += OnThemeChanged;

            MainPage = new NavigationPage(new StartupPage());
        }

        private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e)
        {
#if ANDROID
            var savedTheme = ThemeService.GetSavedTheme();

            if (savedTheme == "System")
            {
                var activity = Platform.CurrentActivity as MainActivity;
                activity?.UpdateSystemBars(e.RequestedTheme);
            }
#endif
        }
    }
}