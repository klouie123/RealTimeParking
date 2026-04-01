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
    private double _destinationLat;
    private double _destinationLng;

    private ParkingSlotUiModel? _selectedSlotForReserve;

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
    }

    private async Task LoadSlotsAsync()
    {
        try
        {
            var slots = await _parkingService.GetSlotsByLocationAsync(_parkingLocationId);

            if (slots == null)
            {
                SlotsCollectionView.ItemsSource = null;
                return;
            }

            var uiSlots = slots.Select(slot =>
            {
                bool isAvailable = string.Equals(slot.Status, "Available", StringComparison.OrdinalIgnoreCase);

                return new ParkingSlotUiModel
                {
                    Id = slot.Id,
                    SlotCode = slot.SlotCode,
                    Status = slot.Status,
                    IsAvailable = isAvailable,

                    SlotBackgroundColor = isAvailable ? "#FFFFFF" : "#FEE2E2",
                    SlotBorderColor = isAvailable ? "#E5E7EB" : "#FCA5A5",
                    SlotTextColor = isAvailable ? "#111827" : "#991B1B",
                    SlotSubTextColor = isAvailable ? "#6B7280" : "#B91C1C",
                    StatusBadgeColor = isAvailable ? "#16A34A" : "#DC2626",
                    SlotOpacity = isAvailable ? 1.0 : 0.85,
                    SlotMessage = isAvailable
                        ? "Tap to reserve this slot."
                        : "This slot is already reserved."
                };
            }).ToList();

            SlotsCollectionView.ItemsSource = uiSlots;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void SlotCard_Tapped(object sender, TappedEventArgs e)
    {
        try
        {
            if (e.Parameter is not ParkingSlotUiModel slot)
                return;

            if (!slot.IsAvailable)
                return;

            _selectedSlotForReserve = slot;

            PopupSlotCodeLabel.Text = slot.SlotCode;
            PopupSlotMessageLabel.Text = $"Do you want to reserve {slot.SlotCode}?";

            await ShowReservePopupAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task ShowReservePopupAsync()
    {
        ReservePopupOverlay.IsVisible = true;
        ReservePopupPanel.TranslationY = 300;
        await ReservePopupPanel.TranslateTo(0, 0, 220, Easing.CubicOut);
    }

    private async Task HideReservePopupAsync()
    {
        await ReservePopupPanel.TranslateTo(0, 300, 180, Easing.CubicIn);
        ReservePopupOverlay.IsVisible = false;
    }

    private async void CancelReservePopup_Clicked(object sender, EventArgs e)
    {
        await HideReservePopupAsync();
    }

    private async void ReservePopupOverlay_Tapped(object sender, TappedEventArgs e)
    {
        await HideReservePopupAsync();
    }

    private async void ConfirmReserveButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_selectedSlotForReserve == null)
                return;

            var userId = Preferences.Get("user_id", 0);

            if (userId == 0)
            {
                await HideReservePopupAsync();
                await DisplayAlert("Error", "User not found.", "OK");
                return;
            }

            var success = await _parkingService.ReserveSlotAsync(userId, _selectedSlotForReserve.Id);

            await HideReservePopupAsync();

            if (!success)
            {
                await DisplayAlert("Failed", "Could not reserve slot. It may already be reserved.", "OK");
                await LoadSlotsAsync();
                return;
            }

            string destinationName = Uri.UnescapeDataString(ParkingLocationName ?? "Reserved Parking");
            _navigationState.StartNavigation(destinationName, _destinationLat, _destinationLng);

            await DisplayAlert("Success", $"Slot {_selectedSlotForReserve.SlotCode} reserved successfully.", "OK");

            _selectedSlotForReserve = null;
            await LoadSlotsAsync();
        }
        catch (Exception ex)
        {
            await HideReservePopupAsync();
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}

