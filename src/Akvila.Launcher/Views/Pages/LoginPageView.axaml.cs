using Akvila.Launcher.ViewModels.Pages;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace Akvila.Launcher.Views.Pages;

public partial class LoginPageView : ReactiveUserControl<LoginPageViewModel> {
    public LoginPageView() {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
}
