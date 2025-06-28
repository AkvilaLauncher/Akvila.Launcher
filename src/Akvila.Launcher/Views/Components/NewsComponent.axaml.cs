using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using TheArtOfDev.HtmlRenderer.Avalonia;

namespace Akvila.Launcher.Views.Components;

public class NewsComponent : TemplatedControl {
    public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<NewsComponent, string>(
        nameof(Title), "Headline");

    public static readonly StyledProperty<string> DescriptionProperty = AvaloniaProperty.Register<NewsComponent, string>(
        nameof(Description), "<i>Description</i>");

    public static readonly StyledProperty<DateTimeOffset> DateProperty = AvaloniaProperty.Register<NewsComponent, DateTimeOffset>(
        nameof(Date), DateTimeOffset.Now);

    public DateTimeOffset Date {
        get => GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }

    public string Description {
        get => $"<span style='color: gray;'>{GetValue(DescriptionProperty)}</span>";
        set => SetValue(DescriptionProperty, value);
    }

    public string Title {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e) {
        if (e.NameScope.Find<HtmlPanel>("HtmlPanel") is { } htmlPanel) {
            htmlPanel.Text = $"<span style='color: gray;'>{Description}</span>";
        }
    }
}
