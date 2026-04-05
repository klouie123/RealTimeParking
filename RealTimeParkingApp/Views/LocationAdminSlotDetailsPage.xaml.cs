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

        await LoadAsync(showError: true);
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
                            await LoadAsync();
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

            SlotCodeLabel.Text = $"Slot {details.SlotCode}";
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
        SlotCodeLabel.Text = $"Slot {_slotId}";
        SlotStatusLabel.Text = "Status: Waiting for slot details";
        ReservedUserLabel.Text = "Reserved User: N/A";
        ReservedAtLabel.Text = "Reserved At: N/A";
        CheckInAtLabel.Text = "Check In At: N/A";
        ReservationReferenceLabel.Text = "Reservation Ref: N/A";
        PaymentReferenceLabel.Text = "Payment Ref: N/A";

        ConfirmArrivalButton.IsEnabled = true;
        ScanArrivalQrButton.IsEnabled = true;
        CashCheckoutButton.IsEnabled = false;
        OpenGcashButton.IsEnabled = false;
        ManualCheckoutButton.IsEnabled = false;
    }

    private void ApplyButtonState(AdminSlotDetailsModel details)
    {
        string status = details.Status?.Trim() ?? string.Empty;
        string paymentMethod = details.PaymentMethod?.Trim() ?? string.Empty;

        bool isReserved = status.Equals("Reserved", StringComparison.OrdinalIgnoreCase);
        bool isOccupied = status.Equals("Occupied", StringComparison.OrdinalIgnoreCase);
        bool isCompleted = status.Equals("Completed", StringComparison.OrdinalIgnoreCase);
        bool isPendingCash = status.Equals("PendingCashConfirmation", StringComparison.OrdinalIgnoreCase);
        bool isPendingGcash = status.Equals("PendingGcashConfirmation", StringComparison.OrdinalIgnoreCase);

        bool isCash = paymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase);
        bool isGcash = paymentMethod.Equals("GCash", StringComparison.OrdinalIgnoreCase);

        bool hasArrivalQr = !string.IsNullOrWhiteSpace(details.ReservationReference);
        bool hasCheckedIn = details.CheckInAt.HasValue;

        ConfirmArrivalButton.IsEnabled = false;
        ScanArrivalQrButton.IsEnabled = false;
        CashCheckoutButton.IsEnabled = false;
        OpenGcashButton.IsEnabled = false;
        ManualCheckoutButton.IsEnabled = false;

        if (isCompleted)
            return;

        if (isReserved && !hasCheckedIn)
        {
            ConfirmArrivalButton.IsEnabled = true;
            ScanArrivalQrButton.IsEnabled = hasArrivalQr;
        }

        if (isCash && (isOccupied || isPendingCash))
        {
            CashCheckoutButton.IsEnabled = true;
            ManualCheckoutButton.IsEnabled = true;
        }

        if (isGcash && (isOccupied || isPendingGcash))
        {
            OpenGcashButton.IsEnabled = true;
            ManualCheckoutButton.IsEnabled = true;
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

            await LoadAsync(showError: true);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
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
                result?.Message ?? (isSuccess ? "Cash payment confirmed." : "Cash payment failed."),
                "OK");

            if (isSuccess)
            {
                StopAutoRefresh();
                await Shell.Current.GoToAsync("..");
                return;
            }

            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
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
            ManualCheckoutButton.IsEnabled = false;

            var result = await _apiService.ManualCheckoutAsync(_slotId);
            bool isSuccess = result?.Success == true;

            await DisplayAlert(
                isSuccess ? "Success" : "Error",
                result?.Message ?? (isSuccess ? "Checkout successful." : "Checkout failed."),
                "OK");

            if (isSuccess)
            {
                StopAutoRefresh();
                await Shell.Current.GoToAsync("..");
                return;
            }

            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async void OpenGcash_Clicked(object sender, EventArgs e)
    {
        try
        {
            _isBusy = true;
            OpenGcashButton.IsEnabled = false;

            bool open = await DisplayAlert(
                "Open GCash",
                "You will be redirected to GCash to complete your payment.",
                "Continue",
                "Cancel");

            if (!open)
                return;

            var gcashUri = new Uri("gcash://");

            if (await Launcher.Default.CanOpenAsync(gcashUri))
            {
                await Launcher.Default.OpenAsync(gcashUri);
            }
            else
            {
                await Launcher.Default.OpenAsync("https://www.gcash.com");
            }

            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            OpenGcashButton.IsEnabled = true;
            _isBusy = false;
        }
    }

    private async void ScanArrivalQr_Clicked(object sender, EventArgs e)
    {
        StopAutoRefresh();
        await Shell.Current.GoToAsync($"{nameof(AdminQrScannerPage)}?mode=arrival&slotId={_slotId}");
    }
}