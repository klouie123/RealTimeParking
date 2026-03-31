using RealTimeParkingApp.Shells;

namespace RealTimeParkingApp.Views;

public partial class StartupPage : ContentPage
{
	public StartupPage()
	{
		InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // konting delay para makita loading page
        await Task.Delay(1500);

        try
        {
            var token = Preferences.Get("jwt_token", string.Empty);
            var role = Preferences.Get("user_role", string.Empty);

            if (!string.IsNullOrEmpty(token))
            {
                if (role == "Admin")
                {
                    Application.Current.MainPage = new AdminShell();
                }
                else
                {
                    Application.Current.MainPage = new UserShell();
                }
            }
            else
            {
                Application.Current.MainPage = new NavigationPage(new LoginPage());
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
            Application.Current.MainPage = new NavigationPage(new LoginPage());
        }
    }
}