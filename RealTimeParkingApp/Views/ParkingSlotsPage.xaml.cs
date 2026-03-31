using System.Globalization;
using RealTimeParkingApp.Models;
using RealTimeParkingApp.Services;

namespace RealTimeParkingApp.Views;

[QueryProperty(nameof(ParkingLocationIdText), "parkingLocationId")]
[QueryProperty(nameof(ParkingLocationName), "parkingLocationName")]
[QueryProperty(nameof(DestinationLatText), "lat")]
[QueryProperty(nameof(DestinationLngText), "lng")]
public partial class ParkingSlotsPage : ContentPage
{
    private readonly ParkingService _parkingService;
    private readonly NavigationStateService _navigationState;

    private int _parkingLocationId;
    private ParkingSlot? _reservedSlot;

    // ito ang gagamitin sa navigation
    private double _destinationLat;
    private double _destinationLng;

    public string DestinationLatText { get; set; }
    public string DestinationLngText { get; set; }

    public string ParkingLocationIdText { get; set; }
    public string ParkingLocationName { get; set; }

    public ParkingSlotsPage(ParkingService parkingService, NavigationStateService navigationState)
    {
        InitializeComponent();
        _parkingService = parkingService;
        _navigationState = navigationState;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!int.TryParse(ParkingLocationIdText, out _parkingLocationId))
            return;

        LocationNameLabel.Text = Uri.UnescapeDataString(ParkingLocationName ?? "Parking Slots");

        double.TryParse(DestinationLatText, CultureInfo.InvariantCulture, out _destinationLat);
        double.TryParse(DestinationLngText, CultureInfo.InvariantCulture, out _destinationLng);

        await LoadSlotsAsync();
        await CheckActiveReservationAsync();
    }

    private async Task LoadSlotsAsync()
    {
        var slots = await _parkingService.GetSlotsByLocationAsync(_parkingLocationId);
        SlotsCollectionView.ItemsSource = slots;
    }

    private async Task CheckActiveReservationAsync()
    {
        try
        {
            var userId = Preferences.Get("user_id", 0);
            if (userId == 0)
                return;

            var reservation = await _parkingService.GetActiveReservationAsync(userId);

            if (reservation != null && reservation.ParkingLocationId == _parkingLocationId)
            {
                NavigateButton.IsVisible = true;
            }
            else
            {
                NavigateButton.IsVisible = false;
            }
        }
        catch
        {
            NavigateButton.IsVisible = false;
        }
    }

    private async void ReserveButton_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not ParkingSlot slot)
            return;

        if (slot.Status != "Available")
        {
            await DisplayAlert("Unavailable", "This slot is not available.", "OK");
            return;
        }

        var userId = Preferences.Get("user_id", 0);

        if (userId == 0)
        {
            await DisplayAlert("Error", "User not found.", "OK");
            return;
        }

        var success = await _parkingService.ReserveSlotAsync(userId, slot.Id);

        if (!success)
        {
            await DisplayAlert("Failed", "Could not reserve slot. It may already be reserved.", "OK");
            await LoadSlotsAsync();
            return;
        }

        _reservedSlot = slot;

        await DisplayAlert("Success", $"Slot {slot.SlotCode} reserved successfully.", "OK");

        NavigateButton.IsVisible = true;

        await LoadSlotsAsync();
    }

    private async void NavigateButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            var userId = Preferences.Get("user_id", 0);
            if (userId == 0)
            {
                await DisplayAlert("Error", "User not found.", "OK");
                return;
            }

            var reservation = await _parkingService.GetActiveReservationAsync(userId);
            if (reservation == null || reservation.ParkingLocationId != _parkingLocationId)
            {
                await DisplayAlert("Info", "Please reserve a slot first.", "OK");
                return;
            }

            if (_destinationLat == 0 && _destinationLng == 0)
            {
                await DisplayAlert("Error", "Destination coordinates not found.", "OK");
                return;
            }

            string destinationName = Uri.UnescapeDataString(ParkingLocationName ?? "Reserved Parking");

            _navigationState.StartNavigation(destinationName, _destinationLat, _destinationLng);

            string encodedName = Uri.EscapeDataString(destinationName);

            //await DisplayAlert("Debug", $"Destination: {_destinationLat}, {_destinationLng}", "OK");

            await Shell.Current.GoToAsync(
                $"{nameof(NavigationMapPage)}" +
                $"?destLat={_destinationLat.ToString(CultureInfo.InvariantCulture)}" +
                $"&destLng={_destinationLng.ToString(CultureInfo.InvariantCulture)}" +
                $"&destName={encodedName}");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}