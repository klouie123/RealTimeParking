using RealTimeParkingApp.Services;

namespace RealTimeParkingApp.Views;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        UsernameValueLabel.Text = Preferences.Get("username", "User");
        EmailValueLabel.Text = Preferences.Get("email", "No email");
        RoleValueLabel.Text = Preferences.Get("user_role", "User");
    }

    private async void ParkingHistoryButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ParkingHistoryPage));
    }

    private async void SettingsButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

    private async void LogoutButton_Clicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");

        if (!confirm)
            return;

        var apiService = App.Services.GetRequiredService<ApiService>();
        apiService.Logout();

        Preferences.Remove("jwt_token");
        Preferences.Remove("user_role");
        Preferences.Remove("user_id");
        Preferences.Remove("username");
        Preferences.Remove("email");
        Preferences.Remove("parking_location_id");
        Preferences.Remove("parking_location_name");

        Application.Current!.MainPage =
            new NavigationPage(App.Services.GetRequiredService<LoginPage>());
    }
}