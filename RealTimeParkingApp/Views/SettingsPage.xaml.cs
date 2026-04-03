using RealTimeParkingApp.Services;

namespace RealTimeParkingApp.Views;

public partial class SettingsPage : ContentPage
{
    private bool _themeLoaded;

    public SettingsPage()
    {
        InitializeComponent();
        LoadThemeSelection();
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
}