using RealTimeParkingApp.Services;
using RealTimeParkingApp.Shells;

namespace RealTimeParkingApp.Views;

public partial class EmailConfirmationPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly string _email;
    private readonly string _password;

    public EmailConfirmationPage(string email, string password)
    {
        InitializeComponent();
        _apiService = App.Services.GetRequiredService<ApiService>();
        _email = email;
        _password = password;

        EmailLabel.Text = $"We sent a code to {_email}";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await SendCodeAsync();
    }

    private async Task SendCodeAsync()
    {
        var result = await _apiService.SendConfirmationCodeAsync(_email);

        if (!result.Success)
        {
            await DisplayAlert("Error", result.Message, "OK");
            return;
        }

        if (!string.IsNullOrWhiteSpace(result.DebugCode))
        {
            await DisplayAlert("Dev Code", $"Verification code: {result.DebugCode}", "OK");
        }
    }

    private async void VerifyButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(CodeEntry.Text))
            {
                await DisplayAlert("Error", "Please enter the confirmation code.", "OK");
                return;
            }

            var verifyResult = await _apiService.VerifyEmailCodeAsync(_email, CodeEntry.Text.Trim());

            if (!verifyResult.Success)
            {
                await DisplayAlert("Error", verifyResult.Message, "OK");
                return;
            }

            var loginResult = await _apiService.Login(_email, _password);

            if (loginResult == null || !loginResult.IsSuccess)
            {
                await DisplayAlert("Error", "Email confirmed, but login failed.", "OK");
                await Navigation.PopToRootAsync();
                return;
            }

            Preferences.Set("jwt_token", loginResult.Token ?? string.Empty);
            Preferences.Set("user_role", loginResult.Role ?? string.Empty);
            Preferences.Set("user_id", loginResult.UserId);
            Preferences.Set("username", loginResult.Username ?? string.Empty);
            Preferences.Set("email", loginResult.Email ?? string.Empty);

            await DisplayAlert("Success", "Email confirmed successfully.", "OK");

            switch (loginResult.Role)
            {
                case "SuperAdmin":
                    Application.Current!.MainPage =
                        App.Services.GetRequiredService<SuperAdminShell>();
                    break;

                case "LocationAdmin":
                    Application.Current!.MainPage =
                        App.Services.GetRequiredService<LocationAdminShell>();
                    break;

                case "User":
                default:
                    Application.Current!.MainPage =
                        App.Services.GetRequiredService<UserShell>();
                    break;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void ResendButton_Clicked(object sender, EventArgs e)
    {
        await SendCodeAsync();
    }
}