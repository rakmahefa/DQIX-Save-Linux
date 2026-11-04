using DQ9_Cheat.DataManager;
using DQ9_Cheat.GameData;
using System.Collections.ObjectModel;
using System.Linq;

namespace DQ9.Avalonia.ViewModels.Screens;

/// <summary>
/// Monster encyclopedia editor (WinForms MonsterDataControl parity): victory
/// counts, discovery flag and drop item counts for the 307 entries, plus the
/// lump-edit and comp-count revision tools.
/// </summary>
public sealed class MonstersScreenViewModel : EditorScreenViewModel
{
    private string _lumpValue = "0";
    private string _compCount = "0";
    private MonsterRow? _selectedMonster;
    private string _victory = "0";
    private string _itemCount1 = "0";
    private string _itemCount2 = "0";

    public MonstersScreenViewModel()
    {
        Monsters = new ObservableCollection<MonsterRow>(
            Enumerable.Range(0, MonsterDataManager.MonsterDataCount)
                .Select(i => new MonsterRow(i, MonsterDataList.List[i])));
        SelectedMonster = Monsters.FirstOrDefault();
    }

    public ObservableCollection<MonsterRow> Monsters { get; }

    public string CompCount
    {
        get => _compCount;
        private set => Set(ref _compCount, value);
    }

    public string LumpValue
    {
        get => _lumpValue;
        set => Set(ref _lumpValue, value);
    }

    public MonsterRow? SelectedMonster
    {
        get => _selectedMonster;
        set
        {
            if (!Set(ref _selectedMonster, value))
                return;
            LoadDetail();
        }
    }

    public string Victory
    {
        get => _victory;
        set
        {
            if (!Set(ref _victory, value) || SelectedMonster == null || IsRefreshing)
                return;
            if (!ValidateParse<ushort>(nameof(Victory), "Victories", value, ushort.TryParse,
                    "a whole number between 0 and 65 535", out var v))
                return;
            if (EngineGuard.Ready)
            {
                Data.MonsterDataManager.SetVictoryCount(SelectedMonster.Index, v);
                SelectedMonster.Reload();
            }
        }
    }

    public string ItemCount1
    {
        get => _itemCount1;
        set
        {
            if (!Set(ref _itemCount1, value) || SelectedMonster == null || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(ItemCount1), "Drop 1 count", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                Data.MonsterDataManager.SetItemCount1(SelectedMonster.Index, v);
        }
    }

    public string ItemCount2
    {
        get => _itemCount2;
        set
        {
            if (!Set(ref _itemCount2, value) || SelectedMonster == null || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(ItemCount2), "Drop 2 count", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                Data.MonsterDataManager.SetItemCount2(SelectedMonster.Index, v);
        }
    }

    /// <summary>Lump edit: writes the value to all victory counts in one undo group.</summary>
    public void ApplyLumpVictoryCount()
    {
        if (!ushort.TryParse(_lumpValue, out var value))
            return;
        Engine.UndoRedoMgr.BeginPluralEdit();
        for (var i = 0; i < MonsterDataManager.MonsterDataCount; i++)
            Data.MonsterDataManager.SetVictoryCount(i, value);
        Engine.UndoRedoMgr.EndPluralEdit();
        foreach (var monster in Monsters)
            monster.Reload();
        LoadDetail();
    }

    public void ReviseCompCount()
    {
        Data.MonsterDataManager.ReviseCompCount();
        CompCount = Data.MonsterDataManager.CompCount.ToString();
    }

    private void LoadDetail()
    {
        SuppressWrites(() =>
        {
            if (SelectedMonster == null || !EngineGuard.Ready)
                return;
            var index = SelectedMonster.Index;
            Victory = Data.MonsterDataManager.GetVictoryCount(index).ToString();
            ItemCount1 = Data.MonsterDataManager.GetItemCount1(index).ToString();
            ItemCount2 = Data.MonsterDataManager.GetItemCount2(index).ToString();
        });
    }

    /// <inheritdoc />
    public override string Header => "Monsters";

    protected override void OnRefresh()
    {
        foreach (var monster in Monsters)
            monster.Reload();
        CompCount = Data.MonsterDataManager.CompCount.ToString();
        LoadDetail();
    }
}

/// <summary>One monster entry in the list.</summary>
public sealed class MonsterRow : ObservableObject
{
    private string _display;

    public MonsterRow(int index, string name)
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
        var manager = SaveDataManager.Instance.SaveData.MonsterDataManager;
        var findOut = manager.IsFindOut(index);
        return $"#{index + 1}: {name} - {manager.GetVictoryCount(index)}{(findOut ? " [seen]" : "")}";
    }
}
