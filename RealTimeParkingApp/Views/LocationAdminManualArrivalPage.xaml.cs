using RealTimeParkingApp.Models;
using RealTimeParkingApp.Services;

namespace RealTimeParkingApp.Views;

public partial class LocationAdminManualArrivalPage : ContentPage
{
    private readonly ApiService _apiService;

    public LocationAdminManualArrivalPage()
    {
        InitializeComponent();
        _apiService = App.Services.GetRequiredService<ApiService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadReservationsAsync();
    }

    private async Task LoadReservationsAsync()
    {
        try
        {
            var items = await _apiService.GetLocationManualArrivalReservationsAsync();
            ReservationsCollectionView.ItemsSource = items;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void ConfirmArrival_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is not Button button ||
                button.CommandParameter is not ManualArrivalReservationModel item)
                return;

            bool confirm = await DisplayAlert(
                "Confirm Arrival",
                $"Confirm arrival for {item.FullName} in slot {item.SlotCode}?",
                "Yes",
                "No");

            if (!confirm)
                return;

            var result = await _apiService.ConfirmManualArrivalAsync(item.ReservationId);

            if (result == null)
            {
                await DisplayAlert("Error", "Manual arrival confirmation failed.", "OK");
                return;
            }

            await DisplayAlert(result.Success ? "Success" : "Error", result.Message, "OK");

            if (result.Success)
                await LoadReservationsAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}