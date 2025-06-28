using Akvila.Launcher.ViewModels.Pages;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace Akvila.Launcher.Views.Pages;

public partial class OverviewPageView : ReactiveUserControl<OverviewPageViewModel> {
    public OverviewPageView() {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
}
