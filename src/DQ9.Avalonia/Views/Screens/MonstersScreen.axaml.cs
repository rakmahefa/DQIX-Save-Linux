using Avalonia.Controls;
using Avalonia.Interactivity;
using DQ9.Avalonia.ViewModels.Screens;
using DQ9.Avalonia.Views;

namespace DQ9.Avalonia.Views.Screens;

public partial class MonstersScreen : UserControl
{
    public MonstersScreen() => InitializeComponent();

    // UX-16: the bulk overwrite asks for confirmation ("Apply to all" with a
    // silent 0 default zeroed 300+ counters in one click before).
    private async void OnLump(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MonstersScreenViewModel vm)
            return;
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;
        var result = await TaskDialog.ShowAsync(owner,
            "Apply to all victories",
            $"Set the victory count of ALL monsters to {vm.LumpValue}? This overwrites every counter at once (undoable via Ctrl+Z).",
            new TaskDialog.Choice("Apply", "apply", isDefault: true, danger: true),
            new TaskDialog.Choice("Cancel", "cancel", isCancel: true));
        if (result as string != "apply")
            return;
        vm.ApplyLumpVictoryCount();
    }

    private void OnRevise(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MonstersScreenViewModel vm)
            vm.ReviseCompCount();
    }
}
