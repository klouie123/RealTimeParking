using RealTimeParkingApp.Services;
using RealTimeParkingApp.Shells;

namespace RealTimeParkingApp.Views;

public partial class StartupPage : ContentPage
{
    public StartupPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await Task.Delay(1500);

        try
        {
            var apiService = App.Services.GetRequiredService<ApiService>();
            apiService.RestoreTokenFromPreferences();

            var token = Preferences.Get("jwt_token", string.Empty);
            var role = Preferences.Get("user_role", string.Empty);

            if (!string.IsNullOrWhiteSpace(token))
            {
                if (role == "Admin")
                {
                    Application.Current!.MainPage =
                        App.Services.GetRequiredService<AdminShell>();
                }
                else
                {
                    Application.Current!.MainPage =
                        App.Services.GetRequiredService<UserShell>();
                }
            }
            else
            {
                Application.Current!.MainPage =
                    new NavigationPage(App.Services.GetRequiredService<LoginPage>());
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.ToString(), "OK");

            Application.Current!.MainPage =
                new NavigationPage(App.Services.GetRequiredService<LoginPage>());
        }
    }
}