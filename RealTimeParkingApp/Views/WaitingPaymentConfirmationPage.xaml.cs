using RealTimeParkingApp.Services;

namespace RealTimeParkingApp.Views;

[QueryProperty(nameof(ReservationIdText), "reservationId")]
public partial class WaitingPaymentConfirmationPage : ContentPage
{
    private readonly ApiService _apiService;
    private CancellationTokenSource? _refreshCts;
    private int _reservationId;
    private bool _navigated;
    private bool _isLoading;

    public string ReservationIdText
    {
        get => _reservationId.ToString();
        set => int.TryParse(value, out _reservationId);
    }

    public WaitingPaymentConfirmationPage()
    {
        InitializeComponent();
        _apiService = App.Services.GetRequiredService<ApiService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _navigated = false;
        await LoadAsync();
        StartAutoRefresh();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopAutoRefresh();
    }

    protected override bool OnBackButtonPressed()
    {
        return true;
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

                    if (_refreshCts.Token.IsCancellationRequested || _navigated || _isLoading)
                        continue;

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        if (!_navigated && !_isLoading)
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

    private async Task LoadAsync()
    {
        if (_isLoading || _navigated)
            return;

        try
        {
            _isLoading = true;

            var activeParking = await _apiService.GetMyActiveParkingAsync();

            if (activeParking == null)
            {
                await NavigateToSuccessAsync(_reservationId);
                return;
            }

            string paymentMethod = activeParking.PaymentMethod ?? "N/A";

            LocationLabel.Text = activeParking.ParkingLocationName;
            SlotLabel.Text = $"Slot: {activeParking.SlotCode}";
            StatusLabel.Text = $"Status: {activeParking.Status}";
            PaymentMethodLabel.Text = $"Payment Method: {paymentMethod}";
            AmountLabel.Text = $"Amount: ₱{activeParking.PaymentAmount:F2}";

            MessageLabel.Text = paymentMethod.Equals("GCash", StringComparison.OrdinalIgnoreCase)
                ? "GCash payment opened. Please wait while the location admin confirms your payment."
                : "Please wait while the location admin confirms your cash payment.";

            if (activeParking.PaymentStatus?.Equals("Paid", StringComparison.OrdinalIgnoreCase) == true ||
                activeParking.Status?.Equals("Completed", StringComparison.OrdinalIgnoreCase) == true ||
                activeParking.Status?.Equals("Paid", StringComparison.OrdinalIgnoreCase) == true)
            {
                await NavigateToSuccessAsync(activeParking.ReservationId);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task NavigateToSuccessAsync(int reservationId)
    {
        if (_navigated)
            return;

        _navigated = true;
        StopAutoRefresh();

        await Shell.Current.GoToAsync($"{nameof(ReservationSuccessPage)}?reservationId={reservationId}");
    }
}