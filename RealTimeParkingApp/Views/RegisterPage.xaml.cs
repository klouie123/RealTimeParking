using RealTimeParkingApp.Models;
using RealTimeParkingApp.Services;
using RealTimeParkingApp.Shells;

namespace RealTimeParkingApp.Views;

public partial class RegisterPage : ContentPage
{
    private readonly ApiService _apiService = new ApiService();

    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void RegisterButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(UsernameEntry.Text) ||
                string.IsNullOrWhiteSpace(FirstNameEntry.Text) ||
                string.IsNullOrWhiteSpace(LastNameEntry.Text) ||
                string.IsNullOrWhiteSpace(EmailEntry.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                await DisplayAlertAsync("Error", "Please fill in all required fields.", "OK");
                return;
            }

            // Create user object
            var user = new User
            {
                Username = UsernameEntry.Text.Trim(),
                FirstName = FirstNameEntry.Text.Trim(),
                MiddleName = MiddleNameEntry.Text?.Trim(), // optional
                LastName = LastNameEntry.Text.Trim(),
                Email = EmailEntry.Text.Trim(),
                PasswordHash = PasswordEntry.Text, // real password, backend will hash
                Role = "User" // default role
            };

            // Call API to register (returns true/false or throws error for duplicates)
            var success = await _apiService.Register(user);

            if (!success)
            {
                await DisplayAlertAsync("Error", "Username or Email already exists.", "OK");
                return;
            }

            // Automatic login after registration
            // Automatic login after registration
            var loginResponse = await _apiService.Login(user.Username, user.PasswordHash);

            if (loginResponse != null && loginResponse.IsSuccess)
            {
                Preferences.Set("jwt_token", loginResponse.Token);
                Preferences.Set("user_role", loginResponse.Role);

                await DisplayAlertAsync("Success", "Account created and logged in!", "OK");

                // Always User
                Application.Current.MainPage = new UserShell();
            }
            else
            {
                await DisplayAlertAsync("Info", "Account created, but automatic login failed. Please log in manually.", "OK");
                await Navigation.PopAsync();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }
}