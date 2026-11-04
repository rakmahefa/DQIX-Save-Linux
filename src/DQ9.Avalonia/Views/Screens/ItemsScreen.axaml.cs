using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DQ9.Avalonia.ViewModels;
using DQ9.Avalonia.ViewModels.Screens;

namespace DQ9.Avalonia.Views.Screens;

public partial class ItemsScreen : UserControl
{
    public ItemsScreen() => InitializeComponent();

    private void OnParty(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ItemsScreenViewModel vm || sender is not Button { CommandParameter: string text })
            return;
        vm.SelectedPartyIndex = int.Parse(text);
    }

    private async void OnSearchItem(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ItemsScreenViewModel vm)
            return;
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;
        var result = await SearchPickerWindow.ShowPickerAsync(owner, vm.CreateItemPicker());
        vm.ApplyPickedItem(result);
    }
}
