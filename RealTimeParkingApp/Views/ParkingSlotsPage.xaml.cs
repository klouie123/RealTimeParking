using RealTimeParkingApp.Models;
using RealTimeParkingApp.Services;

namespace RealTimeParkingApp.Views;

[QueryProperty(nameof(ParkingLocationIdText), "parkingLocationId")]
[QueryProperty(nameof(ParkingLocationName), "parkingLocationName")]
public partial class ParkingSlotsPage : ContentPage
{
    private readonly ParkingService _parkingService;
    private readonly NavigationStateService _navigationState;

    private int _parkingLocationId;

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

        await LoadSlotsAsync();
    }

    private async Task LoadSlotsAsync()
    {
        var slots = await _parkingService.GetSlotsByLocationAsync(_parkingLocationId);
        SlotsCollectionView.ItemsSource = slots;
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

        await DisplayAlert("Success", $"Slot {slot.SlotCode} reserved successfully.", "OK");
        await LoadSlotsAsync();
    }
}