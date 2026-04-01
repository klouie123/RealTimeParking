using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using RealTimeParkingApp.Config;
using RealTimeParkingApp.Models;

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

                return response.IsSuccessStatusCode;
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
                var response = await _httpClient.GetAsync("parking");

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
    }
}