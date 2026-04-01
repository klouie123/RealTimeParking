using System.Collections.Specialized;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Maps;
using RealTimeParkingApp.Services;
using RealTimeParkingApp.ViewModels;

namespace RealTimeParkingApp.Views;

public partial class AdminDashboardPage : ContentPage
{

    public AdminDashboardPage()
    {
        InitializeComponent();

        var username = Preferences.Get("username", "User");
        UsernameLabel.Text = $"Logged in as: {username}";
        FlyoutUsernameLabel.Text = $"Username: {username}";
    }

    private async void HamburgerButton_Clicked(object sender, EventArgs e)
    {
        if (FlyoutPanel.IsVisible)
        {
            // Hide flyout
            await FlyoutPanel.TranslateTo(250, 0, 200);
            FlyoutPanel.IsVisible = false;
        }
        else
        {
            // Show flyout
            FlyoutPanel.IsVisible = true;
            await FlyoutPanel.TranslateTo(0, 0, 200);
        }
    }

    private void LogoutButton_Clicked(object sender, EventArgs e)
    {
        var apiService = App.Services.GetRequiredService<ApiService>();
        apiService.Logout();

        Preferences.Remove("jwt_token");
        Preferences.Remove("user_role");
        Preferences.Remove("username");
        Preferences.Remove("user_id");
        Preferences.Remove("email");
         
        Application.Current!.MainPage =
            new NavigationPage(App.Services.GetRequiredService<LoginPage>());
    }

    private async void ManageUsers_Clicked(object sender, EventArgs e)
    {
        // Temporary action - replace with navigation to your ManageUsers page
        await DisplayAlert("Manage Users", "Manage Users clicked", "OK");
    }
}