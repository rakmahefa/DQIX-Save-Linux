using Avalonia.Controls;
using Avalonia.Interactivity;
using DQ9.Avalonia.ViewModels.Screens;

namespace DQ9.Avalonia.Views.Screens;

public partial class CollectionsScreen : UserControl
{
    public CollectionsScreen() => InitializeComponent();

    private void OnReviseCollection(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CollectionsScreenViewModel vm)
            vm.ReviseCollectionCompCount();
    }

    private void OnReviseSmart(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CollectionsScreenViewModel vm)
            vm.ReviseSmartCompCount();
    }
}
