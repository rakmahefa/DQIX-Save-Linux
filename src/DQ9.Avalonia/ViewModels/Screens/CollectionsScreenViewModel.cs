using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using DQ9_Cheat.DataManager;
using DQ9_Cheat.GameData;

namespace DQ9.Avalonia.ViewModels.Screens;

/// <summary>
/// Alchemy recipe states (WinForms AlchemyDataControl parity), item collection
/// flags and smart-item (DQVC) flags (WinForms ItemCollectionDataControl and
/// SmartItemDataControl parity) share one tab.
/// </summary>
public sealed class CollectionsScreenViewModel : EditorScreenViewModel
{
    private static readonly AlchemyState[] AlchemyStates =
    {
        AlchemyState.NotAppear,
        AlchemyState.Appear,
        AlchemyState.Created,
    };

    private string _alchemyCompCount = "0";
    private string _collectionCompCount = "0";
    private string _smartCompCount = "0";
    private AlchemyRow? _selectedAlchemy;
    private AlchemyState _selectedAlchemyState;

    public CollectionsScreenViewModel()
    {
        var recipes = ItemDataList.GetAllAlchemyList()
            .Where(i => i.AlchemyIndex >= 0)
            .GroupBy(i => i.AlchemyIndex)
            .OrderBy(g => g.Key);
        Alchemy = new ObservableCollection<AlchemyRow>(
            recipes.Select(g => new AlchemyRow(g.Key, BuildRecipeName(g.ToList()))));
        AlchemyStateList = new ObservableCollection<AlchemyState>(AlchemyStates);
        // UX-14: labelled display rows instead of the raw engine enum.
        // (built before SelectedAlchemy — its setter syncs the option row)
        AlchemyStateOptions = new ObservableCollection<EnumOption<AlchemyState>>(AlchemyStates
            .Select(s => new EnumOption<AlchemyState>(s, s.ToDisplay())));
        SelectedAlchemy = Alchemy.FirstOrDefault();

        Collection = new ObservableCollection<FlagRow>(
            ItemDataList.GetAllCollectionList()
                .Select(i => new FlagRow(i.DataIndex, i.Name,
                    idx => Data.ItemCollectionData.IsItemCollectionHold(idx),
                    (idx, v) => Data.ItemCollectionData.SetItemCollectionHold(idx, v))));
        SmartItems = new ObservableCollection<FlagRow>(
            ItemDataList.GetAllSmartList()
                .Select(i => new FlagRow(i.DataIndex, i.Name,
                    idx => Data.SmartItemData.IsSmartItemHold(idx),
                    (idx, v) => Data.SmartItemData.SetSmartItemHold(idx, v))));
    }

    public ObservableCollection<AlchemyRow> Alchemy { get; }

    public ObservableCollection<AlchemyState> AlchemyStateList { get; }

    /// <summary>UX-14: the combo binds these labelled rows, not the raw enum.</summary>
    public ObservableCollection<EnumOption<AlchemyState>> AlchemyStateOptions { get; }

    private EnumOption<AlchemyState>? _selectedAlchemyStateOption;

    public EnumOption<AlchemyState>? SelectedAlchemyStateOption
    {
        get => _selectedAlchemyStateOption;
        set
        {
            if (!Set(ref _selectedAlchemyStateOption, value) || value == null)
                return;
            SelectedAlchemyState = value.Value;
        }
    }

    public ObservableCollection<FlagRow> Collection { get; }

    public ObservableCollection<FlagRow> SmartItems { get; }

    public string AlchemyCompCount
    {
        get => _alchemyCompCount;
        private set => Set(ref _alchemyCompCount, value);
    }

    public string CollectionCompCount
    {
        get => _collectionCompCount;
        private set => Set(ref _collectionCompCount, value);
    }

    public string SmartCompCount
    {
        get => _smartCompCount;
        private set => Set(ref _smartCompCount, value);
    }

    public AlchemyRow? SelectedAlchemy
    {
        get => _selectedAlchemy;
        set
        {
            if (!Set(ref _selectedAlchemy, value))
                return;
            if (value == null || !EngineGuard.Ready)
                return;
            _selectedAlchemyState = Data.AlchemyData.GetAlchemyState(value.Index);
            _selectedAlchemyStateOption = AlchemyStateOptions.FirstOrDefault(o => o.Value == _selectedAlchemyState);
            Raise(nameof(SelectedAlchemyState));
            Raise(nameof(SelectedAlchemyStateOption));
        }
    }

