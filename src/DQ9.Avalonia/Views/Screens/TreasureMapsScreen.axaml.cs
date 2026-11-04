using Avalonia.Controls;
using Avalonia.Interactivity;
using DQ9.Avalonia.ViewModels.Screens;
using DQ9.Avalonia.Views;

namespace DQ9.Avalonia.Views.Screens;

public partial class TreasureMapsScreen : UserControl
{
    public TreasureMapsScreen() => InitializeComponent();

    private void OnNew(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TreasureMapsScreenViewModel vm)
            vm.CreateNewMap();
    }

    // UX-16: deletion is destructive and asks for confirmation first.
    private async void OnDelete(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not TreasureMapsScreenViewModel vm)
            return;
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;
        var result = await TaskDialog.ShowAsync(owner,
            "Delete map",
            $"Delete map #{(vm.SelectedMap?.Index ?? -1) + 1} from the grotto list? This cannot be undone except via Undo (Ctrl+Z).",
            new TaskDialog.Choice("Delete", "delete", isDefault: true, danger: true),
            new TaskDialog.Choice("Cancel", "cancel", isCancel: true));
        if (result as string != "delete")
            return;
        vm.DeleteMap();
    }

    private void OnUp(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TreasureMapsScreenViewModel vm)
            vm.MoveMapUp();
    }

    private void OnDown(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TreasureMapsScreenViewModel vm)
            vm.MoveMapDown();
    }
}
