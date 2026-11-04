using System;
using System.IO;
using System.Threading.Tasks;
using DQ9_Cheat.DataManager;
using DQ9_Cheat.GameData;

namespace DQ9.Avalonia.ViewModels;

/// <summary>
/// P3 shell view-model: open / save / save-as / close, block selection,
/// modified state and error display. Business operations go straight to the
/// portable DQ9.Core engine — exactly as spec.md §9 requires.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly Func<Task<string?>> _pickOpenFile;
    private readonly Func<Task<string?>> _pickSaveFile;
    private readonly Func<Task<bool>>? _confirmDiscardEdits;
    private string? _filePath;
    private bool _isBlock1Checked = true;
    private string _title = "Dragon Quest IX Save Data Editor (Avalonia)";
    private string _statusText = "Open a save file to start.";
    private bool _hasError;
    private bool _hasWarning;
    private string _moneyInput = string.Empty;
    private string _playTimeText = "–";
    private string _savePlaceText = "–";
    private string _bankMoneyText = "–";
    private string _medalText = "–";
    private string _blockInfoText = string.Empty;

    public MainViewModel(Func<Task<string?>> pickOpenFile, Func<Task<string?>> pickSaveFile,
        Func<Task<bool>>? confirmDiscardEdits = null)
    {
        _pickOpenFile = pickOpenFile;
        _pickSaveFile = pickSaveFile;
        _confirmDiscardEdits = confirmDiscardEdits;
        SaveDataManager.Instance.UndoRedoMgr.SavedIndexChanged += OnSavedIndexChanged;
        // UX-01: screen input-validation warnings land in the status bar.
        ScreenStatus.Reported += OnScreenStatus;

        OpenCommand = new RelayCommand(async () => await OpenAsync(), () => true);
        SaveCommand = new RelayCommand(Save, () => HasOpenFile);
        SaveAsCommand = new RelayCommand(async () => await SaveAsAsync(), () => HasOpenFile);
        UndoCommand = new RelayCommand(() => { SaveDataManager.Instance.UndoRedoMgr.Undo(); RefreshFromEngine(); Screens.RefreshAll(); },
            () => HasOpenFile && SaveDataManager.Instance.UndoRedoMgr.IsUndo);
        RedoCommand = new RelayCommand(() => { SaveDataManager.Instance.UndoRedoMgr.Redo(); RefreshFromEngine(); Screens.RefreshAll(); },
            () => HasOpenFile && SaveDataManager.Instance.UndoRedoMgr.IsRedo);
        ApplyMoneyCommand = new RelayCommand(ApplyMoney,
            () => HasOpenFile && uint.TryParse(MoneyInput, out _));
        CopyBlock1ToBlock2Command = new RelayCommand(CopyBlock1ToBlock2, () => HasOpenFile);
        ExitCommand = new RelayCommand(() => ExitRequested?.Invoke());
    }

    /// <summary>Raised by ExitCommand; the window closes itself.</summary>
    public event Action? ExitRequested;

    public RelayCommand ExitCommand { get; }

    public RelayCommand OpenCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand SaveAsCommand { get; }
    public RelayCommand UndoCommand { get; }
    public RelayCommand RedoCommand { get; }
    public RelayCommand ApplyMoneyCommand { get; }
    public RelayCommand CopyBlock1ToBlock2Command { get; }

    /// <summary>Aggregate of the P4 business screens.</summary>
    public ScreenRegistry Screens { get; } = new();

    public string Title
    {
        get => _title;
        private set => Set(ref _title, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => Set(ref _statusText, value);
    }

    public bool HasError
    {
        get => _hasError;
        private set => Set(ref _hasError, value);
    }

    /// <summary>Amber severity of the status bar (input-validation hints).</summary>
    public bool HasWarning
    {
        get => _hasWarning;
        private set => Set(ref _hasWarning, value);
    }

    /// <summary>UX-04: true when the open save has edits not written to disk;
    /// mirrored into the status bar (the title-bar * alone is invisible).</summary>
    public bool IsDirty => HasOpenFile && SaveDataManager.Instance.UndoRedoMgr.EditFlag;

    public string MoneyInput
    {
        get => _moneyInput;
        set
        {
            if (Set(ref _moneyInput, value))
                ApplyMoneyCommand.RaiseCanExecuteChanged();
        }
    }

    public string PlayTimeText { get => _playTimeText; private set => Set(ref _playTimeText, value); }
    public string BankMoneyText { get => _bankMoneyText; private set => Set(ref _bankMoneyText, value); }
    public string MedalText { get => _medalText; private set => Set(ref _medalText, value); }
    public string SavePlaceText { get => _savePlaceText; private set => Set(ref _savePlaceText, value); }

    public string BlockInfoText
    {
        get => _blockInfoText;
        private set
        {
            if (Set(ref _blockInfoText, value))
                Raise(nameof(HasBlockInfo));
        }
    }

    /// <summary>UX-22: hides the bare "–" placeholder until a file is open.</summary>
    public bool HasBlockInfo => !string.IsNullOrEmpty(_blockInfoText);

    public bool HasOpenFile => _filePath != null;

    public bool HasIntermission => SaveDataManager.Instance.IsThereIntermissionData;

    public bool IsBlock1Checked
    {
        get => _isBlock1Checked;
        set
        {
            if (!Set(ref _isBlock1Checked, value))
                return;
            if (value)
                SaveDataManager.Instance.SelectedDataIndex = 0;
            RefreshFromEngine();
            Screens.RefreshAll();
        }
    }

    public bool IsBlock2Checked
    {
        get => !_isBlock1Checked;
        set
        {
            if (!value)
                return;
            Set(ref _isBlock1Checked, false);
            SaveDataManager.Instance.SelectedDataIndex = 1;
            RefreshFromEngine();
            Screens.RefreshAll();
            Raise(nameof(IsBlock1Checked));
        }
    }

    /// <summary>Opens through the file-picker dialog. A dirty session is
    /// guarded by a Save / Discard / Cancel dialog (UX-04).</summary>
    public async Task OpenAsync()
    {
        if (!await ConfirmDiscardEditsAsync())
            return;
        var path = await _pickOpenFile();
        if (path == null)
            return;
        OpenFile(path);
    }

    /// <summary>Asks the user what to do with unsaved edits before a
    /// destructive action. Returns false when the action must be aborted.</summary>
    private async Task<bool> ConfirmDiscardEditsAsync()
    {
        if (!IsDirty || _confirmDiscardEdits == null)
            return true;
        return await _confirmDiscardEdits();
    }

    /// <summary>Opens an explicit path (used by the smoke harness and drag-drop later).</summary>
    public bool OpenFile(string path)
    {
        try
        {
            using var input = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(input);
            var headerLength = SaveDataManager.Instance.CheckFile(reader);
            if (headerLength < 0)
            {
                ReportError("File is either in incorrect format or corrupted. Load failed.");
                return false;
            }

            input.Position = headerLength;
            SaveDataManager.Instance.ReadData(reader);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            ReportError($"Could not read '{path}': {ex.Message}");
            return false;
        }

        _filePath = path;
        SaveDataManager.Instance.SelectedDataIndex = 0;
        _isBlock1Checked = true;
        HasError = false;
        HasWarning = false;
        StatusText = $"Loaded '{Path.GetFileName(path)}'.";
        Raise(nameof(HasOpenFile));
        Raise(nameof(IsBlock1Checked));
        Raise(nameof(IsBlock2Checked));
        Raise(nameof(HasIntermission));
        Raise(nameof(IsDirty));
        RefreshFromEngine();
        Screens.RefreshAll();
        RefreshCommands();
        return true;
    }

    public void Save()
    {
        if (_filePath == null)
            return;
        SaveToFile(_filePath);
    }

    public async Task SaveAsAsync()
    {
        if (!HasOpenFile)
            return;
        var path = await _pickSaveFile();
        if (path == null)
            return;
        if (SaveToFile(path))
            _filePath = path;
    }

    private bool SaveToFile(string path)
    {
        try
        {
            using var output = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(output);
            SaveDataManager.Instance.WriteData(writer);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            ReportError($"Could not write '{path}': {ex.Message}");
            return false;
        }

        HasError = false;
        HasWarning = false;
        RefreshTitle();
        Raise(nameof(IsDirty));
        StatusText = $"Saved '{Path.GetFileName(path)}'.";
        return true;
    }

    private void ApplyMoney()
    {
        if (!uint.TryParse(MoneyInput, out var value) || _filePath == null)
            return;
        SaveDataManager.Instance.SaveData.BasisData.Money.Value = value;
        RefreshFromEngine();
        StatusText = $"Money set to {value}.";
    }

    private void CopyBlock1ToBlock2()
    {
        SaveDataManager.Instance.CopyIntermissionData(0);
        Raise(nameof(HasIntermission));
        Raise(nameof(IsDirty));
        RefreshFromEngine();
        Screens.RefreshAll();
        RefreshCommands();
        StatusText = "Block 1 copied to block 2 (intermission).";
    }

    private void RefreshFromEngine()
    {
        if (!HasOpenFile)
            return;
        var data = SaveDataManager.Instance.SaveData.BasisData;
        var manager = SaveDataManager.Instance;
        MoneyInput = data.Money.Value.ToString();
        BankMoneyText = data.BankMoney.Value.ToString();
        MedalText = data.Medal.Value.ToString();
        PlayTimeText = $"{data.PlayTimeHour.Value} h {data.PlayTimeMinute.Value:D2} min {data.PlayTimeSecond.Value:D2} s";
        SavePlaceText = data.SavePlace?.Name ?? "unknown";
        BlockInfoText = manager.SelectedDataIndex == 0
            ? "Block 1" + (manager.IsThereIntermissionData ? " (block 2 available)" : " (no intermission data)")
            : "Block 2 (intermission)";
        RefreshTitle();
        RefreshCommands();
    }

    private void RefreshTitle()
    {
        var dirty = SaveDataManager.Instance.UndoRedoMgr.EditFlag;
        Title = (_filePath == null ? "Dragon Quest IX Save Data Editor" : _filePath + (dirty ? " *" : ""))
                + " - Dragon Quest IX Save Data Editor (Avalonia)";
        Raise(nameof(IsDirty));
    }

    private void RefreshCommands()
    {
        SaveCommand.RaiseCanExecuteChanged();
        SaveAsCommand.RaiseCanExecuteChanged();
        UndoCommand.RaiseCanExecuteChanged();
        RedoCommand.RaiseCanExecuteChanged();
        ApplyMoneyCommand.RaiseCanExecuteChanged();
        CopyBlock1ToBlock2Command.RaiseCanExecuteChanged();
    }

    private void OnSavedIndexChanged()
    {
        RefreshTitle();
        RefreshCommands();
    }

    private void OnScreenStatus(ScreenStatus.Kind kind, string message)
    {
        switch (kind)
        {
            case ScreenStatus.Kind.Error:
                ReportError(message);
                break;
            case ScreenStatus.Kind.Warning:
                HasError = false;
                HasWarning = true;
                StatusText = message;
                break;
            default:
                HasError = false;
                HasWarning = false;
                StatusText = message;
                break;
        }
    }

    private void ReportError(string message)
    {
        HasError = true;
        HasWarning = false;
        StatusText = message;
    }
}
