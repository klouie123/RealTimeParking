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

        UsernameValueLabel.Text = Preferences.Get("username", "User");
        EmailValueLabel.Text = Preferences.Get("email", "No email");
        RoleValueLabel.Text = Preferences.Get("user_role", "User");
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