using RealTimeParkingApp.Services;
using RealTimeParkingApp.Shells;

namespace RealTimeParkingApp.Views;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService;

    public LoginPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    private async void LoginButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            ErrorLabel.IsVisible = false;

            if (string.IsNullOrWhiteSpace(UsernameOrEmailEntry.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                ErrorLabel.Text = "Please enter username/email and password.";
                ErrorLabel.IsVisible = true;
                return;
            }

            LoginButton.IsEnabled = false;
            LoginLoadingIndicator.IsVisible = true;
            LoginLoadingIndicator.IsRunning = true;

            var loginResponse = await _apiService.Login(
                UsernameOrEmailEntry.Text.Trim(),
                PasswordEntry.Text
            );

            if (loginResponse != null && loginResponse.IsSuccess)
            {
                Preferences.Set("jwt_token", loginResponse.Token ?? string.Empty);
                Preferences.Set("user_role", loginResponse.Role ?? string.Empty);
                Preferences.Set("user_id", loginResponse.UserId);
                Preferences.Set("username", loginResponse.Username ?? "");
                Preferences.Set("email", loginResponse.Email ?? "");

                if (loginResponse.Role == "Admin")
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
                ErrorLabel.Text = "Invalid login.";
                ErrorLabel.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Crash Error", ex.ToString(), "OK");
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginLoadingIndicator.IsVisible = false;
            LoginLoadingIndicator.IsRunning = false;
        }
    }

    private async void RegisterButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(App.Services.GetRequiredService<RegisterPage>());
    }
}