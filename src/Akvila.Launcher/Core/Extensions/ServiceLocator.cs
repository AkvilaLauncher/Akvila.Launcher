using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Akvila.Launcher.Assets;
using Akvila.Launcher.Core.Services;
using Akvila.Launcher.Models;
using Avalonia;
using Akvila.Client;
using Sentry;
using Splat;

namespace Akvila.Launcher.Core.Extensions;

public static class ServiceLocator {
    public static AppBuilder RegisterServices(this AppBuilder builder, string[] arguments) {
        var systemService = new SystemService();

        var installationDirectory =
            Path.Combine(systemService.GetApplicationFolder(), ResourceKeysDictionary.FolderName);

        RegisterLocalizationService();
        RegisterSystemService(systemService);
        RegisterLogHelper(systemService);
        var manager = RegisterAkvilaManager(systemService, installationDirectory, arguments);
        var storageService = RegisterStorage();

        CheckAndChangeInstallationFolder(storageService, manager);
        CheckAndChangeLanguage(storageService, systemService);
        Locator.CurrentMutable.RegisterConstant(new VpnChecker(), typeof(IVpnChecker));

        AppDomain.CurrentDomain.UnhandledException += (_, args) => {
            SentrySdk.CaptureException((Exception)args.ExceptionObject);
        };

        Debug.WriteLine($"[Akvila][{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Configuring ended");

        return builder;
    }

    private static void RegisterLogHelper(SystemService systemService) {
        Locator.CurrentMutable.RegisterConstant(new LogHandler());
    }

    private static void CheckAndChangeLanguage(LocalStorageService storageService, SystemService systemService) {
        var data = storageService.GetAsync<SettingsInfo>(StorageConstants.Settings).Result;

        if (data != null && !string.IsNullOrEmpty(data.LanguageCode))
            Assets.Resources.Resources.Culture = systemService
                .GetAvailableLanguages()
                .FirstOrDefault(c => c.Culture.Name == data.LanguageCode)?
                .Culture ?? new CultureInfo("en-US");
    }

    private static LocalStorageService RegisterStorage() {
        var storageService = new LocalStorageService();
        Locator.CurrentMutable.RegisterConstant(storageService, typeof(IStorageService));

        return storageService;
    }

    private static void CheckAndChangeInstallationFolder(LocalStorageService storageService, AkvilaClientManager manager) {
        var installationDirectory = storageService.GetAsync<string>(StorageConstants.InstallationDirectory).Result;

        if (!string.IsNullOrEmpty(installationDirectory)) manager.ChangeInstallationFolder(installationDirectory);
    }

    private static AkvilaClientManager RegisterAkvilaManager(SystemService systemService, string installationDirectory,
        string[] arguments) {
        var manager = new AkvilaClientManager(installationDirectory, ResourceKeysDictionary.Host,
            ResourceKeysDictionary.FolderName,
            systemService.GetOsType());
#if DEBUG
        manager.SkipUpdate = true;
#else
        manager.SkipUpdate = arguments.Contains("-skip-update");
#endif

        Locator.CurrentMutable.RegisterConstant(manager, typeof(IAkvilaClientManager));

        return manager;
    }

    private static void RegisterSystemService(SystemService systemService) {
        Locator.CurrentMutable.RegisterConstant(systemService, typeof(ISystemService));
    }

    private static void RegisterLocalizationService() {
        var service = new ResourceLocalizationService();
        Locator.CurrentMutable.RegisterConstant(service, typeof(ILocalizationService));
    }
}
