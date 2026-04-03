using RealTimeParkingApp.Services;

namespace RealTimeParkingApp.Views;

[QueryProperty(nameof(Mode), "mode")]
[QueryProperty(nameof(SlotId), "slotId")]
public partial class AdminQrScannerPage : ContentPage
{
    private readonly ApiService _apiService;
    private bool _isProcessing;

    public string Mode { get; set; } = string.Empty;
    public string SlotId { get; set; } = string.Empty;

    public AdminQrScannerPage()
    {
        InitializeComponent();
        _apiService = App.Services.GetRequiredService<ApiService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var status = await Permissions.RequestAsync<Permissions.Camera>();

        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Permission", "Camera permission is required.", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    private async void CameraView_BarcodesDetected(object sender, ZXing.Net.Maui.BarcodeDetectionEventArgs e)
    {
        if (_isProcessing)
            return;

        var resultText = e.Results?.FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(resultText))
            return;

        _isProcessing = true;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                Models.SimpleActionResult? result;

                if (Mode == "arrival")
                    result = await _apiService.ScanArrivalAsync(resultText);
                else
                    result = await _apiService.ScanPaymentAsync(resultText);

                await DisplayAlert(result?.Success == true ? "Success" : "Error",
                    result?.Message ?? "Scan failed.", "OK");

                await Shell.Current.GoToAsync("..");
            }
            finally
            {
                _isProcessing = false;
            }
        });
    }
}