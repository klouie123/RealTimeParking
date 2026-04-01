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

            MainPage = new NavigationPage(Services.GetRequiredService<StartupPage>());
        }
    }
}