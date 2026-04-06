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
            var history = await _apiService.GetParkingHistoryAsync();

            var latest = history?.FirstOrDefault();

            if (latest != null)
            {
                LocationLabel.Text = $"Location: {latest.ParkingLocationName}";
                SlotLabel.Text = $"Slot: {latest.SlotCode}";
                ReservationReferenceLabel.Text = $"Reservation Ref: {latest.ReservationReference}";
                PaymentReferenceLabel.Text = $"Payment Ref: {latest.PaymentReference ?? "N/A"}";
                PaymentMethodLabel.Text = $"Payment Method: {latest.PaymentMethod}";
                AmountLabel.Text = $"Amount Paid: ₱{latest.PaymentAmount:F2}";
                StatusLabel.Text = $"Status: {latest.PaymentStatus}";
            }
            else
            {
                LocationLabel.Text = "Location: N/A";
                SlotLabel.Text = "Slot: N/A";
                ReservationReferenceLabel.Text = "Reservation Ref: N/A";
                PaymentReferenceLabel.Text = "Payment Ref: N/A";
                PaymentMethodLabel.Text = "Payment Method: N/A";
                AmountLabel.Text = "Amount Paid: N/A";
                StatusLabel.Text = "Status: Completed";
            }
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