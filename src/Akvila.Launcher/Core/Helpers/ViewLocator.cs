using System;
using Akvila.Launcher.ViewModels.Base;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Akvila.Launcher.Core.Helpers;

public class ViewLocator : IDataTemplate {
    public Control Build(object? data) {
        if (data == null)
            return new TextBlock { Text = "Data is null" };

        var name = data.GetType().FullName!.Replace("ViewModel", "View");

        var type = Type.GetType(name);

        if (type != null)
            return (Control)Activator.CreateInstance(type)!;

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data) {
        return data is ViewModelBase;
    }
}
