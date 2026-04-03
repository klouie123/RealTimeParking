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
    private readonly ApiService _apiService;
    private readonly NavigationStateService _navigationState;

    private int _parkingLocationId;
    private double _destinationLat;
    private double _destinationLng;

    private ParkingSlotUiModel? _selectedSlotForReserve;
    private bool _isReserving;

    public string DestinationLatText { get; set; } = string.Empty;
    public string DestinationLngText { get; set; } = string.Empty;
    public string ParkingLocationIdText { get; set; } = string.Empty;
    public string ParkingLocationName { get; set; } = string.Empty;

    public ParkingSlotsPage(
        ParkingService parkingService,
        ApiService apiService,
        NavigationStateService navigationState)
    {
        InitializeComponent();
        _parkingService = parkingService;
        _apiService = apiService;
        _navigationState = navigationState;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!int.TryParse(ParkingLocationIdText, out _parkingLocationId))
        {
            await DisplayAlert("Error", "Invalid parking location.", "OK");
            return;
        }

        LocationNameLabel.Text = Uri.UnescapeDataString(
            string.IsNullOrWhiteSpace(ParkingLocationName)
                ? "Parking Slots"
                : ParkingLocationName);

        double.TryParse(DestinationLatText, NumberStyles.Any, CultureInfo.InvariantCulture, out _destinationLat);
        double.TryParse(DestinationLngText, NumberStyles.Any, CultureInfo.InvariantCulture, out _destinationLng);

        await LoadSlotsAsync();
    }

    private async Task LoadSlotsAsync()
    {
        try
        {
            var slots = await _parkingService.GetSlotsByLocationAsync(_parkingLocationId);

            if (slots == null || slots.Count == 0)
            {
                SlotsCollectionView.ItemsSource = new List<ParkingSlotUiModel>();
                return;
            }

            var uiSlots = slots.Select(MapToUiModel).ToList();
            SlotsCollectionView.ItemsSource = uiSlots;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load slots: {ex.Message}", "OK");
        }
    }

    private static ParkingSlotUiModel MapToUiModel(ParkingSlot slot)
    {
        var status = slot.Status?.Trim() ?? "Unknown";

        var isAvailable = status.Equals("Available", StringComparison.OrdinalIgnoreCase);
        var isReserved = status.Equals("Reserved", StringComparison.OrdinalIgnoreCase);
        var isOccupied = status.Equals("Occupied", StringComparison.OrdinalIgnoreCase);

        return new ParkingSlotUiModel
        {
            Id = slot.Id,
            SlotCode = slot.SlotCode,
            Status = status,
            IsAvailable = isAvailable,

            SlotBackgroundColor = isAvailable ? "#FFFFFF" : "#F8FAFC",
            SlotBorderColor = isAvailable ? "#E5E7EB" : "#CBD5E1",
            SlotTextColor = isAvailable ? "#111827" : "#334155",
            SlotSubTextColor = isAvailable ? "#6B7280" : "#64748B",
            StatusBadgeColor = isAvailable
                ? "#16A34A"
                : isReserved
                    ? "#D97706"
                    : isOccupied
                        ? "#DC2626"
                        : "#64748B",
            SlotOpacity = 1.0,
            SlotMessage = isAvailable
                ? "Tap to reserve this slot."
                : isReserved
                    ? "This slot is currently reserved."
                    : isOccupied
                        ? "This slot is currently occupied."
                        : "Slot status is unavailable."
        };
    }

    private async void SlotCard_Tapped(object sender, TappedEventArgs e)
    {
        try
        {
            if (_isReserving)
                return;

            if (e.Parameter is not ParkingSlotUiModel slot)
                return;

            if (!slot.IsAvailable)
            {
                await DisplayAlert("Info", $"{slot.SlotCode} is not available.", "OK");
                return;
            }

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
        if (_isReserving)
            return;

        try
        {
            if (_selectedSlotForReserve == null)
                return;

            _isReserving = true;
            ConfirmReserveButton.IsEnabled = false;

            var reservedSlotCode = _selectedSlotForReserve.SlotCode;
            var success = await _apiService.ReserveSlotAsync(_selectedSlotForReserve.Id);

            await HideReservePopupAsync();

            if (!success)
            {
                await DisplayAlert("Failed", "Could not reserve slot. It may already be reserved.", "OK");
                await LoadSlotsAsync();
                return;
            }

            var destinationName = Uri.UnescapeDataString(
                string.IsNullOrWhiteSpace(ParkingLocationName)
                    ? "Reserved Parking"
                    : ParkingLocationName);

            _navigationState.StartNavigation(destinationName, _destinationLat, _destinationLng);

            await DisplayAlert("Success", $"Slot {reservedSlotCode} reserved successfully.", "OK");

            _selectedSlotForReserve = null;
            await LoadSlotsAsync();

            await Shell.Current.GoToAsync(nameof(MyActiveParkingPage));
        }
        catch (Exception ex)
        {
            await HideReservePopupAsync();
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            _isReserving = false;
            ConfirmReserveButton.IsEnabled = true;
        }
    }
}