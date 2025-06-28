using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using Akvila.Launcher.Assets;
using Akvila.Launcher.Core.Services;
using Akvila.Launcher.ViewModels.Base;
using Akvila.Client;
using Akvila.Client.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Sentry;

namespace Akvila.Launcher.ViewModels.Pages;

public class ProfilePageViewModel : PageViewModelBase {
    private readonly IAkvilaClientManager _manager;

    [Reactive] public string TextureUrl { get; set; }
    [Reactive] public IUser User { get; set; }
    internal ProfilePageViewModel(
        IScreen screen,
        IUser user,
        IAkvilaClientManager manager,
        ILocalizationService? localizationService = null) : base(screen,
        localizationService) {
        User = user ?? throw new ArgumentNullException(nameof(user));
        _manager = manager;

        RxApp.MainThreadScheduler.Schedule(LoadData);
    }

    public new string Title => LocalizationService.GetString(ResourceKeysDictionary.MainPageTitle);

    private async void LoadData() {
        try {
            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss:fff}] Loading texture data...]");
            var userTextureInfo = await _manager.GetTexturesByName(User.Name);

            if (userTextureInfo is null)
                return;

            TextureUrl = userTextureInfo.FullSkinUrl ?? string.Empty;
            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss:fff}] Textures updated: {TextureUrl}");
        }
        catch (Exception exception) {
            SentrySdk.CaptureException(exception);
        }
    }

}
