using System.Globalization;
using RealTimeParkingApp.Models;
using RealTimeParkingApp.Services;
using ZXing;
using ZXing.Common;
using Microsoft.Maui.Graphics;

namespace RealTimeParkingApp.Views;

public partial class MyActiveParkingPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly NavigationStateService _navigationState;
    private ActiveParkingModel? _activeParking;
    private CancellationTokenSource? _refreshCts;
    private bool _isBusy;
    private bool _noActiveParkingHandled;

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

    private async Task LoadAsync(bool showNoActiveMessage = true)
    {
        try
        {
            var latest = await _apiService.GetMyActiveParkingAsync();

            if (latest == null)
            {
                // clear navigation state kapag tapos na ang cycle
                _navigationState.Clear();

                if (showNoActiveMessage && !_noActiveParkingHandled)
                {
                    _noActiveParkingHandled = true;
                    StopAutoRefresh();

                    await DisplayAlert("Parking Completed", "Your parking session has ended.", "OK");

                    if (Shell.Current != null)
                    {
                        await Shell.Current.GoToAsync("//UserDashboardPage");
                    }
                    else
                    {
                        await Navigation.PopAsync();
                    }
                }

                return;
            }

            _activeParking = latest;

            LocationLabel.Text = _activeParking.ParkingLocationName;
            SlotLabel.Text = $"Slot: {_activeParking.SlotCode}";
            StatusLabel.Text = $"Status: {_activeParking.Status}";
            ReferenceLabel.Text = $"Reservation Ref: {_activeParking.ReservationReference}";
            PaymentLabel.Text = $"Payment Ref: {_activeParking.PaymentReference ?? "Not ready"}";

            // default hide muna
            NavigateButton.IsVisible = false;
            DoneParkingButton.IsVisible = false;

            if (_activeParking.Status == "Reserved")
            {
                var qrValue = _activeParking.ReservationReference;

                QrTextLabel.Text = qrValue;
                QrImage.Source = GenerateQr(qrValue);

                NavigateButton.IsVisible =
                    _activeParking.Latitude != 0 &&
                    _activeParking.Longitude != 0;
            }
            else if (_activeParking.Status == "Occupied")
            {
                var qrValue = _activeParking.PaymentReference ?? "";

                QrTextLabel.Text = string.IsNullOrWhiteSpace(qrValue)
                    ? "Tap Done Parking first"
                    : qrValue;

                QrImage.Source = string.IsNullOrWhiteSpace(qrValue)
                    ? null
                    : GenerateQr(qrValue);

                DoneParkingButton.IsVisible = true;

                NavigateButton.IsVisible = false;
                _navigationState.Clear();
            }
            else
            {
                var qrValue = _activeParking.ReservationReference;

                QrTextLabel.Text = qrValue;
                QrImage.Source = GenerateQr(qrValue);

                NavigateButton.IsVisible = false;
                DoneParkingButton.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            if (showNoActiveMessage)
                await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void Navigate_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_activeParking == null)
                return;

            if (_activeParking.Status != "Reserved")
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

            string name = Uri.EscapeDataString(_activeParking.ParkingLocationName);

            await Shell.Current.GoToAsync(
                $"{nameof(NavigationMapPage)}" +
                $"?destLat={_activeParking.Latitude.ToString(CultureInfo.InvariantCulture)}" +
                $"&destLng={_activeParking.Longitude.ToString(CultureInfo.InvariantCulture)}" +
                $"&destName={name}");
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

            _isBusy = true;

            var result = await _apiService.DoneParkingAsync(_activeParking.ReservationId);

            if (result == null)
            {
                await DisplayAlert("Error", "Failed to generate payment reference.", "OK");
                return;
            }

            await DisplayAlert(
                result.Success ? "Success" : "Error",
                result.Message,
                "OK");

            await LoadAsync(false);
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