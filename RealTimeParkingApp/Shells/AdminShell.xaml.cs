using RealTimeParkingApp.Views;

namespace RealTimeParkingApp.Shells;

public partial class AdminShell : Shell
{
    public AdminShell()
    {
        InitializeComponent();

        AdminDashboardContent.ContentTemplate =
            new DataTemplate(() => App.Services.GetRequiredService<AdminDashboardPage>());
    }
}