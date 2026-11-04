using DQ9_Cheat.DataManager;
using System.Collections.ObjectModel;
using System.Linq;
using DQ9_Cheat.GameData;

namespace DQ9.Avalonia.ViewModels.Screens;

/// <summary>Title flags (WinForms TitleDataControl parity) and first-clear
/// records (WinForms FirstClearDataControl parity) share one tab.</summary>
public sealed class TitlesScreenViewModel : EditorScreenViewModel
{
    private string _titleCount = "0";
    private string _playHour = "0";
    private string _playMinute = "0";
    private string _playSecond = "0";
    private string _multiHour = "0";
    private string _multiMinute = "0";
    private string _multiSecond = "0";
    private string _victoryCount = "0";
    private string _alchemyCount = "0";
    private string _questCount = "0";
    private string _visitorCount = "0";
    private string _treasureMapCount = "0";
    private TitleElement? _selectedFirstClearTitle;

    public TitlesScreenViewModel()
    {
        Titles = new ObservableCollection<TitleRow>(
            TitleDataList.List.Select((t, i) => new TitleRow(i, t)));
        FirstClearTitles = new ObservableCollection<TitleElement>(TitleDataList.List);
        SelectedTitle = Titles.FirstOrDefault();
    }

    public ObservableCollection<TitleRow> Titles { get; }

    public ObservableCollection<TitleElement> FirstClearTitles { get; }

    public string TitleCount
    {
        get => _titleCount;
        private set => Set(ref _titleCount, value);
    }

    public TitleRow? SelectedTitle { get; set; }

    public string PlayHour
    {
        get => _playHour;
        set
        {
            if (!Set(ref _playHour, value) || IsRefreshing)
                return;
            if (!ValidateParse<ushort>(nameof(PlayHour), "First-clear play time (hours)", value, ushort.TryParse,
                    "a whole number between 0 and 65 535", out var v))
                return;
            if (EngineGuard.Ready)
                Data.FirstClearData.PlayTimeHour.Value = v;
        }
    }

