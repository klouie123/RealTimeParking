using RealTimeParkingApp.Services;

namespace RealTimeParkingApp.Views;

public partial class SuperAdminDashboardPage : ContentPage
{
    private readonly ApiService _apiService;

    public SuperAdminDashboardPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;

        var username = Preferences.Get("username", "User");
        UsernameLabel.Text = $"Logged in as: {username}";
    }

    private void LogoutButton_Clicked(object sender, EventArgs e)
    {
        _apiService.Logout();

        Preferences.Clear();

        Application.Current!.MainPage =
            new NavigationPage(App.Services.GetRequiredService<LoginPage>());
    }

    private async void ManageUsers_Clicked(object sender, EventArgs e)
    {
        await DisplayAlert("Manage Users", "Soon...", "OK");
    }

    private async void ManageLocations_Clicked(object sender, EventArgs e)
    {
        await DisplayAlert("Manage Locations", "Soon...", "OK");
    }

    // MAIN FEATURE: CREATE LOCATION ADMIN
    private async void CreateLocationAdmin_Clicked(object sender, EventArgs e)
    {
        try
        {
            string username = await DisplayPromptAsync("Create Admin", "Username:");
            if (string.IsNullOrWhiteSpace(username)) return;

            string firstName = await DisplayPromptAsync("Create Admin", "First Name:");
            if (string.IsNullOrWhiteSpace(firstName)) return;

            string lastName = await DisplayPromptAsync("Create Admin", "Last Name:");
            if (string.IsNullOrWhiteSpace(lastName)) return;

            string email = await DisplayPromptAsync("Create Admin", "Email:");
            if (string.IsNullOrWhiteSpace(email)) return;

            string password = await DisplayPromptAsync("Create Admin", "Password:");
            if (string.IsNullOrWhiteSpace(password)) return;

            string locationIdStr = await DisplayPromptAsync("Create Admin", "Parking Location ID:");
            if (!int.TryParse(locationIdStr, out int locationId)) return;

            var success = await _apiService.CreateLocationAdminAsync(
                username,
                firstName,
                lastName,
                email,
                password,
                locationId
            );

            if (success)
            {
                await DisplayAlert("Success", "Location Admin created!", "OK");
            }
            else
            {
                await DisplayAlert("Error", "Failed to create admin", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}