using Newtonsoft.Json;
using RealTimeParkingApp.Config;
using RealTimeParkingApp.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace RealTimeParkingApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public string Token { get; private set; } = "";

        public ApiService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(ApiConfig.BaseUrl)
            };

            RestoreTokenFromPreferences();
        }

        public void RestoreTokenFromPreferences()
        {
            var token = Preferences.Get("jwt_token", string.Empty);

            if (!string.IsNullOrWhiteSpace(token))
            {
                Token = token;
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                Token = "";
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        public void Logout()
        {
            Token = "";
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        public async Task<LoginResponse?> Login(string usernameOrEmail, string password)
        {
            try
            {
                var loginData = new
                {
                    usernameOrEmail,
                    password
                };

                var jsonData = JsonConvert.SerializeObject(loginData);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("user/login", content);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return null;

                var result = JsonConvert.DeserializeObject<LoginResponse>(responseText);

                if (result != null)
                {
                    result.IsSuccess = true;
                    Token = result.Token ?? "";

                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", Token);

                    Preferences.Set("jwt_token", Token);
                    Preferences.Set("user_role", result.Role ?? "");
                    Preferences.Set("username", result.Username ?? "");
                    Preferences.Set("user_id", result.UserId);
                    Preferences.Set("email", result.Email ?? "");
                    Preferences.Set("parking_location_id", result.ParkingLocationId ?? 0);
                    Preferences.Set("parking_location_name", result.ParkingLocationName ?? "");
                }

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login error: {ex}");
                return null;
            }
        }

        public async Task<bool> Register(User user)
        {
            try
            {
                var json = JsonConvert.SerializeObject(user);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("user/register", content);

                if (!response.IsSuccessStatusCode)
                    return false;

                var responseText = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<LoginResponse>(responseText);

                if (result != null && !string.IsNullOrWhiteSpace(result.Token))
                {
                    Token = result.Token;
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", Token);

                    Preferences.Set("jwt_token", Token);
                    Preferences.Set("user_role", result.Role ?? "");
                    Preferences.Set("username", result.Username ?? "");
                    Preferences.Set("user_id", result.UserId);
                    Preferences.Set("email", result.Email ?? "");
                    Preferences.Set("parking_location_id", result.ParkingLocationId ?? 0);
                    Preferences.Set("parking_location_name", result.ParkingLocationName ?? "");
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Register Error: {ex}");
                return false;
            }
        }

        public async Task<List<User>> GetUsers()
        {
            try
            {
                var response = await _httpClient.GetAsync("user");

                if (!response.IsSuccessStatusCode)
                    return new List<User>();

                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<User>>(json) ?? new List<User>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetUsers error: {ex}");
                return new List<User>();
            }
        }

        public async Task<bool> AddUser(User user)
        {
            try
            {
                var content = new StringContent(
                    JsonConvert.SerializeObject(user),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("user", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddUser error: {ex}");
                return false;
            }
        }

        public async Task<string> GetParkingLocations()
        {
            try
            {
                var response = await _httpClient.GetAsync("admin/parking-locations");

                if (!response.IsSuccessStatusCode)
                    return "";

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetParkingLocations error: {ex}");
                return "";
            }
        }

        public async Task<LocationAdminDashboardModel?> GetLocationAdminDashboardAsync()
        {
            try
            {
                RestoreTokenFromPreferences();

                var response = await _httpClient.GetAsync("admin/my-location-dashboard");
                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<LocationAdminDashboardModel>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetLocationAdminDashboardAsync error: {ex}");
                return null;
            }
        }

        public async Task<List<ParkingSlotModel>> GetMyLocationSlotsAsync()
        {
            try
            {
                RestoreTokenFromPreferences();

                var response = await _httpClient.GetAsync("admin/my-location-slots");
                var json = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"Slots status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Slots json: {json}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API Error: {response.StatusCode} - {json}");
                }

                return JsonConvert.DeserializeObject<List<ParkingSlotModel>>(json)
                       ?? new List<ParkingSlotModel>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetMyLocationSlotsAsync error: {ex}");
                throw;
            }
        }

        public async Task<bool> CreateLocationAdminAsync(
                string username,
                string firstName,
                string lastName,
                string email,
                string password,
                int parkingLocationId)
        {   
            try
            {
                RestoreTokenFromPreferences();

                var data = new
                {
                    username,
                    firstName,
                    lastName,
                    email,
                    password,
                    parkingLocationId
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(data),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("admin/create-location-admin", content);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateLocationAdmin error: {ex}");
                return false;
            }
        }

        public async Task<ActiveReservationArrivalModel?> GetMyActiveReservationForArrivalAsync()
        {
            try
            {
                RestoreTokenFromPreferences();

                var response = await _httpClient.GetAsync("reservations/my-active-arrival");
                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ActiveReservationArrivalModel>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetMyActiveReservationForArrivalAsync error: {ex}");
                return null;
            }
        }

        public async Task<ArrivalResultModel?> CheckArrivalByLocationAsync(int reservationId, double latitude, double longitude)
        {
            try
            {
                RestoreTokenFromPreferences();

                var data = new
                {
                    reservationId,
                    latitude,
                    longitude
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(data),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("reservations/arrival/by-location", content);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new ArrivalResultModel
                    {
                        Success = false,
                        Message = string.IsNullOrWhiteSpace(json) ? "Location arrival failed." : json
                    };

                return JsonConvert.DeserializeObject<ArrivalResultModel>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckArrivalByLocationAsync error: {ex}");
                return new ArrivalResultModel
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ArrivalResultModel?> CheckArrivalByQrAsync(int reservationId, string qrCodeValue)
        {
            try
            {
                RestoreTokenFromPreferences();

                var data = new
                {
                    reservationId,
                    qrCodeValue
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(data),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("reservations/arrival/by-qrcode", content);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new ArrivalResultModel
                    {
                        Success = false,
                        Message = string.IsNullOrWhiteSpace(json) ? "QR arrival failed." : json
                    };

                return JsonConvert.DeserializeObject<ArrivalResultModel>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckArrivalByQrAsync error: {ex}");
                return new ArrivalResultModel
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ArrivalResultModel?> ProcessSimulatedPaymentAsync(SimulatedPaymentRequestModel model)
        {
            try
            {
                RestoreTokenFromPreferences();

                var content = new StringContent(
                    JsonConvert.SerializeObject(model),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("payments/simulated", content);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new ArrivalResultModel
                    {
                        Success = false,
                        Message = string.IsNullOrWhiteSpace(json) ? "Payment failed." : json
                    };

                return JsonConvert.DeserializeObject<ArrivalResultModel>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ProcessSimulatedPaymentAsync error: {ex}");
                return new ArrivalResultModel
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<List<ManualArrivalReservationModel>> GetLocationManualArrivalReservationsAsync()
        {
            try
            {
                RestoreTokenFromPreferences();

                var response = await _httpClient.GetAsync("admin/manual-arrival-reservations");
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new List<ManualArrivalReservationModel>();

                return JsonConvert.DeserializeObject<List<ManualArrivalReservationModel>>(json)
                       ?? new List<ManualArrivalReservationModel>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetLocationManualArrivalReservationsAsync error: {ex}");
                return new List<ManualArrivalReservationModel>();
            }
        }

        public async Task<ArrivalResultModel?> ConfirmManualArrivalAsync(int reservationId)
        {
            try
            {
                RestoreTokenFromPreferences();

                var content = new StringContent(
                    JsonConvert.SerializeObject(new { reservationId }),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("admin/manual-arrival-check", content);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new ArrivalResultModel
                    {
                        Success = false,
                        Message = string.IsNullOrWhiteSpace(json) ? "Manual check failed." : json
                    };

                return JsonConvert.DeserializeObject<ArrivalResultModel>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ConfirmManualArrivalAsync error: {ex}");
                return new ArrivalResultModel
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ActiveReservation?> GetActiveReservationAsync(int userId)
        {
            try
            {
                RestoreTokenFromPreferences();

                var response = await _httpClient.GetAsync($"Reservations/user/{userId}/active");
                var json = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"GetActiveReservation status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"GetActiveReservation body: {json}");

                if (!response.IsSuccessStatusCode)
                    return null;

                return JsonConvert.DeserializeObject<ActiveReservation>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetActiveReservation error: {ex}");
                return null;
            }
        }

        public async Task<ActiveParkingModel?> GetMyActiveParkingAsync()
        {
            try
            {
                RestoreTokenFromPreferences();

                var response = await _httpClient.GetAsync("reservations/my-active");
                var json = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"GetMyActiveParkingAsync status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"GetMyActiveParkingAsync body: {json}");

                if (!response.IsSuccessStatusCode)
                    return null;

                return JsonConvert.DeserializeObject<ActiveParkingModel>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetMyActiveParkingAsync error: {ex}");
                return null;
            }
        }

        public async Task<SimpleActionResult?> DoneParkingAsync(int reservationId)
        {
            try
            {
                RestoreTokenFromPreferences();

                var content = new StringContent(
                    JsonConvert.SerializeObject(new { reservationId }),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("reservations/done-parking", content);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new SimpleActionResult { Success = false, Message = json };

                return JsonConvert.DeserializeObject<SimpleActionResult>(json)
                       ?? new SimpleActionResult { Success = true, Message = "Done parking requested." };
            }
            catch (Exception ex)
            {
                return new SimpleActionResult { Success = false, Message = ex.Message };
            }
        }

        public async Task<SlotDetailsModel?> GetAdminSlotDetailsAsync(int slotId)
        {
            try
            {
                RestoreTokenFromPreferences();

                var response = await _httpClient.GetAsync($"reservations/admin/slot-details/{slotId}");
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return null;

                return JsonConvert.DeserializeObject<SlotDetailsModel>(json);
            }
            catch
            {
                return null;
            }
        }

        public async Task<SimpleActionResult?> ManualArriveAsync(int parkingSlotId)
        {
            try
            {
                RestoreTokenFromPreferences();

                var content = new StringContent(
                    JsonConvert.SerializeObject(new { parkingSlotId }),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("reservations/admin/manual-arrive", content);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new SimpleActionResult { Success = false, Message = json };

                return JsonConvert.DeserializeObject<SimpleActionResult>(json)
                       ?? new SimpleActionResult { Success = true, Message = "Manual arrival completed." };
            }
            catch (Exception ex)
            {
                return new SimpleActionResult { Success = false, Message = ex.Message };
            }
        }

        public async Task<SimpleActionResult?> ManualCheckoutAsync(int parkingSlotId)
        {
            try
            {
                RestoreTokenFromPreferences();

                var content = new StringContent(
                    JsonConvert.SerializeObject(new { parkingSlotId }),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("reservations/admin/manual-checkout", content);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new SimpleActionResult { Success = false, Message = json };

                return JsonConvert.DeserializeObject<SimpleActionResult>(json)
                       ?? new SimpleActionResult { Success = true, Message = "Manual checkout completed." };
            }
            catch (Exception ex)
            {
                return new SimpleActionResult { Success = false, Message = ex.Message };
            }
        }

        public async Task<SimpleActionResult?> ScanArrivalAsync(string reference)
        {
            try
            {
                RestoreTokenFromPreferences();

                var content = new StringContent(
                    JsonConvert.SerializeObject(new { reference }),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("reservations/admin/scan-arrival", content);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new SimpleActionResult { Success = false, Message = json };

                return JsonConvert.DeserializeObject<SimpleActionResult>(json)
                       ?? new SimpleActionResult { Success = true, Message = "Arrival scanned." };
            }
            catch (Exception ex)
            {
                return new SimpleActionResult { Success = false, Message = ex.Message };
            }
        }

        public async Task<SimpleActionResult?> ScanPaymentAsync(string reference)
        {
            try
            {
                RestoreTokenFromPreferences();

                var content = new StringContent(
                    JsonConvert.SerializeObject(new { reference }),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("reservations/admin/scan-payment", content);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new SimpleActionResult { Success = false, Message = json };

                return JsonConvert.DeserializeObject<SimpleActionResult>(json)
                       ?? new SimpleActionResult { Success = true, Message = "Payment scanned." };
            }
            catch (Exception ex)
            {
                return new SimpleActionResult { Success = false, Message = ex.Message };
            }
        }

        public async Task<bool> ReserveSlotAsync(int parkingSlotId)
        {
            try
            {
                RestoreTokenFromPreferences();

                var content = new StringContent(
                    JsonConvert.SerializeObject(new { parkingSlotId }),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("reservations/create", content);
                var json = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"ReserveSlotAsync status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"ReserveSlotAsync body: {json}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ReserveSlotAsync error: {ex}");
                return false;
            }
        }

        public async Task<List<ParkingHistoryItem>> GetParkingHistoryAsync()
        {
            try
            {
                var token = Preferences.Get("jwt_token", string.Empty);

                if (string.IsNullOrWhiteSpace(token))
                    return new List<ParkingHistoryItem>();

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync("reservations/my-history");

                if (!response.IsSuccessStatusCode)
                    return new List<ParkingHistoryItem>();

                var json = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<List<ParkingHistoryItem>>(json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<ParkingHistoryItem>();
            }
            catch
            {
                return new List<ParkingHistoryItem>();
            }
        }
    }
}