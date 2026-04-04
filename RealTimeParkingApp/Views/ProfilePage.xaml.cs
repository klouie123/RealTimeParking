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

        var username = Preferences.Get("username", "User");
        var email = Preferences.Get("email", "No email");
        var role = Preferences.Get("user_role", "User");

        UsernameValueLabel.Text = username;
        EmailValueLabel.Text = email;
        RoleValueLabel.Text = role;

        ApplyRoleBasedActions(role);
    }

    private void ApplyRoleBasedActions(string role)
    {
        ParkingHistoryButton.IsVisible = false;
        TransactionHistoryButton.IsVisible = false;

        if (string.Equals(role, "User", StringComparison.OrdinalIgnoreCase))
        {
            ParkingHistoryButton.IsVisible = true;
        }
        else if (string.Equals(role, "LocationAdmin", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            TransactionHistoryButton.IsVisible = true;
        }
    }

    private async void ParkingHistoryButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ParkingHistoryPage));
    }

    private async void TransactionHistoryButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(LocationAdminHistoryPage));
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