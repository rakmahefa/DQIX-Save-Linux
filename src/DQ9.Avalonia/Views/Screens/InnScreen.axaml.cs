using Avalonia.Controls;
using Avalonia.Interactivity;
using DQ9.Avalonia.ViewModels.Screens;
using DQ9.Avalonia.Views;

namespace DQ9.Avalonia.Views.Screens;

public partial class InnScreen : UserControl
{
    public InnScreen() => InitializeComponent();

    private void OnUp(object? sender, RoutedEventArgs e)
    {
        if (DataContext is InnScreenViewModel vm)
            vm.MoveVisitorUp();
    }

    private void OnDown(object? sender, RoutedEventArgs e)
    {
        if (DataContext is InnScreenViewModel vm)
            vm.MoveVisitorDown();
    }

    // UX-16: deletion is destructive and asks for confirmation first.
    private async void OnDelete(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not InnScreenViewModel vm)
            return;
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;
        var result = await TaskDialog.ShowAsync(owner,
            "Delete guest",
            $"Remove guest #{(vm.SelectedVisitor?.Index ?? -1) + 1} from Rikka's inn? This cannot be undone except via Undo (Ctrl+Z).",
            new TaskDialog.Choice("Delete", "delete", isDefault: true, danger: true),
            new TaskDialog.Choice("Cancel", "cancel", isCancel: true));
        if (result as string != "delete")
            return;
        vm.DeleteVisitor();
    }

    private void OnCreate(object? sender, RoutedEventArgs e)
    {
        if (DataContext is InnScreenViewModel vm)
            vm.CreateVisitor();
    }
}
