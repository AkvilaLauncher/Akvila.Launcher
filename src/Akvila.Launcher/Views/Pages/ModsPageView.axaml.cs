using Akvila.Launcher.ViewModels.Pages;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace Akvila.Launcher.Views.Pages;

public partial class ModsPageView : ReactiveUserControl<ModsPageViewModel> {
    public ModsPageView() {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
}
