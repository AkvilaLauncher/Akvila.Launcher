using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Akvila.Launcher.ViewModels.Base;
using ReactiveUI;
using Akvila.Launcher.Core.Services;
using Akvila.Web.Api.Domains.Integrations;
using Splat;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Akvila.Launcher.ViewModels.Components;

public class MicrosoftAuthModalModel : ViewModelBase, IDisposable {
    private readonly IMicrosoftAuthService _microsoftAuthService;
    private string _deviceCode = string.Empty;
    private string _userCode = string.Empty;
    private string _verificationUrl = string.Empty;
    private double _progress;
    private bool _isPolling;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;

    public MicrosoftAuthModalModel(IMicrosoftAuthService? microsoftAuthService = null) {
        _microsoftAuthService = microsoftAuthService ?? Locator.Current.GetService<IMicrosoftAuthService>() ?? new MicrosoftAuthService();

        CancelCommand = ReactiveCommand.Create(Cancel);
        OpenBrowserCommand = ReactiveCommand.Create(OpenBrowser);

        _cancellationTokenSource = new CancellationTokenSource();
    }

    public string DeviceCode {
        get => _deviceCode;
        set => this.RaiseAndSetIfChanged(ref _deviceCode, value);
    }

    public string UserCode {
        get => _userCode;
        set => this.RaiseAndSetIfChanged(ref _userCode, value);
    }

    public string VerificationUrl {
        get => _verificationUrl == "" ? "https://www.microsoft.com/link" : _verificationUrl;
        set => this.RaiseAndSetIfChanged(ref _verificationUrl, value);
    }

    public double Progress {
        get => _progress;
        set => this.RaiseAndSetIfChanged(ref _progress, value);
    }

    public bool IsPolling {
        get => _isPolling;
        set => this.RaiseAndSetIfChanged(ref _isPolling, value);
    }

    public ReactiveCommand<Unit, Unit> OpenBrowserCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public event EventHandler? OnAuthCancelled;
    public event EventHandler<MicrosoftDeviceToken>? OnAuthCompleted;

    public async Task StartAuthenticationAsync() {
        try {
            IsPolling = true;

            var deviceCodeResponse = await _microsoftAuthService.GetDeviceCodeAsync();

            UserCode = deviceCodeResponse.UserCode;
            DeviceCode = deviceCodeResponse.DeviceCode;
            VerificationUrl = deviceCodeResponse.VerificationUri;

            if (_cancellationTokenSource != null) {
                _ = Task.Run(async () => await PollForAuthenticationAsync(deviceCodeResponse), _cancellationTokenSource.Token);
            }
        } catch (Exception ex) {
            Debug.WriteLine($"Failed to start Microsoft authentication: {ex.Message}");
            IsPolling = false;
            OnAuthCancelled?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task PollForAuthenticationAsync(MicrosoftDeviceCode deviceCodeResponse) {
        var interval = TimeSpan.FromSeconds(deviceCodeResponse.Interval);
        var expirationTime = DateTime.Now.AddSeconds(deviceCodeResponse.ExpiresIn);

        while (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested && DateTime.Now < expirationTime) {
            try {
                await Task.Delay(interval, _cancellationTokenSource.Token);

                if (_cancellationTokenSource.Token.IsCancellationRequested) {
                    break;
                }

                var tokenResponse = await _microsoftAuthService.GetDeviceToken(DeviceCode);

                IsPolling = false;
                OnAuthCompleted?.Invoke(this, tokenResponse);
                return;
            } catch (HttpRequestException ex) when (ex.Message.Contains("authorization_pending")) {
                // User hasn't completed authentication yet, continue polling
            } catch (HttpRequestException ex) when (ex.Message.Contains("authorization_declined")) {
                // User declined authentication
                IsPolling = false;
                OnAuthCancelled?.Invoke(this, EventArgs.Empty);
                return;
            } catch (HttpRequestException ex) when (ex.Message.Contains("expired_token")) {
                // Token expired
                IsPolling = false;
                OnAuthCancelled?.Invoke(this, EventArgs.Empty);
                return;
            } catch (Exception ex) {
                Debug.WriteLine($"Error during authentication polling: {ex.Message}");
            }
        }

        IsPolling = false;
        OnAuthCancelled?.Invoke(this, EventArgs.Empty);
    }

    private void Cancel() {
        IsPolling = false;
        if (!_disposed && _cancellationTokenSource != null) {
            try {
                _cancellationTokenSource.Cancel();
            } catch (ObjectDisposedException) {
                // CancellationTokenSource has already been disposed, ignore
            }
        }

        OnAuthCancelled?.Invoke(this, EventArgs.Empty);
    }

    private async void OpenBrowser() {
        try {
            ProcessStartInfo psi = new ProcessStartInfo {
                FileName = VerificationUrl,
                UseShellExecute = true
            };
            Process.Start(psi);

            if (!string.IsNullOrEmpty(UserCode)) {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                    var mainWindow = desktop.MainWindow;
                    if (mainWindow?.Clipboard != null) {
                        await mainWindow.Clipboard.SetTextAsync(UserCode);
                        Debug.WriteLine($"User code copied to clipboard: {UserCode}");
                    }
                }
            }
        } catch (Exception ex) {
            Debug.WriteLine($"Failed to open browser: {ex.Message}");
        }
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (!_disposed && disposing) {
            _disposed = true;
            if (_cancellationTokenSource != null) {
                try {
                    _cancellationTokenSource.Cancel();
                } catch (ObjectDisposedException) {
                    // Already disposed, ignore
                }

                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }
    }
}
