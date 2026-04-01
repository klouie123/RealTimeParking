using RealTimeParkingApp.Services;

namespace RealTimeParkingApp.Views;

public partial class ProfilePage : ContentPage
{
    private bool _themeLoaded;

    public ProfilePage()
	{
		InitializeComponent();
        LoadThemeSelection();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        UsernameValueLabel.Text = Preferences.Get("username", "User");
        EmailValueLabel.Text = Preferences.Get("email", "No email");
        RoleValueLabel.Text = Preferences.Get("user_role", "User");
    }

    private void LoadThemeSelection()
    {
        var savedTheme = ThemeService.GetSavedTheme();

        ThemePicker.SelectedIndex = savedTheme switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0
        };

        _themeLoaded = true;
    }

    private void ThemePicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (!_themeLoaded || ThemePicker.SelectedIndex < 0)
            return;

        var selectedTheme = ThemePicker.SelectedIndex switch
        {
            1 => "Light",
            2 => "Dark",
            _ => "System"
        };

        ThemeService.ApplyTheme(selectedTheme);
    }

    private async void LogoutButton_Clicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");

        if (!confirm)
            return;

        Preferences.Remove("jwt_token");
        Preferences.Remove("user_role");
        Preferences.Remove("username");
        Preferences.Remove("email");
        Preferences.Remove("user_id");

        Application.Current.MainPage = new NavigationPage(new LoginPage());
    }
}