using AccountingApp.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AccountingApp.Services.Core;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private const string TokenKey = "auth_token";
    private const string CompanyIdKey = "company_id";
    private const string UserIdKey = "user_id";

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        LoadSavedCredentials();
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);
    public string? Token { get; private set; }
    public string? UserId { get; private set; }
    public Guid? CompanyId { get; private set; }

    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        try
        {
            var request = new LoginRequest(email, password);
            var response = await _httpClient.PostAsJsonAsync("auth/login", request);

            if (response.IsSuccessStatusCode)
            {
                var currentUser = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (currentUser != null)
                {
                    Token = currentUser.Token;
                    UserId = currentUser.UserId;
                    CompanyId = currentUser.CompanyId;

                    await SecureStorage.SetAsync(TokenKey, Token);
                    await SecureStorage.SetAsync(UserIdKey, UserId);
                    if (CompanyId.HasValue)
                        await SecureStorage.SetAsync(CompanyIdKey, CompanyId.Value.ToString());

                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", Token);

                    return currentUser;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
            return null;
        }
    }

    public async Task LogoutAsync()
    {
        Token = null;
        UserId = null;
        CompanyId = null;

        SecureStorage.Remove(TokenKey);
        SecureStorage.Remove(UserIdKey);
        SecureStorage.Remove(CompanyIdKey);

        _httpClient.DefaultRequestHeaders.Authorization = null;

        await Task.CompletedTask;
    }

    public async Task<bool> ValidateTokenAsync()
    {
        if (string.IsNullOrEmpty(Token))
            return false;

        try
        {
            var response = await _httpClient.GetAsync("auth/validate");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private void LoadSavedCredentials()
    {
        Token = SecureStorage.GetAsync(TokenKey).Result;
        UserId = SecureStorage.GetAsync(UserIdKey).Result;
        var companyIdStr = SecureStorage.GetAsync(CompanyIdKey).Result;

        if (Guid.TryParse(companyIdStr, out var companyId))
            CompanyId = companyId;

        if (!string.IsNullOrEmpty(Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", Token);
        }
    }
}