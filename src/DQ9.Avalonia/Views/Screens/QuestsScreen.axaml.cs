using Avalonia.Controls;
using Avalonia.Interactivity;
using DQ9.Avalonia.ViewModels.Screens;

namespace DQ9.Avalonia.Views.Screens;

public partial class QuestsScreen : UserControl
{
    public QuestsScreen() => InitializeComponent();

    private void OnReviseClear(object? sender, RoutedEventArgs e)
    {
        if (DataContext is QuestsScreenViewModel vm)
            vm.ReviseClearCount();
    }

    private void OnReviseOrder(object? sender, RoutedEventArgs e)
    {
        if (DataContext is QuestsScreenViewModel vm)
            vm.ReviseOrderCount();
    }
}
