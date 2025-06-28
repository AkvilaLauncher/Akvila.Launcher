using System.Collections.Generic;
using Akvila.Launcher.Models;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace Akvila.Launcher.Views.Components;

public class ProfileInfoComponent : TemplatedControl {
    public static readonly StyledProperty<IEnumerable<ProfileInfoItem>> ProfileInfoItemsProperty =
        AvaloniaProperty.Register<ProfileUserControl, IEnumerable<ProfileInfoItem>>(
            nameof(ProfileInfoItems), new List<ProfileInfoItem> {
                // new("Registered", "150 days ago"),
                // new("Playtime", " 551 hours 45 minutes"),
                // new("Balance", "150 coins."),
                // new("Group", "Premium"),
            });


    public IEnumerable<ProfileInfoItem> ProfileInfoItems {
        get => GetValue(ProfileInfoItemsProperty);
        set => SetValue(ProfileInfoItemsProperty, value);
    }
}
