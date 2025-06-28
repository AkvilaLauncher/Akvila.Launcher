using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Akvila.Launcher.ViewModels.Pages;
using Akvila.Web.Api.Dto.Mods;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Akvila.Launcher.Core.Converters;

public class ModsNameConverter : MarkupExtension, IMultiValueConverter {
    public override object ProvideValue(IServiceProvider serviceProvider) => this;

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) {
        var modDto = values.FirstOrDefault() as ModReadDto;

        if (modDto is not null &&
            values.LastOrDefault() is ModsPageViewModel modsPageViewModel &&
            modsPageViewModel.OptionalModsDetails.TryGetValue($"{modDto.Name}.jar", out var modInfo)) {
            return modInfo.Title;
        }

        return modDto?.Name ?? "Не указано";
    }
}
