using RealTimeParkingApp.Models;
using RealTimeParkingApp.Services;

namespace RealTimeParkingApp.Views;

public partial class LocationAdminMaintenancePage : ContentPage
{
    private readonly ApiService _apiService;
    private bool _isBusy;

    public LocationAdminMaintenancePage()
    {
        InitializeComponent();
        _apiService = App.Services.GetRequiredService<ApiService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var maintenance = await _apiService.GetLocationMaintenanceAsync();

            if (maintenance == null)
            {
                await DisplayAlert("Error", "Failed to load maintenance data.", "OK");
                return;
            }

            ParkingPriceEntry.Text = maintenance.ParkingPrice.ToString("0.##");
            SlotsCollectionView.ItemsSource = maintenance.Slots;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void SavePrice_Clicked(object sender, EventArgs e)
    {
        if (_isBusy)
            return;

        try
        {
            if (!decimal.TryParse(ParkingPriceEntry.Text, out var price) || price < 0)
            {
                await DisplayAlert("Invalid", "Please enter a valid parking price.", "OK");
                return;
            }

            _isBusy = true;

            var result = await _apiService.UpdateParkingPriceAsync(price);

            await DisplayAlert(
                result?.Success == true ? "Success" : "Error",
                result?.Message ?? "Failed to update price.",
                "OK");

            if (result?.Success == true)
                await LoadAsync();
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async void AddSlot_Clicked(object sender, EventArgs e)
    {
        if (_isBusy)
            return;

        try
        {
            var code = NewSlotCodeEntry.Text?.Trim();

            if (string.IsNullOrWhiteSpace(code))
            {
                await DisplayAlert("Invalid", "Please enter a slot code.", "OK");
                return;
            }

            _isBusy = true;

            var result = await _apiService.AddParkingSlotAsync(code);

            await DisplayAlert(
                result?.Success == true ? "Success" : "Error",
                result?.Message ?? "Failed to add slot.",
                "OK");

            if (result?.Success == true)
            {
                NewSlotCodeEntry.Text = string.Empty;
                await LoadAsync();
            }
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async void SaveSlotName_Clicked(object sender, EventArgs e)
    {
        if (_isBusy)
            return;

        try
        {
            if ((sender as Button)?.CommandParameter is not AdminMaintenanceSlotItem slot)
                return;

            var newCode = slot.EditableSlotCode?.Trim();

            if (string.IsNullOrWhiteSpace(newCode))
            {
                await DisplayAlert("Invalid", "Slot name cannot be empty.", "OK");
                return;
            }

            _isBusy = true;

            var result = await _apiService.RenameParkingSlotAsync(slot.Id, newCode);

            await DisplayAlert(
                result?.Success == true ? "Success" : "Error",
                result?.Message ?? "Failed to rename slot.",
                "OK");

            if (result?.Success == true)
                await LoadAsync();
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async void ToggleSlotActive_Clicked(object sender, EventArgs e)
    {
        if (_isBusy)
            return;

        try
        {
            if ((sender as Button)?.CommandParameter is not AdminMaintenanceSlotItem slot)
                return;

            _isBusy = true;

            var result = await _apiService.ToggleParkingSlotActiveAsync(slot.Id, !slot.IsActive);

            await DisplayAlert(
                result?.Success == true ? "Success" : "Error",
                result?.Message ?? "Failed to update slot status.",
                "OK");

            if (result?.Success == true)
                await LoadAsync();
        }
        finally
        {
            _isBusy = false;
        }
    }
}