using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using DQ9_Cheat.GameData;

namespace DQ9.Avalonia.ViewModels;

/// <summary>One selectable row of a search picker. <see cref="Value"/> is the
/// engine object to write back (item, job, title…) or null for the empty row.</summary>
public sealed class PickerRow
{
    public PickerRow(string display, object? value)
    {
        Display = display;
        Value = value;
    }

    public string Display { get; }

    public object? Value { get; }

    public override string ToString() => Display;
}

/// <summary>
/// Searchable modal list shared by the item and handle pickers (WinForms
/// <c>ItemSelectWindow</c> / <c>HandleSelectWindow</c> parity): a type selector
/// (when several contexts exist), a case-insensitive regular-expression search
/// box and a result list. The Avalonia window is a thin shell around this
/// class so the filtering and selection logic is unit-testable headlessly.
/// </summary>
public sealed class SearchPickerViewModel : ObservableObject
{
    private readonly Func<string, IReadOnlyList<PickerRow>>[] _builders;
    private string _search = "";
    private int _selectedTypeIndex;
    private PickerRow? _selectedRow;
    private bool _confirmed;

    public SearchPickerViewModel(string title,
        IReadOnlyList<(string Label, Func<string, IReadOnlyList<PickerRow>> Build)> types)
    {
        Title = title;
        Types = types.Select(t => t.Label).ToArray();
        _builders = types.Select(t => t.Build).ToArray();
        TypeSelectorVisible = Types.Length > 1;
        Rebuild();
    }

    public string Title { get; }

    public string[] Types { get; }

    public bool TypeSelectorVisible { get; }

    public string Search
    {
        get => _search;
        set { if (Set(ref _search, value)) Rebuild(); }
    }

    public int SelectedTypeIndex
    {
        get => _selectedTypeIndex;
        set { if (Set(ref _selectedTypeIndex, value)) Rebuild(); }
    }

    public ObservableCollection<PickerRow> Rows { get; } = new();

    public PickerRow? SelectedRow
    {
        get => _selectedRow;
        set { if (Set(ref _selectedRow, value)) Raise(nameof(ConfirmEnabled)); }
    }

    public bool ConfirmEnabled => SelectedRow != null;

    /// <summary>True after <see cref="Confirm"/> ran with a valid selection.</summary>
    public bool Confirmed => _confirmed;

    /// <summary>The picked engine object, or null for the empty row / cancel.</summary>
    public object? Result => _confirmed ? SelectedRow?.Value : null;

    /// <summary>Rebuilds <see cref="Rows"/> from the current type and search text.
    /// UX-09: the preselection skips the “(Empty — clears slot)” row so a stray
    /// Enter / OK cannot wipe the slot; the first real row is selected instead.</summary>
    public void Rebuild()
    {
        Rows.Clear();
        foreach (var row in _builders[Math.Clamp(_selectedTypeIndex, 0, _builders.Length - 1)](_search ?? ""))
            Rows.Add(row);
        SelectedRow = Rows.FirstOrDefault(r => r.Value != null) ?? Rows.FirstOrDefault();
        Raise(nameof(ConfirmEnabled));
    }

    public void Confirm()
    {
        if (SelectedRow != null)
            _confirmed = true;
    }

    /// <summary>Case-insensitive regex like the WinForms clients; an invalid
    /// pattern falls back to a plain case-insensitive substring match.</summary>
    public static bool Matches(string input, string search)
    {
        if (string.IsNullOrEmpty(search))
            return true;
        Regex regex;
        try
        {
            regex = new Regex(search, RegexOptions.IgnoreCase);
        }
        catch (ArgumentException)
        {
            return input.Contains(search, StringComparison.OrdinalIgnoreCase);
        }
        return regex.IsMatch(input);
    }

    /// <summary>Picker for the item contexts of the WinForms ItemSelectWindow.
    /// The first row is the "(Empty)" entry that clears the slot.</summary>
    public static SearchPickerViewModel ForItemTypes(IReadOnlyList<(ItemType Type, string Label)> types)
    {
        return new SearchPickerViewModel("Select item", types
            .Select(t => (t.Label, (Func<string, IReadOnlyList<PickerRow>>)(search => BuildItemRows(t.Type, search))))
            .ToArray());
    }

    /// <summary>Picker for the profile handle of the WinForms HandleSelectWindow:
    /// occupations plus accolades (titles) filtered by the profile sex.</summary>
    public static SearchPickerViewModel ForHandle(byte sex)
    {
        return new SearchPickerViewModel("Select profile title",
            new (string, Func<string, IReadOnlyList<PickerRow>>)[]
            {
                ("Occupations", search => BuildJobRows(search)),
                ("Accolades", search => BuildTitleRows(sex, search)),
            });
    }

    private static IReadOnlyList<PickerRow> BuildItemRows(ItemType type, string search)
    {
        // UX-09: the pseudo row says what it does — one stray click no longer
        // silently clears the slot.
        var rows = new List<PickerRow> { new("(Empty — clears slot)", null) };
        // GetList(…, null) returns the whole table; the shared Matches() keeps
        // one tolerant matching code path (an invalid regex must not throw).
        foreach (var item in ItemDataList.GetList(type, null))
            if (Matches(item.Name, search))
                rows.Add(new PickerRow(item.Name, item));
        return rows;
    }

    private static IReadOnlyList<PickerRow> BuildJobRows(string search)
    {
        var rows = new List<PickerRow>();
        foreach (var job in ProfileJobList.List)
        {
            if (!Matches(job.Name, search))
                continue;
            // UX-23: the engine pseudo-entry (index 700–799 = “keep current”)
            // must not look like one of the 59 real occupations.
            rows.Add(job.Index >= 700 && job.Index < 800
                ? new PickerRow("(Keep current vocation)", job)
                : new PickerRow(job.Name, job));
        }
        return rows;
    }

    private static IReadOnlyList<PickerRow> BuildTitleRows(byte sex, string search)
    {
        var rows = new List<PickerRow>();
        foreach (var title in TitleDataList.List)
        {
            var name = sex == 0 ? title.MaleTitleName : title.LadyTitleName;
            if (!string.IsNullOrEmpty(name) && Matches(name, search))
                rows.Add(new PickerRow(name, title));
        }
        return rows;
    }
}
