using System;
using System.Reactive;
using Akvila.Launcher.ViewModels.Base;
using ReactiveUI;

namespace Akvila.Launcher.ViewModels.Components;

public enum MicrosoftAuthStage {
    AuthXboxLive = 1,
    GetXstsMinecraftToken = 2,
    AuthMinecraft = 3,
    GetGameProfile = 4
}

public class MicrosoftAuthProgressModalModel : ViewModelBase, IDisposable {
    private MicrosoftAuthStage _currentStage = MicrosoftAuthStage.AuthXboxLive;
    private string _currentStageName = "Authenticating Xbox Live...";
    private string _statusMessage = "Authenticating...";
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _disposed;

    public MicrosoftAuthProgressModalModel() {
        CancelCommand = ReactiveCommand.Create(Cancel);
        RetryCommand = ReactiveCommand.Create(Retry);

        UpdateStageInfo();
    }

    public MicrosoftAuthStage CurrentStage {
        get => _currentStage;
        set {
            this.RaiseAndSetIfChanged(ref _currentStage, value);
            UpdateStageInfo();
        }
    }

    public string CurrentStageName {
        get => _currentStageName;
        private set => this.RaiseAndSetIfChanged(ref _currentStageName, value);
    }

    public string StatusMessage {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public string ErrorMessage {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    public bool HasError {
        get => _hasError;
        set => this.RaiseAndSetIfChanged(ref _hasError, value);
    }

    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> RetryCommand { get; }

    public event EventHandler? OnAuthCancelled;
    public event EventHandler? OnAuthRetry;

    private void UpdateStageInfo() {
        CurrentStageName = CurrentStage switch {
            MicrosoftAuthStage.AuthXboxLive => "Authenticating Xbox Live...",
            MicrosoftAuthStage.GetXstsMinecraftToken => "Getting XSTS token...",
            MicrosoftAuthStage.AuthMinecraft => "Authenticating Minecraft...",
            MicrosoftAuthStage.GetGameProfile => "Getting game profile...",
            _ => "Processing..."
        };
        this.RaisePropertyChanged(nameof(CurrentStage));
    }

    public void SetError(string errorMessage, string? userFriendlyMessage = null) {
        ErrorMessage = userFriendlyMessage ?? errorMessage;
        HasError = true;
        StatusMessage = "Authentication failed";
    }

    public void ClearError() {
        HasError = false;
        ErrorMessage = string.Empty;
        StatusMessage = "Authenticating...";
    }

    public void AdvanceToNextStage() {
        if (CurrentStage < MicrosoftAuthStage.GetGameProfile) {
            CurrentStage = (MicrosoftAuthStage)((int)CurrentStage + 1);
            ClearError();
        }
    }

    public void SetStage(MicrosoftAuthStage stage) {
        CurrentStage = stage;
        ClearError();
    }

    private void Cancel() {
        OnAuthCancelled?.Invoke(this, EventArgs.Empty);
    }

    private void Retry() {
        ClearError();
        CurrentStage = MicrosoftAuthStage.AuthXboxLive;
        OnAuthRetry?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (!_disposed && disposing) {
            _disposed = true;
        }
    }
}