    public AlchemyState SelectedAlchemyState
    {
        get => _selectedAlchemyState;
        set
        {
            if (!Set(ref _selectedAlchemyState, value))
                return;
            if (SelectedAlchemy == null || IsRefreshing)
                return;
            Data.AlchemyData.SetAlchemyState(SelectedAlchemy.Index, value);
            RefreshCounts();
        }
    }

    public void ReviseCollectionCompCount()
    {
        Data.ItemCollectionData.ReviseCompCount();
        RefreshCounts();
    }

    public void ReviseSmartCompCount()
    {
        Data.SmartItemData.ReviseCompCount();
        RefreshCounts();
    }

    private static string BuildRecipeName(System.Collections.Generic.List<ItemDataBase> items)
    {
        var names = items.Select(i => i.Name).Distinct().Take(4);
        var text = string.Join(" + ", names);
        if (items.Select(i => i.Name).Distinct().Count() > 4)
            text += " ...";
        return text;
    }

    private void RefreshCounts()
    {
        AlchemyCompCount = Data.AlchemyData.CompCount.ToString();
        CollectionCompCount = Data.ItemCollectionData.CompCount.ToString();
        SmartCompCount = Data.SmartItemData.CompCount.ToString();
    }

    /// <inheritdoc />
    public override string Header => "Alchemy";

    protected override void OnRefresh()
    {
        foreach (var row in Alchemy)
            row.Reload();
        foreach (var row in Collection)
            row.Reload();
        foreach (var row in SmartItems)
            row.Reload();
        RefreshCounts();
        if (SelectedAlchemy != null)
        {
            _selectedAlchemyState = Data.AlchemyData.GetAlchemyState(SelectedAlchemy.Index);
            _selectedAlchemyStateOption = AlchemyStateOptions.FirstOrDefault(o => o.Value == _selectedAlchemyState);
            Raise(nameof(SelectedAlchemyState));
            Raise(nameof(SelectedAlchemyStateOption));
        }
    }
}

/// <summary>One alchemy recipe and its progression state.</summary>
public sealed class AlchemyRow : ObservableObject
{
    private string _display;

    public AlchemyRow(int index, string name)
    {
        Index = index;
        Name = name;
        _display = BuildDisplay(index, name);
    }

    public int Index { get; }

    public string Name { get; }

    public string Display
    {
        get => _display;
        private set => Set(ref _display, value);
    }

    public void Reload() => Display = BuildDisplay(Index, Name);

    private static string BuildDisplay(int index, string name)
    {
        if (!EngineGuard.Ready)
            return $"#{index + 1}: {name}";
        var state = SaveDataManager.Instance.SaveData.AlchemyData.GetAlchemyState(index);
        return $"#{index + 1}: {name} - {state.ToDisplay()}";
    }
}

/// <summary>A hold-flag row backed by an engine flag array (collection / smart items).</summary>
public sealed class FlagRow : ObservableObject
{
    private readonly Func<int, bool> _get;
    private readonly Action<int, bool> _set;
    private string _display;
    private bool _isChecked;

    public FlagRow(int dataIndex, string name, Func<int, bool> get, Action<int, bool> set)
    {
        DataIndex = dataIndex;
        Name = name;
        _get = get;
        _set = set;
        _isChecked = EngineGuard.Ready && get(dataIndex);
        _display = BuildDisplay(dataIndex, name, _isChecked);
    }

    public int DataIndex { get; }

    public string Name { get; }

    public string Display
    {
        get => _display;
        private set => Set(ref _display, value);
    }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (!Set(ref _isChecked, value))
                return;
            _set(DataIndex, value);
            Display = BuildDisplay(DataIndex, Name, value);
        }
    }

    public void Reload()
    {
        Set(ref _isChecked, _get(DataIndex));
        Display = BuildDisplay(DataIndex, Name, _isChecked);
    }

    private static string BuildDisplay(int dataIndex, string name, bool hold)
    {
        return $"{dataIndex}: {name}{(hold ? " [held]" : "")}";
    }
}
