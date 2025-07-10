using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Akvila.Launcher.Core.Exceptions;
using Akvila.Web.Api.Domains.Integrations;
using Newtonsoft.Json;
using Splat;

namespace Akvila.Launcher.Core.Services;

public class MicrosoftAuthService : IMicrosoftAuthService {
    private readonly HttpClient _httpClient;
    private readonly IApplicationStateService _applicationStateService;
    private const string Scope = "XboxLive.signin openid offline_access";
    private const string DeviceCodeUrl = "https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode";
    private const string TokenUrl = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";

    public MicrosoftAuthService(HttpClient? httpClient = null, IApplicationStateService? applicationStateService = null) {
        _httpClient = httpClient ?? new HttpClient();
        _applicationStateService = applicationStateService
                                   ?? Locator.Current.GetService<IApplicationStateService>()
                                   ?? throw new ServiceNotFoundException(typeof(IApplicationStateService));
    }

    public async Task<MicrosoftDeviceCode> GetDeviceCodeAsync() {
        if (_applicationStateService.AuthType == null) {
            throw new InvalidOperationException("AuthType is not set in ApplicationStateService");
        }

        var requestData = new List<KeyValuePair<string, string>> {
            new("client_id", _applicationStateService.AuthType.Data),
            new("scope", Scope)
        };

        var content = new FormUrlEncodedContent(requestData);

        var response = await _httpClient.PostAsync(DeviceCodeUrl, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode) {
            throw new HttpRequestException($"Failed to get device code: {response.StatusCode} - {responseContent}");
        }

        var deviceCode = JsonConvert.DeserializeObject<MicrosoftDeviceCode>(responseContent);
        return deviceCode ?? throw new InvalidOperationException("Failed to deserialize device code response");

    }

    public async Task<MicrosoftDeviceToken> GetDeviceToken(string deviceCode) {
        if (_applicationStateService.AuthType == null) {
            throw new InvalidOperationException("AuthType is not set in ApplicationStateService");
        }

        var requestData = new List<KeyValuePair<string, string>> {
            new("grant_type", "urn:ietf:params:oauth:grant-type:device_code"),
            new("client_id", _applicationStateService.AuthType.Data),
            new("device_code", deviceCode)
        };

        var content = new FormUrlEncodedContent(requestData);

        var response = await _httpClient.PostAsync(TokenUrl, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) {
            throw new HttpRequestException($"Failed to get device token: {response.StatusCode} - {responseContent}");
        }

        var deviceToken = JsonConvert.DeserializeObject<MicrosoftDeviceToken>(responseContent);
        return deviceToken ?? throw new InvalidOperationException("Failed to deserialize device token response");
    }

    public async Task<MicrosoftDeviceToken> RefreshDeviceToken(string refreshToken) {
        if (_applicationStateService.AuthType == null) {
            throw new InvalidOperationException("AuthType is not set in ApplicationStateService");
        }

        var requestData = new List<KeyValuePair<string, string>> {
            new("grant_type", "refresh_token"),
            new("client_id", _applicationStateService.AuthType.Data),
            new("refresh_token", refreshToken)
        };

        var content = new FormUrlEncodedContent(requestData);

        var response = await _httpClient.PostAsync(TokenUrl, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) {
            throw new HttpRequestException($"Failed to get device token: {response.StatusCode} - {responseContent}");
        }

        var deviceToken = JsonConvert.DeserializeObject<MicrosoftDeviceToken>(responseContent);
        return deviceToken ?? throw new InvalidOperationException("Failed to deserialize device token response");
    }

    public async Task<MicrosoftAuthResult> AuthenticateXboxLive(string accessToken) {
        if (_applicationStateService.AuthType == null) {
            throw new InvalidOperationException("AuthType is not set in ApplicationStateService");
        }

        var requestBody = new {
            Properties = new {
                AuthMethod = "RPS",
                SiteName = "user.auth.xboxlive.com",
                RpsTicket = $"d={accessToken}"
            },
            RelyingParty = "http://auth.xboxlive.com",
            TokenType = "JWT"
        };
        var requestContent = new StringContent(JsonConvert.SerializeObject(requestBody), System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://user.auth.xboxlive.com/user/authenticate", requestContent);
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) {
            throw new HttpRequestException($"Failed to authenticate Xbox Live: {response.StatusCode} - {responseContent}");
        }

        var authResult = JsonConvert.DeserializeObject<MicrosoftAuthResult>(responseContent);
        return authResult ?? throw new InvalidOperationException("Failed to deserialize Xbox Live authentication response");
    }

    public async Task<Object> ObtainXTSTMinecraftToken(string accessToken) {
        if (_applicationStateService.AuthType == null) {
            throw new InvalidOperationException("AuthType is not set in ApplicationStateService");
        }

        var requestBody = new {
            Properties = new {
                SandboxId = "RETAIL",
                UserTokens = new List<string> { accessToken }
            },
            RelyingParty = "rp://api.minecraftservices.com/",
            TokenType = "JWT"
        };
        var requestContent = new StringContent(JsonConvert.SerializeObject(requestBody), System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://xsts.auth.xboxlive.com/xsts/authorize", requestContent);
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) {
            if (response.StatusCode == HttpStatusCode.Unauthorized) {
                var microsoftAuthError = JsonConvert.DeserializeObject<MicrosoftAuthError>(responseContent);
                return microsoftAuthError ?? throw new InvalidOperationException("Failed to deserialize Xbox Live authentication response");
            }

            throw new HttpRequestException($"Failed to obtain XSTS Minecraft token: {response.StatusCode} - {responseContent}");
        }

        var authResult = JsonConvert.DeserializeObject<MicrosoftAuthResult>(responseContent);
        return authResult ?? throw new InvalidOperationException("Failed to deserialize XSTS Minecraft token response");
    }

    public async Task<MinecraftAuthResult> AuthenticateMinecraft(string userHash, string accessToken) {
        if (_applicationStateService.AuthType == null) {
            throw new InvalidOperationException("AuthType is not set in ApplicationStateService");
        }

        var requestBody = new {
            identityToken = $"XBL3.0 x={userHash};{accessToken}",
        };
        var requestContent = new StringContent(JsonConvert.SerializeObject(requestBody), System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://api.minecraftservices.com/authentication/login_with_xbox", requestContent);
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) {
            throw new HttpRequestException($"Failed to authenticate Minecraft: {response.StatusCode} - {responseContent}");
        }

        var authResult = JsonConvert.DeserializeObject<MinecraftAuthResult>(responseContent);
        return authResult ?? throw new InvalidOperationException("Failed to deserialize Minecraft authentication response");
    }

    public async Task<Object> GetMinecraftProfile(string accessToken) {
        if (_applicationStateService.AuthType == null) {
            throw new InvalidOperationException("AuthType is not set in ApplicationStateService");
        }

        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.minecraftservices.com/minecraft/profile");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) {
            var minecraftAuthError = JsonConvert.DeserializeObject<MinecraftProfileError>(responseContent);
            return minecraftAuthError ?? throw new InvalidOperationException("Failed to deserialize Minecraft profile error response");
        }

        var authResult = JsonConvert.DeserializeObject<MinecraftProfile>(responseContent);
        return authResult ?? throw new InvalidOperationException("Failed to deserialize Minecraft profile response");
    }
}
