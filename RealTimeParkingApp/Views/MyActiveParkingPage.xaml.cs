using System.Globalization;
using RealTimeParkingApp.Models;
using RealTimeParkingApp.Services;
using ZXing;
using ZXing.Common;

namespace RealTimeParkingApp.Views;

public partial class MyActiveParkingPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly NavigationStateService _navigationState;

    private ActiveParkingModel? _activeParking;
    private CancellationTokenSource? _refreshCts;
    private bool _isBusy;
    private bool _noActiveParkingHandled;

    private const string PaymentMethodPrefix = "payment_method_";
    private const string ArrivedPrefix = "arrived_";

    public MyActiveParkingPage()
    {
        InitializeComponent();
        _apiService = App.Services.GetRequiredService<ApiService>();
        _navigationState = App.Services.GetRequiredService<NavigationStateService>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _noActiveParkingHandled = false;

        try
        {
            StartAutoRefresh();
            _ = LoadAsync();
        }
        catch
        {
        }
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

    private async Task LoadAsync(bool showNoActiveMessage = true)
    {
        try
        {
            var latest = await _apiService.GetMyActiveParkingAsync();

            if (latest == null)
            {
                _navigationState.Clear();
                ResetUi();

                if (showNoActiveMessage && !_noActiveParkingHandled)
                {
                    _noActiveParkingHandled = true;
                    StopAutoRefresh();
                    await DisplayAlert("Info", "No active parking found.", "OK");

                    if (Shell.Current != null)
                        await Shell.Current.GoToAsync("//UserDashboardPage");
                }

                return;
            }

            _activeParking = latest;

            if (_activeParking.HasArrived)
                SaveArrivalState(_activeParking.ReservationId, true);

            if (_navigationState.HasArrived)
                SaveArrivalState(_activeParking.ReservationId, true);

            if (!string.IsNullOrWhiteSpace(_activeParking.PaymentMethod))
                SavePaymentMethod(_activeParking.ReservationId, _activeParking.PaymentMethod);

            ApplyHeaderState(_activeParking);

            bool hasArrived = ResolveHasArrived(_activeParking);
            string paymentMethod = ResolvePaymentMethod(_activeParking);
            string status = _activeParking.Status?.Trim() ?? string.Empty;
            string paymentStatus = _activeParking.PaymentStatus?.Trim() ?? string.Empty;

            if (status.Equals("Reserved", StringComparison.OrdinalIgnoreCase))
            {
                if (!hasArrived)
                    ApplyBeforeArrivalState(_activeParking);
                else
                    ApplyArrivalQrState(_activeParking);
            }
            else if (status.Equals("Occupied", StringComparison.OrdinalIgnoreCase))
            {
                ApplyOccupiedState(paymentMethod);
            }
            else if (status.Equals("PendingCashConfirmation", StringComparison.OrdinalIgnoreCase) ||
                     status.Equals("PendingGcashConfirmation", StringComparison.OrdinalIgnoreCase) ||
                     paymentStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                ApplyWaitingState(paymentMethod);
            }
            else if (status.Equals("Paid", StringComparison.OrdinalIgnoreCase) ||
                     status.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                     paymentStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase))
            {
                StopAutoRefresh();
                await Shell.Current.GoToAsync(
                    $"{nameof(ReservationSuccessPage)}?reservationId={_activeParking.ReservationId}");
            }
            else
            {
                ApplyDefaultState(_activeParking);
            }
        }
        catch (Exception ex)
        {
            if (showNoActiveMessage)
                await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void ApplyArrivalQrState(ActiveParkingModel parking)
    {
        QrFrame.IsVisible = false;
        InfoFrame.IsVisible = true;

        InfoLabel.Text = "You have arrived. Tap the button below to show your arrival QR to the location admin.";

        NavigateButton.IsVisible = false;
        ShowArrivalQrButton.IsVisible = !string.IsNullOrWhiteSpace(parking.ReservationReference);
        DoneParkingButton.IsVisible = false;
    }

    private void ApplyHeaderState(ActiveParkingModel parking)
    {
        string paymentMethod = ResolvePaymentMethod(parking);

        LocationLabel.Text = parking.ParkingLocationName;
        SlotLabel.Text = $"Slot: {parking.SlotCode}";
        StatusLabel.Text = $"Status: {parking.Status}";
        ReferenceLabel.Text = $"Reservation Ref: {parking.ReservationReference}";
        PaymentLabel.Text = $"Payment Ref: {parking.PaymentReference ?? "Not ready"}";
        PaymentMethodLabel.Text = $"Payment Method: {paymentMethod}";
        AmountLabel.Text = $"Amount: ₱{parking.PaymentAmount:F2}";

        HideAllActionButtons();
        QrFrame.IsVisible = false;
        InfoFrame.IsVisible = false;
    }

    private void ApplyBeforeArrivalState(ActiveParkingModel parking)
    {
        QrFrame.IsVisible = false;
        InfoFrame.IsVisible = true;
        InfoLabel.Text = "You have an active reservation. Tap Navigate to go to the parking location.";

        HideAllActionButtons();
        NavigateButton.IsVisible = parking.Latitude != 0 && parking.Longitude != 0;
    }

    private void ApplyOccupiedState(string paymentMethod)
    {
        QrFrame.IsVisible = false;
        InfoFrame.IsVisible = true;

        InfoLabel.Text = paymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase)
            ? "Arrival confirmed. Tap Done Parking when ready to wait for cash payment confirmation."
            : "Arrival confirmed. Tap Done Parking to open GCash payment and then wait for admin confirmation.";

        HideAllActionButtons();
        DoneParkingButton.IsVisible = true;
    }

    private void ApplyWaitingState(string paymentMethod)
    {
        QrFrame.IsVisible = false;
        InfoFrame.IsVisible = true;

        InfoLabel.Text = paymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase)
            ? "Waiting for cash payment confirmation from the location admin..."
            : "Waiting for GCash payment confirmation from the location admin...";

        HideAllActionButtons();
    }

    private void ApplyDefaultState(ActiveParkingModel parking)
    {
        string qrValue = parking.ReservationReference ?? string.Empty;

        QrSectionTitleLabel.Text = "QR Code";
        QrTextLabel.Text = string.IsNullOrWhiteSpace(qrValue) ? "No QR available" : qrValue;
        QrImage.Source = string.IsNullOrWhiteSpace(qrValue)
            ? null
            : GenerateQr(qrValue);

        GenerateQrButton.IsVisible = !string.IsNullOrWhiteSpace(qrValue);
        NavigateButton.IsVisible = false;
        DoneParkingButton.IsVisible = false;
        QrFrame.IsVisible = true;
        InfoFrame.IsVisible = false;
    }

    private async void Navigate_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_activeParking == null)
                return;

            if (!_activeParking.Status.Equals("Reserved", StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert("Info", "Navigation is only available before arrival.", "OK");
                return;
            }

            if (_activeParking.Latitude == 0 || _activeParking.Longitude == 0)
            {
                await DisplayAlert("Info", "Location coordinates are not available.", "OK");
                return;
            }

            StopAutoRefresh();

            _navigationState.StartNavigation(
                _activeParking.ParkingLocationName,
                _activeParking.Latitude,
                _activeParking.Longitude);

            string name = Uri.EscapeDataString(_activeParking.ParkingLocationName ?? "Destination");

            await Shell.Current.GoToAsync(
                $"{nameof(NavigationMapPage)}" +
                $"?destLat={_activeParking.Latitude.ToString(CultureInfo.InvariantCulture)}" +
                $"&destLng={_activeParking.Longitude.ToString(CultureInfo.InvariantCulture)}" +
                $"&destName={name}" +
                $"&reservationId={_activeParking.ReservationId}");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void DoneParking_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_activeParking == null)
                return;

            bool confirm = await DisplayAlert(
                "Done Parking",
                "Are you sure you want to proceed?",
                "Yes",
                "No");

            if (!confirm)
                return;

            _isBusy = true;
            DoneParkingButton.IsEnabled = false;

            string paymentMethod = ResolvePaymentMethod(_activeParking);

            var result = await _apiService.DoneParkingAsync(_activeParking.ReservationId);

            if (result?.Success != true)
            {
                await DisplayAlert("Error", result?.Message ?? "Failed to continue checkout.", "OK");
                return;
            }

            if (paymentMethod.Equals("GCash", StringComparison.OrdinalIgnoreCase))
            {
                var payment = await _apiService.CreatePaymentRequestAsync(_activeParking.ReservationId);

                if (payment != null && !string.IsNullOrWhiteSpace(payment.ExternalPaymentUrl))
                    await Launcher.Default.OpenAsync(payment.ExternalPaymentUrl);
            }

            await Shell.Current.GoToAsync(
                $"{nameof(WaitingPaymentConfirmationPage)}?reservationId={_activeParking.ReservationId}");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            DoneParkingButton.IsEnabled = true;
            _isBusy = false;
        }
    }

    private async void GenerateQr_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_activeParking == null)
            {
                await DisplayAlert("Info", "No active parking found.", "OK");
                return;
            }

            string qrValue = _activeParking.ReservationReference ?? string.Empty;

            if (string.IsNullOrWhiteSpace(qrValue))
            {
                await DisplayAlert("Info", "No QR value available to generate.", "OK");
                return;
            }

            QrImage.Source = GenerateQr(qrValue);
            await DisplayAlert("Success", "QR generated successfully.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void ResetUi()
    {
        LocationLabel.Text = string.Empty;
        SlotLabel.Text = string.Empty;
        StatusLabel.Text = string.Empty;
        ReferenceLabel.Text = string.Empty;
        PaymentLabel.Text = string.Empty;
        PaymentMethodLabel.Text = string.Empty;
        AmountLabel.Text = string.Empty;

        InfoLabel.Text = string.Empty;

        HideAllActionButtons();
        QrFrame.IsVisible = false;
        InfoFrame.IsVisible = false;

        _activeParking = null;
    }

    private string ResolvePaymentMethod(ActiveParkingModel parking)
    {
        if (!string.IsNullOrWhiteSpace(parking.PaymentMethod))
            return parking.PaymentMethod;

        return Preferences.Get($"{PaymentMethodPrefix}{parking.ReservationId}", "Cash");
    }

    private void SavePaymentMethod(int reservationId, string paymentMethod)
    {
        Preferences.Set($"{PaymentMethodPrefix}{reservationId}", paymentMethod);
    }

    private void SaveArrivalState(int reservationId, bool hasArrived)
    {
        Preferences.Set($"{ArrivedPrefix}{reservationId}", hasArrived);
    }

    private bool HasArrived(int reservationId)
    {
        return Preferences.Get($"{ArrivedPrefix}{reservationId}", false);
    }

    private bool ResolveHasArrived(ActiveParkingModel parking)
    {
        return parking.HasArrived || HasArrived(parking.ReservationId);
    }

    private ImageSource GenerateQr(string value)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions
            {
                Height = 300,
                Width = 300,
                Margin = 1
            }
        };

        var pixelData = writer.Write(value);

        return ImageSource.FromStream(() =>
        {
            var stream = new MemoryStream();
            var bitmap = new SkiaSharp.SKBitmap(pixelData.Width, pixelData.Height);

            for (int y = 0; y < pixelData.Height; y++)
            {
                for (int x = 0; x < pixelData.Width; x++)
                {
                    int index = (y * pixelData.Width + x) * 4;

                    bitmap.SetPixel(x, y, new SkiaSharp.SKColor(
                        pixelData.Pixels[index],
                        pixelData.Pixels[index + 1],
                        pixelData.Pixels[index + 2],
                        pixelData.Pixels[index + 3]));
                }
            }

            using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);

            data.SaveTo(stream);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        });
    }

    private async void ShowArrivalQr_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_activeParking == null)
                return;

            string qrValue = _activeParking.ReservationReference ?? string.Empty;

            if (string.IsNullOrWhiteSpace(qrValue))
            {
                await DisplayAlert("Info", "No arrival QR available.", "OK");
                return;
            }

            StopAutoRefresh();

            await Navigation.PushModalAsync(new ArrivalQrPopupPage(qrValue));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void HideAllActionButtons()
    {
        NavigateButton.IsVisible = false;
        ShowArrivalQrButton.IsVisible = false;
        DoneParkingButton.IsVisible = false;
    }
}