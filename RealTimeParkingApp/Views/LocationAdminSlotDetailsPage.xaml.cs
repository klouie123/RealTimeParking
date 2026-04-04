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
        set => int.TryParse(value, out _slotId);
    }

    public LocationAdminSlotDetailsPage()
    {
        InitializeComponent();
        _apiService = App.Services.GetRequiredService<ApiService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_slotId <= 0)
            await Task.Delay(150);

        if (_slotId <= 0)
        {
            await DisplayAlert("Error", "Invalid slot id.", "OK");
            return;
        }

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
        if (_refreshCts == null)
            return;

        _refreshCts.Cancel();
        _refreshCts.Dispose();
        _refreshCts = null;
    }

    private async Task LoadAsync(bool showError = false)
    {
        try
        {
            var details = await _apiService.GetAdminSlotDetailsAsync(_slotId);

            if (details == null)
            {
                ApplyFallbackState();
                return;
            }

            SlotCodeLabel.Text = $"Slot: {details.SlotCode}";
            SlotStatusLabel.Text = $"Status: {details.Status}";
            ReservedUserLabel.Text = $"Reserved User: {details.ReservedUser ?? "None"}";
            ReservedAtLabel.Text = $"Reserved At: {FormatDate(details.ReservedAt)}";
            CheckInAtLabel.Text = $"Check In At: {FormatDate(details.CheckInAt)}";
            ReservationReferenceLabel.Text = $"Reservation Ref: {details.ReservationReference ?? "N/A"}";
            PaymentReferenceLabel.Text = $"Payment Ref: {details.PaymentReference ?? "N/A"}";

            ApplyButtonState(details);
        }
        catch (Exception ex)
        {
            ApplyFallbackState();

            if (showError)
                await DisplayAlert("Error", $"Failed to load slot details.\n{ex.Message}", "OK");
        }
    }

    private void ApplyFallbackState()
    {
        SlotCodeLabel.Text = $"Slot: {_slotId}";
        SlotStatusLabel.Text = "Status: Waiting for slot details";
        ReservedUserLabel.Text = "Reserved User: N/A";
        ReservedAtLabel.Text = "Reserved At: N/A";
        CheckInAtLabel.Text = "Check In At: N/A";
        ReservationReferenceLabel.Text = "Reservation Ref: N/A";
        PaymentReferenceLabel.Text = "Payment Ref: N/A";

        ConfirmArrivalButton.IsVisible = true;
        CashCheckoutButton.IsVisible = false;
        ManualCheckoutButton.IsVisible = false;
        ScanArrivalQrButton.IsVisible = true;
        ScanPaymentQrButton.IsVisible = false;
    }

    private void ApplyButtonState(AdminSlotDetailsModel details)
    {
        var status = details.Status?.Trim() ?? string.Empty;
        var paymentMethod = details.PaymentMethod?.Trim() ?? string.Empty;

        bool isReserved = status.Equals("Reserved", StringComparison.OrdinalIgnoreCase);
        bool isOccupied = status.Equals("Occupied", StringComparison.OrdinalIgnoreCase);
        bool isCompleted = status.Equals("Completed", StringComparison.OrdinalIgnoreCase);
        bool isCash = paymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase);
        bool isGcash = paymentMethod.Equals("GCash", StringComparison.OrdinalIgnoreCase);

        if (isCompleted)
        {
            ConfirmArrivalButton.IsVisible = false;
            CashCheckoutButton.IsVisible = false;
            ManualCheckoutButton.IsVisible = false;
            ScanArrivalQrButton.IsVisible = false;
            ScanPaymentQrButton.IsVisible = false;
            return;
        }

        ConfirmArrivalButton.IsVisible = isReserved;

        if (isCash)
        {
            ScanArrivalQrButton.IsVisible = false;
            ScanPaymentQrButton.IsVisible = false;
            ManualCheckoutButton.IsVisible = false;
            CashCheckoutButton.IsVisible = isOccupied || isReserved;
        }
        else if (isGcash)
        {
            CashCheckoutButton.IsVisible = false;
            ManualCheckoutButton.IsVisible = false;
            ScanArrivalQrButton.IsVisible = isReserved && !string.IsNullOrWhiteSpace(details.ReservationReference);
            ScanPaymentQrButton.IsVisible = isOccupied && !string.IsNullOrWhiteSpace(details.PaymentReference);
        }
        else
        {
            // fallback kung walang payment method na bumalik
            CashCheckoutButton.IsVisible = isOccupied;
            ScanArrivalQrButton.IsVisible = isReserved && !string.IsNullOrWhiteSpace(details.ReservationReference);
            ScanPaymentQrButton.IsVisible = isOccupied && !string.IsNullOrWhiteSpace(details.PaymentReference);
            ManualCheckoutButton.IsVisible = false;
        }
    }

    private static string FormatDate(DateTime? value)
    {
        return value?.ToLocalTime().ToString("MMM dd, yyyy hh:mm tt") ?? "N/A";
    }

    private async void ManualArrive_Clicked(object sender, EventArgs e)
    {
        try
        {
            _isBusy = true;
            ConfirmArrivalButton.IsEnabled = false;

            var result = await _apiService.ManualArriveAsync(_slotId);

            bool isSuccess = result?.Success == true;

            await DisplayAlert(
                isSuccess ? "Success" : "Error",
                result?.Message ?? (isSuccess ? "Arrival confirmed." : "Failed."),
                "OK");

            await LoadAsync();
        }
        finally
        {
            ConfirmArrivalButton.IsEnabled = true;
            _isBusy = false;
        }
    }

    private async void CashCheckout_Clicked(object sender, EventArgs e)
    {
        try
        {
            _isBusy = true;
            CashCheckoutButton.IsEnabled = false;

            var result = await _apiService.CashCheckoutAsync(_slotId);
            bool isSuccess = result?.Success == true;

            await DisplayAlert(
                isSuccess ? "Success" : "Error",
                result?.Message ?? (isSuccess ? "Cash checkout successful." : "Cash checkout failed."),
                "OK");

            if (isSuccess)
            {
                StopAutoRefresh();
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await LoadAsync();
            }
        }
        finally
        {
            CashCheckoutButton.IsEnabled = true;
            _isBusy = false;
        }
    }

    private async void ManualCheckout_Clicked(object sender, EventArgs e)
    {
        try
        {
            _isBusy = true;
            ManualCheckoutButton.IsEnabled = false;

            var result = await _apiService.ManualCheckoutAsync(_slotId);
            bool isSuccess = result?.Success == true;

            await DisplayAlert(
                isSuccess ? "Success" : "Error",
                result?.Message ?? (isSuccess ? "Checkout successful." : "Failed."),
                "OK");

            if (isSuccess)
            {
                StopAutoRefresh();
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await LoadAsync();
            }
        }
        finally
        {
            ManualCheckoutButton.IsEnabled = true;
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