using Microsoft.Extensions.DependencyInjection;
using RealTimeParkingApp.Shells;
using RealTimeParkingApp.Views;

namespace RealTimeParkingApp
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; }
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            CheckLogin();

            Services = serviceProvider;

            MainPage = new NavigationPage(new LoginPage());
        }

        private void CheckLogin()
        {
            var token = Preferences.Get("jwt_token", string.Empty);
            var role = Preferences.Get("user_role", string.Empty);

            if (!string.IsNullOrEmpty(token))
            {
                if (role == "Admin")
                {
                    MainPage = new AdminShell();
                }
                else
                {
                    MainPage = new UserShell();
                }
            }
            else
            {
                MainPage = new NavigationPage(new LoginPage());
            }
        }
    }

}