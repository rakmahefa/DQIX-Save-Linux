using System;
using System.Collections.Generic;
using DQ9_Cheat.GameData;

namespace DQ9.Avalonia.ViewModels.Screens;

/// <summary>
/// A checkbox row bound to an engine bit. The row re-reads the engine in
/// <see cref="Reload"/> (called by the owning screen on refresh) and writes
/// through the engine on user edits, so undo/redo support comes for free.
/// </summary>
public sealed class CheckRow : ObservableObject
{
    private readonly Func<bool> _get;
    private readonly Action<bool> _set;
    private bool _isChecked;

    public CheckRow(string name, Func<bool> get, Action<bool> set)
    {
        Name = name;
        _get = get;
        _set = set;
    }

    public string Name { get; }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (!Set(ref _isChecked, value))
                return;
            _set(value);
        }
    }

    /// <summary>Pulls the current engine state into the bound property.</summary>
    public void Reload() => Set(ref _isChecked, _get());
}

/// <summary>An editable numeric list row (label + value) used by grids.</summary>
public sealed class NumRow : ObservableObject
{
    private readonly Func<uint> _get;
    private readonly Action<uint> _set;
    private uint _value;

    public NumRow(string name, Func<uint> get, Action<uint> set)
    {
        Name = name;
        _get = get;
        _set = set;
    }

    public string Name { get; }

    public uint Value
    {
        get => _value;
        set
        {
            if (Set(ref _value, value))
                _set(value);
        }
    }

    public void Reload() => Set(ref _value, _get());
}

/// <summary>
/// One job row of the Characters "Job levels" grid: the level and the
/// experience points of a single vocation, both addressed through the job's
/// <c>DataIndex</c> (the save-file slot order, like the WinForms status-edit
/// group — not the display order of the job list). The two cells re-read the
/// engine in <see cref="Reload"/> and write through it on edits, so undo/redo
/// support comes for free.
/// </summary>
public sealed class JobLevelRow : ObservableObject
{
    public JobLevelRow(string name, NumRow level, NumRow experience)
    {
        Name = name;
        Level = level;
        Experience = experience;
    }

    public string Name { get; }

    /// <summary>Job level (engine range 0–99).</summary>
    public NumRow Level { get; }

    /// <summary>Job experience points (engine range 0–999 999 999).</summary>
    public NumRow Experience { get; }

    /// <summary>Pulls both engine values into the bound properties.</summary>
    public void Reload()
    {
        Level.Reload();
        Experience.Reload();
    }
}

/// <summary>
/// A combo row of the General screen bound to one gesture direction byte of
/// the engine (up / left / right / down 1-4). The row re-reads the engine in
/// <see cref="Reload"/> and writes the selected gesture index through on
/// user edits.
/// </summary>
public sealed class GestureRow : ObservableObject
{
    private readonly Func<byte> _get;
    private readonly Action<byte> _set;
    private readonly IReadOnlyList<Gesture> _options;
    private Gesture? _selected;

    public GestureRow(string name, Func<byte> get, Action<byte> set, IReadOnlyList<Gesture> options)
    {
        Name = name;
        _get = get;
        _set = set;
        _options = options;
        // no engine read here: the screen is built before any file is open;
        // Reload() pulls the real value on refresh
        _selected = options.Count == 0 ? null : options[0];
    }

    public string Name { get; }

    public IReadOnlyList<Gesture> Options => _options;

    public Gesture? Selected
    {
        get => _selected;
        set
        {
            if (!Set(ref _selected, value) || value == null)
                return;
            _set((byte)value.Index);
        }
    }

    public void Reload()
    {
        var index = Math.Min(_get(), _options.Count - 1);
        Set(ref _selected, index >= 0 ? _options[index] : null);
    }
}

/// <summary>
/// An item option for the ComboBox-based item pickers. <see cref="Item"/> is
/// null for the "None" entry, which maps to <c>ushort.MaxValue</c> in the save.
/// </summary>
public sealed class ItemOption
{
    public static readonly ItemOption None = new(null);

    public ItemOption(ItemDataBase? item) => Item = item;

    public ItemDataBase? Item { get; }

    public string Display => Item != null
        ? $"{Item.Name} ({Item.Value})"
        : "(none)";

    public override string ToString() => Display;
}

/// <summary>A selectable item slot in an inventory list (index + name + count).</summary>
public sealed class ItemSlotRow : ObservableObject
{
    private string _name;
    private string _count;

    public ItemSlotRow(int index, string name, string count)
    {
        Index = index;
        _name = name;
        _count = count;
    }

    public int Index { get; }

    public string Name
    {
        get => _name;
        set => Set(ref _name, value);
    }

    public string Count
    {
        get => _count;
        set => Set(ref _count, value);
    }

    public string Display => $"#{Index + 1}  {Name}  x{Count}";
}
