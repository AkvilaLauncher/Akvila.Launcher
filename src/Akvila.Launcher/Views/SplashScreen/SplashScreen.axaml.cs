using System;
using Akvila.Launcher.ViewModels;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using GamerVII.Notification.Avalonia;
using Akvila.Launcher.Assets;
using Akvila.Launcher.ViewModels.Pages;

namespace Akvila.Launcher.Views.SplashScreen;

public partial class SplashScreen : ReactiveWindow<SplashScreenViewModel> {
    public SplashScreen() {
        InitializeComponent();
    }

    public MainWindow GetMainWindow() {
        var mainWindow = new MainWindow {
            DataContext = new MainWindowViewModel()
        };

        return mainWindow;
    }
}
