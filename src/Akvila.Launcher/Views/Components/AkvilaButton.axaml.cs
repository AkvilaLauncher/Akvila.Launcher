using System;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;

namespace Akvila.Launcher.Views.Components;

public class AkvilaButton : TemplatedControl {
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<AkvilaButton, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    public static readonly StyledProperty<Uri> IconPathProperty = AvaloniaProperty.Register<AkvilaButton, Uri>(
        nameof(IconPath));

    public static readonly StyledProperty<double> IconSizeProperty = AvaloniaProperty.Register<AkvilaButton, double>(
        nameof(IconSize), 16);

    public static readonly StyledProperty<ICommand> CommandProperty = AvaloniaProperty.Register<AkvilaButton, ICommand>(
        nameof(Command));

    public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<AkvilaButton, string?>(
        nameof(Text), "DefaultButton style");

    public static readonly StyledProperty<int> SpacingProperty = AvaloniaProperty.Register<AkvilaButton, int>(
        nameof(Spacing), 10);

    public static readonly StyledProperty<bool> IsDefaultProperty = AvaloniaProperty.Register<AkvilaButton, bool>(
        nameof(IsDefault));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<AkvilaButton, object?>(
            nameof(CommandParameter));

    public object? CommandParameter {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public bool IsDefault {
        get => GetValue(IsDefaultProperty);
        set => SetValue(IsDefaultProperty, value);
    }

    public int Spacing {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public string? Text {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public ICommand Command {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public double IconSize {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public Uri IconPath {
        get => GetValue(IconPathProperty);
        set => SetValue(IconPathProperty, value);
    }

    public event EventHandler<RoutedEventArgs>? Click {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e) {
        base.OnApplyTemplate(e);

        if (this.GetTemplateChildren().First() is Button button)
            button.Click += (_, _) => RaiseEvent(new RoutedEventArgs(ClickEvent));
        ;
    }
}
