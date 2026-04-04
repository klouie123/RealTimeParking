using RealTimeParkingApp.DTOs;
using RealTimeParkingApp.Models;
using RealTimeParkingApp.Services;
using System.Globalization;
using ZXing;
using ZXing.Common;

namespace RealTimeParkingApp.Views;

public partial class MyActiveParkingPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly NavigationStateService _navigationState;

    private ActiveParkingModel? _activeParking;
    private PaymentRequestDto? _paymentRequest;

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

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _noActiveParkingHandled = false;

        try
        {
            await LoadAsync();
            StartAutoRefresh();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
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

            if (_activeParking.Status.Equals("Reserved", StringComparison.OrdinalIgnoreCase))
            {
                if (!hasArrived)
                {
                    ApplyBeforeArrivalState(_activeParking);
                }
                else
                {
                    ApplyArrivedState(_activeParking, paymentMethod);
                }
            }
            else if (_activeParking.Status.Equals("Occupied", StringComparison.OrdinalIgnoreCase))
            {
                await ApplyOccupiedStateAsync(_activeParking, paymentMethod);
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

    private void ApplyHeaderState(ActiveParkingModel parking)
    {
        string paymentMethod = ResolvePaymentMethod(parking);

        LocationLabel.Text = parking.ParkingLocationName;
        SlotLabel.Text = $"Slot: {parking.SlotCode}";
        StatusLabel.Text = $"Status: {parking.Status}";
        ReferenceLabel.Text = $"Reservation Ref: {parking.ReservationReference}";
        PaymentLabel.Text = $"Payment Ref: {parking.PaymentReference ?? "Not ready"}";
        PaymentMethodLabel.Text = $"Payment Method: {paymentMethod}";

        NavigateButton.IsVisible = false;
        CashButton.IsVisible = false;
        GeneratePaymentQrButton.IsVisible = false;
        OpenPaymentButton.IsVisible = false;
        DoneParkingButton.IsVisible = false;
        GenerateQrButton.IsVisible = false;
    }

    private void ApplyBeforeArrivalState(ActiveParkingModel parking)
    {
        QrSectionTitleLabel.Text = "Arrival QR";
        QrTextLabel.Text = "Arrival QR will appear once you reach the destination.";
        QrImage.Source = null;

        NavigateButton.IsVisible = parking.Latitude != 0 && parking.Longitude != 0;
        GenerateQrButton.IsVisible = false;
    }

    private void ApplyArrivedState(ActiveParkingModel parking, string paymentMethod)
    {
        string qrValue = parking.ReservationReference ?? string.Empty;

        QrSectionTitleLabel.Text = "Arrival QR";
        QrTextLabel.Text = string.IsNullOrWhiteSpace(qrValue)
            ? "Arrival QR is not available."
            : qrValue;

        QrImage.Source = string.IsNullOrWhiteSpace(qrValue)
            ? null
            : GenerateQr(qrValue);

        GenerateQrButton.IsVisible = !string.IsNullOrWhiteSpace(qrValue);

        CashButton.IsVisible = paymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase);
        GeneratePaymentQrButton.IsVisible = paymentMethod.Equals("GCash", StringComparison.OrdinalIgnoreCase);

        OpenPaymentButton.IsVisible = false;
        DoneParkingButton.IsVisible = false;
        NavigateButton.IsVisible = false;
    }

    private async Task ApplyOccupiedStateAsync(ActiveParkingModel parking, string paymentMethod)
    {
        var qrValue = parking.PaymentReference ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(qrValue) &&
            (_paymentRequest == null || _paymentRequest.PaymentReference != qrValue))
        {
            _paymentRequest = await _apiService.GetPaymentRequestByReservationAsync(parking.ReservationId);
        }

        if (paymentMethod.Equals("GCash", StringComparison.OrdinalIgnoreCase))
        {
            QrSectionTitleLabel.Text = "Payment QR";
            QrTextLabel.Text = string.IsNullOrWhiteSpace(qrValue)
                ? "Payment QR not ready."
                : qrValue;

            QrImage.Source = string.IsNullOrWhiteSpace(qrValue)
                ? null
                : GenerateQr(qrValue);

            GenerateQrButton.IsVisible = !string.IsNullOrWhiteSpace(qrValue);
            OpenPaymentButton.IsVisible =
                !string.IsNullOrWhiteSpace(qrValue) &&
                _paymentRequest != null &&
                !string.IsNullOrWhiteSpace(_paymentRequest.ExternalPaymentUrl);

            DoneParkingButton.IsVisible = true;
        }
        else
        {
            QrSectionTitleLabel.Text = "Cash Payment";
            QrTextLabel.Text = "Please proceed to the location admin for cash payment and checkout.";
            QrImage.Source = null;
            GenerateQrButton.IsVisible = false;
            OpenPaymentButton.IsVisible = false;
            DoneParkingButton.IsVisible = false;
        }

        NavigateButton.IsVisible = false;
        CashButton.IsVisible = false;
        GeneratePaymentQrButton.IsVisible = false;

        _navigationState.Clear();
    }

    private void ApplyDefaultState(ActiveParkingModel parking)
    {
        QrSectionTitleLabel.Text = "QR Code";
        QrTextLabel.Text = parking.ReservationReference ?? "No QR available";
        QrImage.Source = string.IsNullOrWhiteSpace(parking.ReservationReference)
            ? null
            : GenerateQr(parking.ReservationReference);

        GenerateQrButton.IsVisible = !string.IsNullOrWhiteSpace(parking.ReservationReference);
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

    private async void CashButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_activeParking == null)
                return;

            _isBusy = true;
            CashButton.IsEnabled = false;

            var result = await _apiService.DoneParkingAsync(_activeParking.ReservationId);

            if (result?.Success != true)
            {
                await DisplayAlert("Error", result?.Message ?? "Failed to continue cash payment flow.", "OK");
                return;
            }

            _activeParking.Status = "Occupied";
            _activeParking.PaymentMethod = "Cash";
            _activeParking.PaymentStatus = "Unpaid";
            _activeParking.PaymentReference = null;

            SavePaymentMethod(_activeParking.ReservationId, "Cash");

            await ApplyOccupiedStateAsync(_activeParking, "Cash");

            await DisplayAlert("Success", "Cash payment selected. Please proceed to the location admin for checkout.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            CashButton.IsEnabled = true;
            _isBusy = false;
        }
    }

    private async void GeneratePaymentQr_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_activeParking == null)
                return;

            _isBusy = true;
            GeneratePaymentQrButton.IsEnabled = false;

            var payment = await _apiService.CreatePaymentRequestAsync(_activeParking.ReservationId);

            if (payment == null)
            {
                await DisplayAlert("Error", "Failed to create payment request.", "OK");
                return;
            }

            _paymentRequest = payment;

            _activeParking.Status = "Occupied";
            _activeParking.PaymentMethod = "GCash";
            _activeParking.PaymentReference = payment.PaymentReference;
            _activeParking.PaymentStatus = "Pending";

            SavePaymentMethod(_activeParking.ReservationId, "GCash");

            PaymentLabel.Text = $"Payment Ref: {payment.PaymentReference}";

            await ApplyOccupiedStateAsync(_activeParking, "GCash");

            await DisplayAlert(
                "Payment Request Created",
                $"Amount: ₱{payment.Amount:F2}\nReceiver: {payment.MerchantDisplayName}\nGCash No: {payment.MerchantGcashNumber}\nRef: {payment.PaymentReference}",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            GeneratePaymentQrButton.IsEnabled = true;
            _isBusy = false;
        }
    }

    private async void DoneParking_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_activeParking == null)
                return;

            _isBusy = true;
            DoneParkingButton.IsEnabled = false;

            var result = await _apiService.DoneParkingAsync(_activeParking.ReservationId);

            if (result?.Success != true)
            {
                await DisplayAlert("Error", result?.Message ?? "Failed to update parking.", "OK");
                return;
            }

            await DisplayAlert("Success", result.Message ?? "Parking updated.", "OK");

            await LoadAsync(false);

            if (_activeParking == null)
            {
                await Shell.Current.GoToAsync("//UserDashboardPage");
            }
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

    private async void OpenPayment_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_paymentRequest == null || string.IsNullOrWhiteSpace(_paymentRequest.ExternalPaymentUrl))
            {
                await DisplayAlert("Info", "No payment URL is available.", "OK");
                return;
            }

            await Launcher.Default.OpenAsync(_paymentRequest.ExternalPaymentUrl);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
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

            string qrValue = ResolveCurrentQrValue();

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

    private string ResolveCurrentQrValue()
    {
        if (_activeParking == null)
            return string.Empty;

        string paymentMethod = ResolvePaymentMethod(_activeParking);
        bool hasArrived = ResolveHasArrived(_activeParking);

        if (_activeParking.Status.Equals("Occupied", StringComparison.OrdinalIgnoreCase) &&
            paymentMethod.Equals("GCash", StringComparison.OrdinalIgnoreCase))
        {
            return _activeParking.PaymentReference ?? _paymentRequest?.PaymentReference ?? string.Empty;
        }

        if (_activeParking.Status.Equals("Reserved", StringComparison.OrdinalIgnoreCase) && hasArrived)
        {
            return _activeParking.ReservationReference ?? string.Empty;
        }

        return string.Empty;
    }

    private void ResetUi()
    {
        LocationLabel.Text = string.Empty;
        SlotLabel.Text = string.Empty;
        StatusLabel.Text = string.Empty;
        ReferenceLabel.Text = string.Empty;
        PaymentLabel.Text = string.Empty;
        PaymentMethodLabel.Text = string.Empty;

        QrSectionTitleLabel.Text = "QR Code";
        QrTextLabel.Text = string.Empty;
        QrImage.Source = null;

        NavigateButton.IsVisible = false;
        CashButton.IsVisible = false;
        GeneratePaymentQrButton.IsVisible = false;
        OpenPaymentButton.IsVisible = false;
        DoneParkingButton.IsVisible = false;
        GenerateQrButton.IsVisible = false;

        GeneratePaymentQrButton.IsEnabled = true;
        CashButton.IsEnabled = true;
        DoneParkingButton.IsEnabled = true;

        _paymentRequest = null;
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
}