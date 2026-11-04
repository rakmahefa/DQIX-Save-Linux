using Avalonia.Controls;
using Avalonia.Interactivity;
using DQ9.Avalonia.ViewModels.Screens;

namespace DQ9.Avalonia.Views.Screens;

public partial class TitlesScreen : UserControl
{
    public TitlesScreen() => InitializeComponent();

    private void OnReviseCount(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TitlesScreenViewModel vm)
            vm.ReviseTitleCount();
    }
}
