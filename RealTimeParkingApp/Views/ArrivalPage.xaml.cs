using RealTimeParkingApp.Models;
using RealTimeParkingApp.Services;

namespace RealTimeParkingApp.Views;

public partial class ArrivalPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly LocationService _locationService;
    private ActiveReservationArrivalModel? _reservation;

    public ArrivalPage()
    {
        InitializeComponent();
        _apiService = App.Services.GetRequiredService<ApiService>();
        _locationService = App.Services.GetRequiredService<LocationService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadReservationAsync();
    }

    private async Task LoadReservationAsync()
    {
        _reservation = await _apiService.GetMyActiveReservationForArrivalAsync();

        if (_reservation == null)
        {
            await DisplayAlert("Info", "No active reservation found.", "OK");
            await Navigation.PopAsync();
            return;
        }

        LocationLabel.Text = _reservation.ParkingLocationName;
        SlotLabel.Text = $"Slot: {_reservation.SlotCode}";
        StatusLabel.Text = $"Status: {_reservation.Status}";
        PaymentStatusLabel.Text = _reservation.IsPaid
            ? "Payment: Paid"
            : $"Payment: Unpaid{(_reservation.Amount.HasValue ? $" - ₱{_reservation.Amount.Value:F2}" : "")}";
    }

    private async void ArriveByLocation_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_reservation == null)
                return;

            var location = await _locationService.GetCurrentLocationAsync();

            if (location == null)
            {
                await DisplayAlert("Error", "Unable to get current location.", "OK");
                return;
            }

            var result = await _apiService.CheckArrivalByLocationAsync(
                _reservation.Id,
                location.Latitude,
                location.Longitude);

            if (result == null)
            {
                await DisplayAlert("Error", "Arrival failed.", "OK");
                return;
            }

            await DisplayAlert(result.Success ? "Success" : "Error", result.Message, "OK");

            if (result.Success)
                await LoadReservationAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void ArriveByQr_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_reservation == null)
                return;

            var qrValue = QrCodeEntry.Text?.Trim();

            if (string.IsNullOrWhiteSpace(qrValue))
            {
                await DisplayAlert("Validation", "Please enter QR code value.", "OK");
                return;
            }

            var result = await _apiService.CheckArrivalByQrAsync(_reservation.Id, qrValue);

            if (result == null)
            {
                await DisplayAlert("Error", "QR arrival failed.", "OK");
                return;
            }

            await DisplayAlert(result.Success ? "Success" : "Error", result.Message, "OK");

            if (result.Success)
                await LoadReservationAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void SimulatedPayment_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_reservation == null)
                return;

            if (!decimal.TryParse(AmountEntry.Text, out var amount))
            {
                await DisplayAlert("Validation", "Please enter a valid amount.", "OK");
                return;
            }

            var reference = ReferenceEntry.Text?.Trim();

            if (string.IsNullOrWhiteSpace(reference))
            {
                await DisplayAlert("Validation", "Please enter a reference number.", "OK");
                return;
            }

            var request = new SimulatedPaymentRequestModel
            {
                ReservationId = _reservation.Id,
                Amount = amount,
                ReferenceNumber = reference,
                PaymentMethod = "Simulated"
            };

            var result = await _apiService.ProcessSimulatedPaymentAsync(request);

            if (result == null)
            {
                await DisplayAlert("Error", "Payment failed.", "OK");
                return;
            }

            await DisplayAlert(result.Success ? "Success" : "Error", result.Message, "OK");

            if (result.Success)
                await LoadReservationAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}