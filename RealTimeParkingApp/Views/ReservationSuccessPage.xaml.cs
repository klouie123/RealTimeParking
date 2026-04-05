using RealTimeParkingApp.Services;

namespace RealTimeParkingApp.Views;

[QueryProperty(nameof(ReservationIdText), "reservationId")]
public partial class ReservationSuccessPage : ContentPage
{
    private readonly ApiService _apiService;
    private int _reservationId;

    public string ReservationIdText
    {
        get => _reservationId.ToString();
        set => int.TryParse(value, out _reservationId);
    }

    public ReservationSuccessPage()
    {
        InitializeComponent();
        _apiService = App.Services.GetRequiredService<ApiService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Shell.Current.GoToAsync("//UserDashboardPage");
        });

        return true;
    }

    private async Task LoadAsync()
    {
        try
        {
            var activeParking = await _apiService.GetMyActiveParkingAsync();

            if (activeParking != null)
            {
                LocationLabel.Text = $"Location: {activeParking.ParkingLocationName}";
                SlotLabel.Text = $"Slot: {activeParking.SlotCode}";
                ReservationReferenceLabel.Text = $"Reservation Ref: {activeParking.ReservationReference}";
                PaymentReferenceLabel.Text = $"Payment Ref: {activeParking.PaymentReference ?? "N/A"}";
                PaymentMethodLabel.Text = $"Payment Method: {activeParking.PaymentMethod ?? "N/A"}";
                AmountLabel.Text = $"Amount Paid: ₱{activeParking.PaymentAmount:F2}";
                StatusLabel.Text = $"Status: {activeParking.PaymentStatus ?? activeParking.Status}";
                return;
            }

            LocationLabel.Text = "Location: Payment completed";
            SlotLabel.Text = "Slot: Completed";
            ReservationReferenceLabel.Text = $"Reservation Id: {_reservationId}";
            PaymentReferenceLabel.Text = "Payment Ref: Confirmed";
            PaymentMethodLabel.Text = "Payment Method: Completed";
            AmountLabel.Text = "Amount Paid: Confirmed";
            StatusLabel.Text = "Status: Paid";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void Done_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//UserDashboardPage");
    }
}