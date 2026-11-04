using System;
using System.Collections.Generic;
using System.ComponentModel;
using DQ9_Cheat.DataManager;

namespace DQ9.Avalonia.ViewModels;

/// <summary>Channel from the business screens to the shell status bar.
/// Screens raise input-validation warnings (UX-01); the MainViewModel turns
/// them into a styled status-bar message so a failed parse is never silent.</summary>
public static class ScreenStatus
{
    public enum Kind
    {
        Info,
        Warning,
        Error,
    }

    /// <summary>Raised on the UI thread by screen view-models.</summary>
    public static event Action<Kind, string>? Reported;

    public static void Raise(Kind kind, string message) => Reported?.Invoke(kind, message);

    /// <summary>Convenience for input-validation warnings.</summary>
    public static void Warn(string message) => Raise(Kind.Warning, message);
}

/// <summary>
/// Base class of the P4 business screens. A screen reads and writes the
/// portable engine (<see cref="SaveDataManager"/>) and never touches Avalonia
/// types, so every screen is unit-testable from a plain xUnit process.
/// <para>
/// Editing follows the original WinForms behaviour: each bound property writes
/// through to the engine immediately, and the engine's <c>DataValue</c> layer
/// records undo/redo entries. <see cref="Refresh"/> pulls engine state back
/// into the bound properties; it is invoked by <see cref="MainViewModel"/>
/// after open / undo / redo / block switch.
/// </para>
/// <para>
/// UX-01: the class implements <see cref="INotifyDataErrorInfo"/> so numeric
/// fields whose parse failed report an error through the binding system — the
/// Fluent TextBox template then draws its native red border + message, and a
/// warning is echoed to the status bar. Errors are registered with
/// <see cref="SetFieldError"/> / <see cref="ClearFieldError"/>.
/// </para>
/// </summary>
public abstract class EditorScreenViewModel : ObservableObject, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _fieldErrors = new();
    private bool _refreshing;

    /// <summary>The shared engine instance.</summary>
    internal static SaveDataManager Engine => SaveDataManager.Instance;

    /// <summary>The save block currently displayed.</summary>
    internal static SaveData Data => SaveDataManager.Instance.SaveData;

    /// <summary>True while <see cref="OnRefresh"/> pulls values from the engine; setters must not write back.</summary>
    protected bool IsRefreshing => _refreshing;

    /// <summary>True when a save file is loaded in this session; bound to the screen IsEnabled.</summary>
    public bool IsFileLoaded => Engine.IsFileLoaded;

    /// <summary>Tab header of the screen.</summary>
    public abstract string Header { get; }

    // ---- INotifyDataErrorInfo (UX-01) --------------------------------------

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public bool HasErrors => _fieldErrors.Count > 0;

    public System.Collections.IEnumerable GetErrors(string? propertyName)
        => propertyName != null && _fieldErrors.TryGetValue(propertyName, out var list)
            ? list
            : System.Linq.Enumerable.Empty<string>();

    /// <summary>Registers (or replaces) the validation error of one field and
    /// echoes a warning to the status bar so the failed parse is visible even
    /// without looking at the field.</summary>
    protected void SetFieldError(string propertyName, string message)
    {
        _fieldErrors[propertyName] = new List<string> { message };
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        Raise(nameof(HasErrors));
        ScreenStatus.Warn(message);
    }

    /// <summary>Removes the validation error of one field (after a successful
    /// parse or an engine refresh).</summary>
    protected void ClearFieldError(string propertyName)
    {
        if (_fieldErrors.Remove(propertyName))
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            Raise(nameof(HasErrors));
        }
    }

    /// <summary>Clears every field error; called by <see cref="Refresh"/> so
    /// stale red borders do not survive an undo / reload.</summary>
    protected void ClearAllFieldErrors()
    {
        foreach (var key in new List<string>(_fieldErrors.Keys))
        {
            _fieldErrors.Remove(key);
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(key));
        }
        Raise(nameof(HasErrors));
    }

    /// <summary>Parse delegate matching <c>int.TryParse</c>-style method
    /// groups so call sites can pass the BCL parser directly.</summary>
    protected delegate bool TryParse<T>(string text, out T value);

    /// <summary>Shared numeric-parse helper (UX-01): reports a field error and
    /// returns false when the text is not a valid number of the target type;
    /// returns true (and clears any error) with the parsed value.</summary>
    protected bool ValidateParse<T>(string propertyName, string label, string text,
        TryParse<T> tryParse, string rangeHint, out T value)
    {
        if (tryParse(text, out value!) && value != null)
        {
            ClearFieldError(propertyName);
            return true;
        }
        value = default!;
        SetFieldError(propertyName, $"{label} must be {rangeHint}.");
        return false;
    }

    /// <summary>Runs an action with engine writes suppressed (list-selection
    /// loads must not record undo entries or raise validation warnings —
    /// otherwise merely selecting a row would flag the save as dirty).</summary>
    protected void SuppressWrites(Action action)
    {
        var wasRefreshing = _refreshing;
        _refreshing = true;
        try
        {
            action();
        }
        finally
        {
            _refreshing = wasRefreshing;
        }
    }

    public void Refresh()
    {
        if (_refreshing || !Engine.IsFileLoaded)
            return;
        _refreshing = true;
        try
        {
            ClearAllFieldErrors();
            OnRefresh();
        }
        finally
        {
            _refreshing = false;
        }
    }

    protected abstract void OnRefresh();
}
