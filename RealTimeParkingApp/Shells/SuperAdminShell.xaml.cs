using RealTimeParkingApp.Views;

namespace RealTimeParkingApp.Shells;

public partial class SuperAdminShell : Shell
{
    public SuperAdminShell()
    {
        InitializeComponent();

        SuperAdminDashboardContent.ContentTemplate =
            new DataTemplate(() => App.Services.GetRequiredService<SuperAdminDashboardPage>());

        ProfileContent.ContentTemplate =
            new DataTemplate(() => App.Services.GetRequiredService<ProfilePage>());
    }
}