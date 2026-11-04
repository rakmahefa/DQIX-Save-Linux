using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using DQ9_Cheat.DataManager;
using DQ9.Avalonia.ViewModels;

namespace DQ9.Avalonia.Views;

public partial class MainWindow : Window
{
    private const string ChoiceSave = "save";
    private const string ChoiceDiscard = "discard";
    private const string ChoiceCancel = "cancel";

    private readonly MainViewModel _viewModel;
    private bool _closeConfirmed;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel(PickOpenFileAsync, PickSaveFileAsync, ConfirmDiscardEditsAsync);
        _viewModel.ExitRequested += () => Close();
        DataContext = _viewModel;
    }

    private async Task<string?> PickOpenFileAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open DQ9 save file",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("All files") { Patterns = ["*"] }]
        });
        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    private async Task<string?> PickSaveFileAsync()
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save DQ9 save file",
            DefaultExtension = "sav"
        });
        return file?.Path.LocalPath;
    }

    /// <summary>UX-04: the VM raises this before replacing an open save; the
    /// window decides through the Save / Discard / Cancel dialog.</summary>
    private async Task<bool> ConfirmDiscardEditsAsync()
    {
        var result = await ShowUnsavedChangesDialog();
        if (result == ChoiceSave)
        {
            _viewModel.Save();
            return true;
        }
        return result == ChoiceDiscard;
    }

    /// <summary>UX-04: closing (and Ctrl+Q) with unsaved edits asks first —
    /// Cancel aborts the close, Save writes and continues.</summary>
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (_closeConfirmed || !SaveDataManager.Instance.UndoRedoMgr.EditFlag)
            return;
        e.Cancel = true;
        var result = await ShowUnsavedChangesDialog();
        if (result is null or ChoiceCancel)
            return;
        if (result == ChoiceSave)
            _viewModel.Save();
        _closeConfirmed = true;
        Close();
    }

    /// <summary>Returns the dialog choice; null when Cancel / closed.</summary>
    private async Task<string?> ShowUnsavedChangesDialog()
    {
        var result = await TaskDialog.ShowAsync(this,
            "Unsaved changes",
            "The open save has edits that are not written to disk. Save them before continuing?",
            new TaskDialog.Choice("Save", ChoiceSave, isDefault: true),
            new TaskDialog.Choice("Discard", ChoiceDiscard, danger: true),
            new TaskDialog.Choice("Cancel", ChoiceCancel, isCancel: true));
        return result as string;
    }
}
