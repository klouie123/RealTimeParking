using RealTimeParkingApp.Models;
using RealTimeParkingApp.Services;

namespace RealTimeParkingApp.Views;

[QueryProperty(nameof(SlotId), "slotId")]
public partial class LocationAdminSlotDetailsPage : ContentPage
{
    private readonly ApiService _apiService;
    private int _slotId;
    private CancellationTokenSource? _refreshCts;
    private bool _isBusy;

    public string SlotId
    {
        set
        {
            int.TryParse(value, out _slotId);
        }
    }

    public LocationAdminSlotDetailsPage()
    {
        InitializeComponent();
        _apiService = App.Services.GetRequiredService<ApiService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
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
                    await Task.Delay(TimeSpan.FromSeconds(3), _refreshCts.Token);

                    if (_refreshCts.Token.IsCancellationRequested || _isBusy)
                        continue;

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        if (!_isBusy)
                            await LoadAsync(false);
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

    private async Task LoadAsync(bool showError = true)
    {
        try
        {
            var details = await _apiService.GetAdminSlotDetailsAsync(_slotId);

            if (details == null)
            {
                if (showError)
                    await DisplayAlert("Error", "Failed to load slot details.", "OK");
                return;
            }

            SlotCodeLabel.Text = $"Slot: {details.SlotCode}";
            SlotStatusLabel.Text = $"Status: {details.Status}";
            ReservedUserLabel.Text = $"Reserved User: {details.ReservedUser ?? "None"}";
            ReservedAtLabel.Text = $"Reserved At: {details.ReservedAt?.ToLocalTime().ToString("MMM dd, yyyy hh:mm tt") ?? "N/A"}";
            CheckInAtLabel.Text = $"Check In At: {details.CheckInAt?.ToLocalTime().ToString("MMM dd, yyyy hh:mm tt") ?? "N/A"}";
            ReservationReferenceLabel.Text = $"Reservation Ref: {details.ReservationReference ?? "N/A"}";
            PaymentReferenceLabel.Text = $"Payment Ref: {details.PaymentReference ?? "N/A"}";
        }
        catch (Exception ex)
        {
            if (showError)
                await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void ManualArrive_Clicked(object sender, EventArgs e)
    {
        try
        {
            _isBusy = true;

            var result = await _apiService.ManualArriveAsync(_slotId);
            await DisplayAlert(result?.Success == true ? "Success" : "Error", result?.Message ?? "Failed.", "OK");

            await LoadAsync();
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async void ManualCheckout_Clicked(object sender, EventArgs e)
    {
        try
        {
            _isBusy = true;

            var result = await _apiService.ManualCheckoutAsync(_slotId);
            await DisplayAlert(result?.Success == true ? "Success" : "Error", result?.Message ?? "Failed.", "OK");

            await LoadAsync();
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async void ScanArrivalQr_Clicked(object sender, EventArgs e)
    {
        StopAutoRefresh();
        await Shell.Current.GoToAsync($"{nameof(AdminQrScannerPage)}?mode=arrival&slotId={_slotId}");
    }

    private async void ScanPaymentQr_Clicked(object sender, EventArgs e)
    {
        StopAutoRefresh();
        await Shell.Current.GoToAsync($"{nameof(AdminQrScannerPage)}?mode=payment&slotId={_slotId}");
    }
}