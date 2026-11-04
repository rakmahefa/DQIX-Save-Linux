using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DQ9.Avalonia.ViewModels;

namespace DQ9.Avalonia.Views;

/// <summary>
/// Modal searchable list (WinForms ItemSelectWindow / HandleSelectWindow
/// replacement). All behaviour lives in <see cref="SearchPickerViewModel"/>;
/// this shell only forwards OK / cancel / double-click and exposes the
/// asynchronous <see cref="ShowPickerAsync"/> entry point.
/// <para>UX-09: OK is the default button (Enter confirms), Cancel reacts to
/// Escape, and the search box takes focus when the dialog opens. UX-10: the
/// dialog is resizable with minimum sizes so no list row is clipped.</para>
/// </summary>
public partial class SearchPickerWindow : Window
{
    public SearchPickerWindow()
    {
        InitializeComponent();
    }

    /// <summary>Runs the picker modally and returns the picked engine object,
    /// or null when the user cancels (or picks the empty row for items).</summary>
    public static async Task<object?> ShowPickerAsync(Window owner, SearchPickerViewModel viewModel)
    {
        var window = new SearchPickerWindow { DataContext = viewModel };
        await window.ShowDialog(owner);
        return viewModel.Result;
    }

    /// <summary>UX-09: keyboard users land in the search box, not nowhere.</summary>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        SearchBox.Focus();
    }

    private void OnOk(object? sender, RoutedEventArgs e) => CloseOk();

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();

    private void OnRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (List.SelectedItem is not null)
            CloseOk();
    }

    private void CloseOk()
    {
        (DataContext as SearchPickerViewModel)?.Confirm();
        Close();
    }
}
