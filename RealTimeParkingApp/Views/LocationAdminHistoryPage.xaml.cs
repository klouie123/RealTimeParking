using RealTimeParkingApp.Services;

namespace RealTimeParkingApp.Views;

public partial class LocationAdminHistoryPage : ContentPage
{
    private readonly ApiService _apiService;

    public LocationAdminHistoryPage()
    {
        InitializeComponent();
        _apiService = App.Services.GetRequiredService<ApiService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            var history = await _apiService.GetLocationTransactionHistoryAsync();
            HistoryCollectionView.ItemsSource = history;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}