    public string PlayMinute
    {
        get => _playMinute;
        set
        {
            if (!Set(ref _playMinute, value) || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(PlayMinute), "First-clear play time (minutes)", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                Data.FirstClearData.PlayTimeMinute.Value = v;
        }
    }

    public string PlaySecond
    {
        get => _playSecond;
        set
        {
            if (!Set(ref _playSecond, value) || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(PlaySecond), "First-clear play time (seconds)", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                Data.FirstClearData.PlayTimeSecond.Value = v;
        }
    }

    public string MultiHour
    {
        get => _multiHour;
        set
        {
            if (!Set(ref _multiHour, value) || IsRefreshing)
                return;
            if (!ValidateParse<ushort>(nameof(MultiHour), "First-clear multiplayer (hours)", value, ushort.TryParse,
                    "a whole number between 0 and 65 535", out var v))
                return;
            if (EngineGuard.Ready)
                Data.FirstClearData.MultiPlayTimeHour.Value = v;
        }
    }

    public string MultiMinute
    {
        get => _multiMinute;
        set
        {
            if (!Set(ref _multiMinute, value) || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(MultiMinute), "First-clear multiplayer (minutes)", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                Data.FirstClearData.MultiPlayTimeMinute.Value = v;
        }
    }

    public string MultiSecond
    {
        get => _multiSecond;
        set
        {
            if (!Set(ref _multiSecond, value) || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(MultiSecond), "First-clear multiplayer (seconds)", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                Data.FirstClearData.MultiPlayTimeSecond.Value = v;
        }
    }

    public string VictoryCount
    {
        get => _victoryCount;
        set
        {
            if (!Set(ref _victoryCount, value) || IsRefreshing)
                return;
            if (!ValidateParse<ushort>(nameof(VictoryCount), "First-clear victories", value, ushort.TryParse,
                    "a whole number between 0 and 65 535", out var v))
                return;
            if (EngineGuard.Ready)
                Data.FirstClearData.VictoryCount.Value = v;
        }
    }

    public string AlchemyCount
    {
        get => _alchemyCount;
        set
        {
            if (!Set(ref _alchemyCount, value) || IsRefreshing)
                return;
            if (!ValidateParse<ushort>(nameof(AlchemyCount), "First-clear alchemy count", value, ushort.TryParse,
                    "a whole number between 0 and 65 535", out var v))
                return;
            if (EngineGuard.Ready)
                Data.FirstClearData.AlchemyCount.Value = v;
        }
    }

    public string QuestCount
    {
        get => _questCount;
        set
        {
            if (!Set(ref _questCount, value) || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(QuestCount), "First-clear quests", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                Data.FirstClearData.QuestCount.Value = v;
        }
    }

    public string VisitorCount
    {
        get => _visitorCount;
        set
        {
            if (!Set(ref _visitorCount, value) || IsRefreshing)
                return;
            if (!ValidateParse<ushort>(nameof(VisitorCount), "First-clear visitors", value, ushort.TryParse,
                    "a whole number between 0 and 65 535", out var v))
                return;
            if (EngineGuard.Ready)
                Data.FirstClearData.VisitorCount = v;
        }
    }

    public string TreasureMapCount
    {
        get => _treasureMapCount;
        set
        {
            if (!Set(ref _treasureMapCount, value) || IsRefreshing)
                return;
            if (!ValidateParse<ushort>(nameof(TreasureMapCount), "First-clear treasure maps", value, ushort.TryParse,
                    "a whole number between 0 and 65 535", out var v))
                return;
            if (EngineGuard.Ready)
                Data.FirstClearData.TreasureMap = v;
        }
    }

    public TitleElement? SelectedFirstClearTitle
    {
        get => _selectedFirstClearTitle;
        set
        {
            if (!Set(ref _selectedFirstClearTitle, value))
                return;
            if (value != null && !IsRefreshing && EngineGuard.Ready)
                Data.FirstClearData.Title = value;
        }
    }

    public void ReviseTitleCount()
    {
        Data.TitleData.ReviseTitleCount();
        TitleCount = Data.TitleData.TitleCount.ToString();
    }

    /// <inheritdoc />
    public override string Header => "Titles";

    protected override void OnRefresh()
    {
        foreach (var title in Titles)
            title.Reload();
        TitleCount = Data.TitleData.TitleCount.ToString();
        var firstClear = Data.FirstClearData;
        PlayHour = firstClear.PlayTimeHour.Value.ToString();
        PlayMinute = firstClear.PlayTimeMinute.Value.ToString();
        PlaySecond = firstClear.PlayTimeSecond.Value.ToString();
        MultiHour = firstClear.MultiPlayTimeHour.Value.ToString();
        MultiMinute = firstClear.MultiPlayTimeMinute.Value.ToString();
        MultiSecond = firstClear.MultiPlayTimeSecond.Value.ToString();
        VictoryCount = firstClear.VictoryCount.Value.ToString();
        AlchemyCount = firstClear.AlchemyCount.Value.ToString();
        QuestCount = firstClear.QuestCount.Value.ToString();
        VisitorCount = firstClear.VisitorCount.ToString();
        TreasureMapCount = firstClear.TreasureMap.ToString();
        var held = firstClear.Title;
        SelectedFirstClearTitle = held != null
            ? FirstClearTitles.FirstOrDefault(t => t.DataIndex == held.DataIndex) ?? FirstClearTitles.FirstOrDefault()
            : FirstClearTitles.FirstOrDefault();
    }
}

/// <summary>One title in the list with its held flag.</summary>
public sealed class TitleRow : ObservableObject
{
    private string _display;

    public TitleRow(int index, TitleElement element)
    {
        Index = index;
        Element = element;
        _display = BuildDisplay(index, element);
    }

    public int Index { get; }

    public TitleElement Element { get; }

    public string Display
    {
        get => _display;
        private set => Set(ref _display, value);
    }

    public bool IsHold
    {
        get => SaveDataManager.Instance.SaveData.TitleData.IsTitleHold(Index);
        set
        {
            SaveDataManager.Instance.SaveData.TitleData.SetTitleHold(Index, value);
            Display = BuildDisplay(Index, Element);
        }
    }

    public void Reload() => Display = BuildDisplay(Index, Element);

    private static string BuildDisplay(int index, TitleElement element)
    {
        if (!EngineGuard.Ready)
            return $"{element.MaleTitleName} / {element.LadyTitleName}";
        var hold = SaveDataManager.Instance.SaveData.TitleData.IsTitleHold(index);
        return $"{element.MaleTitleName} / {element.LadyTitleName}{(hold ? " [held]" : "")}";
    }
}
