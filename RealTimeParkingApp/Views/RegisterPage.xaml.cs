using RealTimeParkingApp.Models;
using RealTimeParkingApp.Services;
using RealTimeParkingApp.Shells;

namespace RealTimeParkingApp.Views;

public partial class RegisterPage : ContentPage
{
    private readonly ApiService _apiService;

    public RegisterPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    private async void RegisterButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(UsernameEntry.Text) ||
                string.IsNullOrWhiteSpace(FirstNameEntry.Text) ||
                string.IsNullOrWhiteSpace(LastNameEntry.Text) ||
                string.IsNullOrWhiteSpace(EmailEntry.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                await DisplayAlert("Error", "Please fill in all required fields.", "OK");
                return;
            }

            var user = new User
            {
                Username = UsernameEntry.Text.Trim(),
                FirstName = FirstNameEntry.Text.Trim(),
                MiddleName = MiddleNameEntry.Text?.Trim(),
                LastName = LastNameEntry.Text.Trim(),
                Email = EmailEntry.Text.Trim(),
                PasswordHash = PasswordEntry.Text,
                Role = "User"
            };

            var success = await _apiService.Register(user);

            if (!success)
            {
                await DisplayAlert("Error", "Username or Email already exists.", "OK");
                return;
            }

            var loginResponse = await _apiService.Login(user.Username, user.PasswordHash);

            if (loginResponse != null && loginResponse.IsSuccess)
            {
                Preferences.Set("jwt_token", loginResponse.Token ?? string.Empty);
                Preferences.Set("user_role", loginResponse.Role ?? string.Empty);
                Preferences.Set("user_id", loginResponse.UserId);
                Preferences.Set("username", loginResponse.Username ?? user.Username ?? "");
                Preferences.Set("email", loginResponse.Email ?? user.Email ?? "");

                await DisplayAlert("Success", "Account created and logged in!", "OK");

                Application.Current!.MainPage =
                    App.Services.GetRequiredService<UserShell>();
            }
            else
            {
                await DisplayAlert("Info", "Account created, but automatic login failed. Please log in manually.", "OK");
                await Navigation.PopAsync();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.ToString(), "OK");
        }
    }
}