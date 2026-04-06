using RealTimeParkingApp.DTOs;
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

            var dto = new RegisterRequestDto
            {
                Username = UsernameEntry.Text.Trim(),
                FirstName = FirstNameEntry.Text.Trim(),
                MiddleName = MiddleNameEntry.Text?.Trim(),
                LastName = LastNameEntry.Text.Trim(),
                Email = EmailEntry.Text.Trim(),
                Password = PasswordEntry.Text
            };

            var result = await _apiService.RegisterAsync(dto);

            if (!result.Success)
            {
                await DisplayAlert("Error", result.Message, "OK");
                return;
            }

            await DisplayAlert("Success", "Account created. Please confirm your email first.", "OK");

            await Navigation.PushAsync(new EmailConfirmationPage(dto.Email, dto.Password));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}