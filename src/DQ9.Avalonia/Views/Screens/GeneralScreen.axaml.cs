using Avalonia.Controls;
using Avalonia.Interactivity;
using DQ9.Avalonia.ViewModels;
using DQ9.Avalonia.ViewModels.Screens;

namespace DQ9.Avalonia.Views.Screens;

public partial class GeneralScreen : UserControl
{
    public GeneralScreen() => InitializeComponent();

    private async void OnPickHandle(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GeneralScreenViewModel vm)
            return;
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;
        var result = await SearchPickerWindow.ShowPickerAsync(owner, vm.CreateHandlePicker());
        vm.ApplyHandle(result);
        vm.Refresh();
    }
}
