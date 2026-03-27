using System.Net.Http.Headers;
using Newtonsoft.Json;
using RealTimeParkingApp.Models;
using RealTimeParkingApp.Services;
using RealTimeParkingApp.Shells;

namespace RealTimeParkingApp.Views;

public partial class LoginPage : ContentPage
{
    private ApiService _apiService = new ApiService();
    public LoginPage()
	{
        InitializeComponent();
    }

    private async void LoginButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            // Call Login and get the response object
            var loginResponse = await _apiService.Login(UsernameOrEmailEntry.Text, PasswordEntry.Text);

            // Check if the login response is valid and has a token
            if (loginResponse != null && loginResponse.IsSuccess)
            {
                // Optionally save token and role locally
                Preferences.Set("jwt_token", loginResponse.Token);
                Preferences.Set("user_role", loginResponse.Role);

                await DisplayAlertAsync("Success", "Logged in!", "OK");

                //Page dashboardPage;
                if (loginResponse.Role == "Admin")
                {
                    Application.Current.MainPage = new AdminShell();
                }
                else
                {
                    Application.Current.MainPage = new UserShell();
                }

                // Make the dashboard page the new root, preventing back navigation to login
                //Application.Current.MainPage = new NavigationPage(dashboardPage);
            }
            else
            {
                await DisplayAlertAsync("Error", "Invalid login", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Crash Error", ex.Message, "OK");
        }
    }

    private async void RegisterButton_Clicked(object sender, EventArgs e)
    {
        // Navigate to the registration page
        await Navigation.PushAsync(new RegisterPage());
    }
}