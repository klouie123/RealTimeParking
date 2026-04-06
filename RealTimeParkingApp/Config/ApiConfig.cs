using Microsoft.Maui.Devices;

namespace RealTimeParkingApp.Config
{
    public static class ApiConfig
    {
        public static string BaseUrl
        {
            get
            {
#if ANDROID
                bool useNgrok = true;

                // PRIORITY 1: Emulator
                if (DeviceInfo.DeviceType == DeviceType.Virtual)
                {
                    return "http://10.0.2.2:6060/api/";
                }

                // PRIORITY 2: ngrok (real device)
                if (useNgrok)
                {
                    return "https://terresa-nonenteric-unblushingly.ngrok-free.dev/api/";
                }

                // PRIORITY 3: local IP fallback
                return "http://10.20.255.219:6060/api/";
                //return "http://192.168.1.20:6060/api/";

#else
                return "http://localhost:6060/api/";
#endif
            }
        }
    }
}