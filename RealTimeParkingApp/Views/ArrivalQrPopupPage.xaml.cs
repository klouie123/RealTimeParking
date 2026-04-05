using ZXing;
using ZXing.Common;

namespace RealTimeParkingApp.Views;

public partial class ArrivalQrPopupPage : ContentPage
{
    private readonly string _qrValue;

    public ArrivalQrPopupPage(string qrValue)
    {
        InitializeComponent();
        _qrValue = qrValue;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        QrValueLabel.Text = _qrValue;
        QrImage.Source = GenerateQr(_qrValue);
    }

    private async void Close_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Navigation.PopModalAsync();
        });

        return true;
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