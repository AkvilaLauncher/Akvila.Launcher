using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Akvila.Launcher.Assets;
using Akvila.Launcher.Core.Exceptions;
using Akvila.Launcher.Core.Services;
using Akvila.Launcher.ViewModels.Base;
using GamerVII.Notification.Avalonia;
using Akvila.Client;
using Akvila.Client.Models;
using Akvila.Launcher.ViewModels.Components;
using Akvila.Web.Api.Domains.Integrations;
using Akvila.Web.Api.Dto.Integration;
using AkvilaCore.Interfaces.Enums;
using ReactiveUI;
using Sentry;
using Splat;

namespace Akvila.Launcher.ViewModels.Pages;

public class LoginPageViewModel : PageViewModelBase {
    private readonly IAkvilaClientManager _akvilaClientManager;
    private readonly IMicrosoftAuthService _microsoftAuthService;
    private readonly IObservable<bool> _onClosed;
    private readonly MainWindowViewModel _screen;
    private readonly IStorageService _storageService;
    private readonly ISystemService _systemService;
    private readonly IApplicationStateService _applicationStateService;
    private ObservableCollection<string> _errorList = new();
    private bool _isProcessing;
    private string _login = string.Empty;
    private string _password = string.Empty;
    private MicrosoftAuthModalModel? _microsoftAuthModal;
    private MicrosoftAuthProgressModalModel? _microsoftAuthProgressModal;

    internal LoginPageViewModel(IScreen screen,
        IObservable<bool> onClosed,
        IAkvilaClientManager? akvilaClientManager = null,
        IStorageService? storageService = null,
        ISystemService? systemService = null,
        ILocalizationService? localizationService = null,
        IMicrosoftAuthService? microsoftAuthService = null,
        IApplicationStateService? applicationStateService = null) : base(screen, localizationService) {
        _screen = (MainWindowViewModel)screen;
        _onClosed = onClosed;

        _storageService = storageService
                          ?? Locator.Current.GetService<IStorageService>()
                          ?? throw new ServiceNotFoundException(typeof(IStorageService));

        _systemService = systemService
                         ?? Locator.Current.GetService<ISystemService>()
                         ?? throw new ServiceNotFoundException(typeof(IStorageService));

        _applicationStateService = applicationStateService
                                   ?? Locator.Current.GetService<IApplicationStateService>()
                                   ?? throw new ServiceNotFoundException(typeof(IApplicationStateService));

        _akvilaClientManager = akvilaClientManager
                               ?? Locator.Current.GetService<IAkvilaClientManager>()
                               ?? throw new ServiceNotFoundException(typeof(IAkvilaClientManager));

        _microsoftAuthService = microsoftAuthService ?? Locator.Current.GetService<IMicrosoftAuthService>() ?? new MicrosoftAuthService();

        _screen.OnClosed.Subscribe(DisposeConnections);

        LoginCommand = ReactiveCommand.CreateFromTask(OnAuth);
        MicrosoftLoginCommand = ReactiveCommand.CreateFromTask(OnMicrosoftAuth);

        RxApp.MainThreadScheduler.Schedule(CheckAuth);
    }

    public string Login {
        get => _login;
        set => this.RaiseAndSetIfChanged(ref _login, value);
    }

