using RealTimeParkingApp.Models;
using RealTimeParkingApp.Services;

namespace RealTimeParkingApp.Views;

public partial class LocationAdminSlotsPage : ContentPage
{
    private readonly ApiService _apiService;
    private CancellationTokenSource? _refreshCts;
    private bool _isNavigating;

    public LocationAdminSlotsPage()
    {
        InitializeComponent();
        _apiService = App.Services.GetRequiredService<ApiService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _isNavigating = false;
        LocationNameLabel.Text = Preferences.Get("parking_location_name", "My Parking Slots");
        await LoadSlotsAsync();
        StartAutoRefresh();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopAutoRefresh();
    }

    private void StartAutoRefresh()
    {
        StopAutoRefresh();
        _refreshCts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            try
            {
                while (!_refreshCts.Token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), _refreshCts.Token);

                    if (_refreshCts.Token.IsCancellationRequested || _isNavigating)
                        break;

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        if (!_isNavigating)
                            await LoadSlotsAsync(showError: false);
                    });
                }
            }
            catch (TaskCanceledException)
            {
            }
        });
    }

    private void StopAutoRefresh()
    {
        if (_refreshCts != null)
        {
            _refreshCts.Cancel();
            _refreshCts.Dispose();
            _refreshCts = null;
        }
    }

    private async Task LoadSlotsAsync(bool showError = true)
    {
        try
        {
            var slots = await _apiService.GetMyLocationSlotsAsync();

            System.Diagnostics.Debug.WriteLine($"Slots count: {slots.Count}");

            LocationNameLabel.Text = $"{Preferences.Get("parking_location_name", "My Parking Slots")} ({slots.Count})";

            var displaySlots = slots.Select(slot => new LocationAdminSlotDisplayModel
            {
                Id = slot.Id,
                ParkingLocationId = slot.ParkingLocationId,
                SlotCode = slot.SlotCode,
                Status = slot.Status,
                IsActive = slot.IsActive,
                CreatedAt = slot.CreatedAt,
                StatusBadgeColor = GetStatusBadgeColor(slot.Status),
                SlotMessage = GetSlotMessage(slot.Status, slot.IsActive)
            }).ToList();

            SlotsCollectionView.ItemsSource = displaySlots;
        }
        catch (Exception ex)
        {
            if (showError)
                await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private static Color GetStatusBadgeColor(string? status)
    {
        return status switch
        {
            "Available" => Color.FromArgb("#16A34A"),
            "Reserved" => Color.FromArgb("#D97706"),
            "Occupied" => Color.FromArgb("#DC2626"),
            _ => Color.FromArgb("#64748B")
        };
    }

    private static string GetSlotMessage(string? status, bool isActive)
    {
        if (!isActive)
            return "This slot is currently inactive.";

        return status switch
        {
            "Available" => "This slot is open and ready to use.",
            "Reserved" => "This slot is currently reserved.",
            "Occupied" => "This slot is currently occupied.",
            _ => "Slot status is unknown."
        };
    }

    private async void SlotCard_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not LocationAdminSlotDisplayModel selected)
            return;

        try
        {
            _isNavigating = true;
            StopAutoRefresh();

            await Shell.Current.GoToAsync($"{nameof(LocationAdminSlotDetailsPage)}?slotId={selected.Id}");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Navigation Error", ex.Message, "OK");
            _isNavigating = false;
            StartAutoRefresh();
        }
    }
}