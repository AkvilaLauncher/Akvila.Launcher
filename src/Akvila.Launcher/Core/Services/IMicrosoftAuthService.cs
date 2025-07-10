using System;
using System.Threading.Tasks;
using Akvila.Web.Api.Domains.Integrations;

namespace Akvila.Launcher.Core.Services;

public interface IMicrosoftAuthService {
    Task<MicrosoftDeviceCode> GetDeviceCodeAsync();
    Task<MicrosoftDeviceToken> GetDeviceToken(string deviceCode);
    Task<MicrosoftDeviceToken> RefreshDeviceToken(string refreshToken);
    Task<MicrosoftAuthResult> AuthenticateXboxLive(string accessToken);
    Task<Object> ObtainXTSTMinecraftToken(string accessToken);
    Task<MinecraftAuthResult> AuthenticateMinecraft(string userHash, string accessToken);
    Task<Object> GetMinecraftProfile(string accessToken);
}