    public string Password {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    public MicrosoftAuthModalModel? MicrosoftAuthModal {
        get => _microsoftAuthModal;
        private set => this.RaiseAndSetIfChanged(ref _microsoftAuthModal, value);
    }

    public MicrosoftAuthProgressModalModel? MicrosoftAuthProgressModal {
        get => _microsoftAuthProgressModal;
        private set => this.RaiseAndSetIfChanged(ref _microsoftAuthProgressModal, value);
    }

    public bool IsShowingMicrosoftModal => MicrosoftAuthModal is not null;
    public bool IsShowingMicrosoftProgressModal => MicrosoftAuthProgressModal is not null;

    public bool IsProcessing {
        get => _isProcessing;
        set {
            this.RaiseAndSetIfChanged(ref _isProcessing, value);

            this.RaisePropertyChanged(nameof(IsNotProcessing));
        }
    }

    public ObservableCollection<string> Errors {
        get => _errorList;
        set => this.RaiseAndSetIfChanged(ref _errorList, value);
    }

    public bool IsNotProcessing => !_isProcessing;
    public ICommand LoginCommand { get; set; }
    public ICommand MicrosoftLoginCommand { get; set; }
    public AuthTypeReadDto? CurrentAuthType => _applicationStateService.AuthType;
    public bool HasAuthTypeData => _applicationStateService.HasAuthType;

    public bool ShowInfoMessage => !HasAuthTypeData || CurrentAuthType?.AuthType == AuthGeneralType.Undefined;
    public bool ShowLoginFields => HasAuthTypeData && CurrentAuthType?.AuthType != AuthGeneralType.Undefined && CurrentAuthType?.AuthType != AuthGeneralType.Microsoft;

    public bool ShowPasswordField => HasAuthTypeData && CurrentAuthType?.AuthType != AuthGeneralType.Undefined && CurrentAuthType?.AuthType != AuthGeneralType.Any
                                     && CurrentAuthType?.AuthType != AuthGeneralType.Microsoft;

    public bool ShowStandardLoginButton => HasAuthTypeData && CurrentAuthType?.AuthType != AuthGeneralType.Undefined && CurrentAuthType?.AuthType != AuthGeneralType.Microsoft;
    public bool ShowMicrosoftLoginButton => HasAuthTypeData && CurrentAuthType?.AuthType == AuthGeneralType.Microsoft;
    public string InfoMessage => "Authorization service is not configured or is configured incorrectly";

    private void DisposeConnections(bool isClosed) {
        _akvilaClientManager.Dispose();
    }

    private async void CheckAuth() {
        var authUser = await _storageService.GetAsync<AuthUser>(StorageConstants.User);

        if (authUser is { IsAuth: true }) {
            if (authUser.ExpiredDate < DateTime.Now) {
                if (_applicationStateService is { HasAuthType: true, AuthType.AuthType: AuthGeneralType.Microsoft }) {
                    if (authUser.RefreshExpiredDate < DateTime.Now) return;

                    var microsoftDeviceToken = await _microsoftAuthService.RefreshDeviceToken(authUser.RefreshToken);
                    StartMicrosoftAuthProgress(microsoftDeviceToken);
                }
                return;
            }

            _screen.Router.Navigate.Execute(new OverviewPageViewModel(_screen, authUser, _onClosed));
            await _akvilaClientManager.OpenServerConnection(authUser);
        }
    }

    private async Task OnAuth(CancellationToken arg) {
        try {
            IsProcessing = true;

            var authInfo = await _akvilaClientManager.Auth(Login, Password, _systemService.GetHwid());

            if (authInfo.User.IsAuth) {
                await _storageService.SetAsync(StorageConstants.User, authInfo.User);
                _screen.Router.Navigate.Execute(new OverviewPageViewModel(_screen, authInfo.User, _onClosed));
                return;
            }

            if (authInfo.Item1.Has2Fa)
                //ToDo: Next versions
                return;

            if (_screen is { } mainView) {
                if (!authInfo.Details.Any())
                    mainView.Manager
                        .CreateMessage(true, "#D03E3E",
                            LocalizationService.GetString(ResourceKeysDictionary.InvalidAuthData),
                            authInfo.Message)
                        .Dismiss()
                        .WithDelay(TimeSpan.FromSeconds(3))
                        .Queue();

                Errors = new ObservableCollection<string>(authInfo.Details);
            }
        } catch (Exception exception) {
            if (_screen is { } mainView) {
                mainView.Manager
                    .CreateMessage(true, "#D03E3E",
                        LocalizationService.GetString(ResourceKeysDictionary.InvalidAuthData),
                        exception.Message)
                    .Dismiss()
                    .WithDelay(TimeSpan.FromSeconds(3))
                    .Queue();
            }

            Debug.WriteLine(exception);
            SentrySdk.CaptureException(exception);
        } finally {
            IsProcessing = false;
        }
    }

    private async Task OnMicrosoftAuth(CancellationToken arg) {
        try {
            IsProcessing = true;
            var authModal = new MicrosoftAuthModalModel();

            authModal.OnAuthCancelled += (sender, args) => {
                IsProcessing = false;
                MicrosoftAuthModal?.Dispose();
                MicrosoftAuthModal = null;
                this.RaisePropertyChanged(nameof(IsShowingMicrosoftModal));
            };

            authModal.OnAuthCompleted += (sender, microsoftDeviceToken) => {
                try {
                    MicrosoftAuthModal?.Dispose();
                    MicrosoftAuthModal = null;
                    this.RaisePropertyChanged(nameof(IsShowingMicrosoftModal));

                    StartMicrosoftAuthProgress(microsoftDeviceToken);
                } catch (Exception ex) {
                    Debug.WriteLine(ex);
                    SentrySdk.CaptureException(ex);
                    IsProcessing = false;
                    MicrosoftAuthModal?.Dispose();
                    MicrosoftAuthModal = null;
                    this.RaisePropertyChanged(nameof(IsShowingMicrosoftModal));
                }
            };

            MicrosoftAuthModal = authModal;
            this.RaisePropertyChanged(nameof(IsShowingMicrosoftModal));

            try {
                await authModal.StartAuthenticationAsync();
            } catch (Exception ex) {
                Debug.WriteLine($"Failed to start authentication: {ex.Message}");
                IsProcessing = false;
                MicrosoftAuthModal?.Dispose();
                MicrosoftAuthModal = null;
                this.RaisePropertyChanged(nameof(IsShowingMicrosoftModal));
            }
        } catch (Exception exception) {
            Debug.WriteLine(exception);
            SentrySdk.CaptureException(exception);
            IsProcessing = false;
            MicrosoftAuthModal?.Dispose();
            MicrosoftAuthModal = null;
            this.RaisePropertyChanged(nameof(IsShowingMicrosoftModal));
        }
    }

    private async void StartMicrosoftAuthProgress(MicrosoftDeviceToken deviceToken) {
        var progressModal = new MicrosoftAuthProgressModalModel();
        try {
            progressModal.OnAuthCancelled += (sender, args) => {
                IsProcessing = false;
                MicrosoftAuthProgressModal?.Dispose();
                MicrosoftAuthProgressModal = null;
                this.RaisePropertyChanged(nameof(IsShowingMicrosoftProgressModal));
            };

            progressModal.OnAuthRetry += (sender, args) => {
                MicrosoftAuthProgressModal?.Dispose();
                MicrosoftAuthProgressModal = null;
                this.RaisePropertyChanged(nameof(IsShowingMicrosoftProgressModal));

                _ = Task.Run(async () => await OnMicrosoftAuth(CancellationToken.None));
            };

            MicrosoftAuthProgressModal = progressModal;
            this.RaisePropertyChanged(nameof(IsShowingMicrosoftProgressModal));

            var xboxLiveAuth = await _microsoftAuthService.AuthenticateXboxLive(deviceToken.AccessToken);
            progressModal.AdvanceToNextStage();

            var minecraftXTST = await _microsoftAuthService.ObtainXTSTMinecraftToken(xboxLiveAuth.Token);
            if (minecraftXTST is MicrosoftAuthError error) {
                string message = null;
                switch (error.XErr) {
                    case 2148916227:
                        message = "Account is banned from Xbox.";
                        break;
                    case 2148916233:
                        message = "Account doesn't have an Xbox account.";
                        break;
                    case 2148916235:
                        message = "Account is from a country where Xbox Live is not available/banned.";
                        break;
                    case 2148916236:
                    case 2148916237:
                        message = "Account needs adult verification on Xbox page.";
                        break;
                    case 2148916238:
                        message = "Account is a child (under 18) and cannot proceed unless the account is added to a Family by an adult.";
                        break;
                }

                message ??= error.Message;

                Debug.Write($"Microsoft authentication failed: {message}");
                progressModal.SetError(message, "Microsoft Authentication Error");
                return;
            }

            if (minecraftXTST is not MicrosoftAuthResult minecraftAuthResult) {
                throw new InvalidOperationException("Failed to obtain Minecraft authentication result.");
            }

            progressModal.AdvanceToNextStage();

            var minecraftAuth = await _microsoftAuthService.AuthenticateMinecraft(minecraftAuthResult.DisplayClaims.Xui.First().Uhs, minecraftAuthResult.Token);
            progressModal.AdvanceToNextStage();

            var minecraftResponse = await _microsoftAuthService.GetMinecraftProfile(minecraftAuth.AccessToken);
            if (minecraftResponse is MinecraftProfileError minecraftProfileError) {
                if (minecraftProfileError.Error == "NOT_FOUND") {
                    Debug.WriteLine("Minecraft profile not found.");
                    progressModal.SetError(minecraftProfileError.ErrorMessage, "Minecraft profile not found. Please ensure your account has a valid Minecraft license.");
                    return;
                }

                Debug.WriteLine($"Failed to get Minecraft profile: {minecraftProfileError.ErrorMessage}");
                progressModal.SetError(minecraftProfileError.ErrorMessage);
                return;
            }

            if (minecraftResponse is not MinecraftProfile minecraftProfile) {
                throw new InvalidOperationException("Failed to obtain Minecraft profile.");
            }

            IUser user = new AuthUser {
                Name = minecraftProfile.Name,
                AccessToken = minecraftAuth.AccessToken,
                RefreshToken = deviceToken.RefreshToken,
                Uuid = minecraftProfile.Id,
                ExpiredDate = DateTime.Now + TimeSpan.FromSeconds(minecraftAuth.ExpiresIn),
                RefreshExpiredDate = DateTime.Now + TimeSpan.FromDays(90),
                TextureUrl = _akvilaClientManager.GetTextureUrl(),
                IsAuth = true,
                Has2Fa = false
            };

            await _storageService.SetAsync(StorageConstants.User, user);
            await _akvilaClientManager.OpenServerConnection(user);

            RxApp.MainThreadScheduler.Schedule(() => {
                _screen.Router.Navigate.Execute(new OverviewPageViewModel(_screen, user, _onClosed));
            });

            IsProcessing = false;
            MicrosoftAuthProgressModal = null;
            this.RaisePropertyChanged(nameof(IsShowingMicrosoftProgressModal));
            MicrosoftAuthProgressModal?.Dispose();
        } catch (Exception e) {
            Debug.WriteLine($"Authentication failed: {e.Message}");
            progressModal.SetError(e.Message);
        }
    }
}
