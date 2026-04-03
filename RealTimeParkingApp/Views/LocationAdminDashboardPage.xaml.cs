using RealTimeParkingApp.Services;

namespace RealTimeParkingApp.Views;

public partial class LocationAdminDashboardPage : ContentPage
{
    private readonly ApiService _apiService;
    private CancellationTokenSource? _refreshCts;

    public LocationAdminDashboardPage()
    {
        InitializeComponent();
        _apiService = App.Services.GetRequiredService<ApiService>();

        var username = Preferences.Get("username", "User");
        UsernameLabel.Text = $"Logged in as: {username}";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadDashboardAsync();
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
                    await Task.Delay(TimeSpan.FromSeconds(5), _refreshCts.Token);

                    if (_refreshCts.Token.IsCancellationRequested)
                        break;

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await LoadDashboardAsync(showError: false);
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

    private async Task LoadDashboardAsync(bool showError = true)
    {
        try
        {
            var dashboard = await _apiService.GetLocationAdminDashboardAsync();

            if (dashboard == null)
            {
                if (showError)
                    await DisplayAlert("Error", "Failed to load dashboard.", "OK");
                return;
            }

            LocationNameLabel.Text = dashboard.ParkingLocationName;
            TotalSlotsLabel.Text = dashboard.TotalSlots.ToString();
            AvailableSlotsLabel.Text = dashboard.AvailableSlots.ToString();
            OccupiedSlotsLabel.Text = dashboard.OccupiedSlots.ToString();
            ReservedSlotsLabel.Text = dashboard.ReservedSlots.ToString();
            ActiveReservationsLabel.Text = dashboard.ActiveReservations.ToString();
        }
        catch (Exception ex)
        {
            if (showError)
                await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void ViewMySlots_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LocationAdminSlotsPage());
    }

    //private async void ManualArrivalCheck_Clicked(object sender, EventArgs e)
    //{
    //    await Shell.Current.GoToAsync(nameof(AdminQrScannerPage) + "?mode=arrival");
    //}   
}