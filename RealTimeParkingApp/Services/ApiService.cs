using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using RealTimeParkingApp.Config;
using RealTimeParkingApp.Models;

namespace RealTimeParkingApp.Services
{
    internal class ApiService
    {
        private readonly HttpClient _httpClient;
        //private string _baseUrl = "http://10.0.2.2:6060/api/";

        public string Token { get; private set; } = "";
        public ApiService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(ApiConfig.BaseUrl)
            };
        }

        // para sa login
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
                    Token = result.Token;
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", Token);
                }

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> Register(User user)
        {
            try
            {
                // Use the HttpClient's BaseAddress + relative path
                var url = "user/register"; // relative path only

                var json = JsonConvert.SerializeObject(user);

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // Optional: log exception
                System.Diagnostics.Debug.WriteLine($"Register Error: {ex.Message}");
                return false;
            }
        }

        // para makakuha ng user (admin)
        public async Task<List<User>> GetUsers()
        {
            var response = await _httpClient.GetAsync("user");
            if (!response.IsSuccessStatusCode) return new List<User>();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<User>>(json);
        }

        //add user
        public async Task<bool> AddUser(User user)
        {
            var content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("user", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<string> GetParkingLocations()
        {
            var response = await _httpClient.GetAsync("parking");

            if (!response.IsSuccessStatusCode)
                return "";

            return await response.Content.ReadAsStringAsync();
        }
    }
}
