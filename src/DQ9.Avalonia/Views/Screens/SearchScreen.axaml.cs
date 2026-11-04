using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DQ9.Avalonia.ViewModels.Screens;

namespace DQ9.Avalonia.Views.Screens;

public partial class SearchScreen : UserControl
{
    public SearchScreen() => InitializeComponent();

    private void OnGenerate(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SearchScreenViewModel vm)
            vm.GenerateCacheAsync();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SearchScreenViewModel vm)
            vm.CancelGeneration();
    }

    private void OnReload(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SearchScreenViewModel vm)
            vm.LoadOrPrepareService();
    }

    private void OnSearch(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SearchScreenViewModel vm)
            vm.Search();
    }
}
