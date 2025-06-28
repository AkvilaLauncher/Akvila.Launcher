using Akvila.Launcher.ViewModels.Pages;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace Akvila.Launcher.Views.Pages;

public partial class ProfilePageView : ReactiveUserControl<ProfilePageViewModel> {
    public ProfilePageView() {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
}
