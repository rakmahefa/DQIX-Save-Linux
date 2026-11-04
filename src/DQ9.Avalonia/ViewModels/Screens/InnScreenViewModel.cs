using System;
using DQ9_Cheat.DataManager;
using System.Collections.ObjectModel;
using System.Linq;
using DQ9_Cheat.GameData;

namespace DQ9.Avalonia.ViewModels.Screens;

/// <summary>
/// Rikka's inn and staying guests (WinForms RikkaInnDataControl and
/// StayingListGroupBox parity): grade, guest list with basic profile fields,
/// reorder / delete / create commands.
/// </summary>
public sealed class InnScreenViewModel : EditorScreenViewModel
{
    private VisitorRow? _selectedVisitor;
    private string _grade = "0";
    private string _uid = "0";
    private string _visitorCount = "0";
    private string _name = "";
    private string _level = "0";
    private JobData? _selectedJob;
    private string _place = "0";
    private bool _haveTreasureMap;
    private string _lodgingYear = "0";
    private string _lodgingMonth = "0";
    private string _lodgingDay = "0";
    private string _monsterCompRate = "0";
    private string _itemCompRate = "0";
    private string _fashionCompRate = "0";
    private string _alchemyCompRate = "0";

    public InnScreenViewModel()
    {
        Visitors = new ObservableCollection<VisitorRow>(
            Enumerable.Range(0, VisitorManager.VisitorMax).Select(i => new VisitorRow(i)));
        Jobs = new ObservableCollection<JobData>(JobDataList.List);
        SelectedVisitor = Visitors.FirstOrDefault();
    }

    public ObservableCollection<VisitorRow> Visitors { get; }

    public ObservableCollection<JobData> Jobs { get; }

    public string Grade
    {
        get => _grade;
        set
        {
            if (Set(ref _grade, value) && !IsRefreshing && EngineGuard.Ready && byte.TryParse(value, out var v))
                Data.RikkaData.Grade = Math.Min(v, (byte)6);
        }
    }

    public string Uid
    {
        get => _uid;
        private set => Set(ref _uid, value);
    }

    public string VisitorCount
    {
        get => _visitorCount;
        private set => Set(ref _visitorCount, value);
    }

    public VisitorRow? SelectedVisitor
    {
        get => _selectedVisitor;
        set
        {
            if (!Set(ref _selectedVisitor, value))
                return;
            LoadDetail();
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (Set(ref _name, value) && SelectedVisitor != null && !IsRefreshing && EngineGuard.Ready
                && value.Length <= SelectedVisitor.Data.Name.MaxLength)
                SelectedVisitor.Data.Name.Value = value;
        }
    }

    public string Level
    {
        get => _level;
        set
        {
            if (!Set(ref _level, value) || SelectedVisitor == null || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(Level), "Level", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                SelectedVisitor.Data.Level = v;
        }
    }

    public JobData? SelectedJob
    {
        get => _selectedJob;
        set
        {
            if (!Set(ref _selectedJob, value))
                return;
            if (SelectedVisitor != null && value != null && !IsRefreshing && EngineGuard.Ready && value.DataIndex < byte.MaxValue)
                SelectedVisitor.Data.Job = (byte)value.DataIndex;
        }
    }

    public string Place
    {
        get => _place;
        set
        {
            if (!Set(ref _place, value) || SelectedVisitor == null || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(Place), "Place", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                SelectedVisitor.Data.Place = v;
        }
    }

    public bool HaveTreasureMap
    {
        get => _haveTreasureMap;
        set
        {
            if (Set(ref _haveTreasureMap, value) && SelectedVisitor != null && !IsRefreshing && EngineGuard.Ready)
                SelectedVisitor.Data.HaveTreasureMap = value;
        }
    }

    public string LodgingYear
    {
        get => _lodgingYear;
        set
        {
            if (!Set(ref _lodgingYear, value) || SelectedVisitor == null || IsRefreshing)
                return;
            if (!ValidateParse<ushort>(nameof(LodgingYear), "Lodging year", value, ushort.TryParse,
                    "a year between 2000 and 2127", out var v))
                return;
            if (EngineGuard.Ready)
                SelectedVisitor.Data.LodgingDay_Year = v;
        }
    }

    public string LodgingMonth
    {
        get => _lodgingMonth;
        set
        {
            if (!Set(ref _lodgingMonth, value) || SelectedVisitor == null || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(LodgingMonth), "Lodging month", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                SelectedVisitor.Data.LodgingDay_Month = v;
        }
    }

    public string LodgingDay
    {
        get => _lodgingDay;
        set
        {
            if (!Set(ref _lodgingDay, value) || SelectedVisitor == null || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(LodgingDay), "Lodging day", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                SelectedVisitor.Data.LodgingDay_Day = v;
        }
    }

    /// <summary>Completion-rate editors of the WinForms StayingListGroupBox
    /// "Completion Rate" group (monster / item / fashion / alchemy, 0-127).</summary>
    public string MonsterCompRate
    {
        get => _monsterCompRate;
        set
        {
            if (!Set(ref _monsterCompRate, value) || SelectedVisitor == null || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(MonsterCompRate), "Monster completion rate", value, byte.TryParse,
                    "a whole number between 0 and 127", out var v))
                return;
            if (EngineGuard.Ready)
                SelectedVisitor.Data.MonsterCompRate = Math.Min(v, (byte)127);
        }
    }

    public string ItemCompRate
    {
        get => _itemCompRate;
        set
        {
            if (!Set(ref _itemCompRate, value) || SelectedVisitor == null || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(ItemCompRate), "Item completion rate", value, byte.TryParse,
                    "a whole number between 0 and 127", out var v))
                return;
            if (EngineGuard.Ready)
                SelectedVisitor.Data.ItemCompRate = Math.Min(v, (byte)127);
        }
    }

    public string FashionCompRate
    {
        get => _fashionCompRate;
        set
        {
            if (!Set(ref _fashionCompRate, value) || SelectedVisitor == null || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(FashionCompRate), "Fashion completion rate", value, byte.TryParse,
                    "a whole number between 0 and 127", out var v))
                return;
            if (EngineGuard.Ready)
                SelectedVisitor.Data.FashionCompRate = Math.Min(v, (byte)127);
        }
    }

    public string AlchemyCompRate
    {
        get => _alchemyCompRate;
        set
        {
            if (!Set(ref _alchemyCompRate, value) || SelectedVisitor == null || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(AlchemyCompRate), "Alchemy completion rate", value, byte.TryParse,
                    "a whole number between 0 and 127", out var v))
                return;
            if (EngineGuard.Ready)
                SelectedVisitor.Data.AlchemyCompRate = Math.Min(v, (byte)127);
        }
    }

    public void MoveVisitorUp()
    {
        if (SelectedVisitor == null || SelectedVisitor.Index == 0)
            return;
        Data.RikkaData.VisitorManager.MoveTo(SelectedVisitor.Index, SelectedVisitor.Index - 1);
        RefreshVisitorList(SelectedVisitor.Index - 1);
    }

    public void MoveVisitorDown()
    {
        if (SelectedVisitor == null || SelectedVisitor.Index >= VisitorManager.VisitorMax - 1)
            return;
        Data.RikkaData.VisitorManager.MoveTo(SelectedVisitor.Index, SelectedVisitor.Index + 1);
        RefreshVisitorList(SelectedVisitor.Index + 1);
    }

    public void DeleteVisitor()
    {
        if (SelectedVisitor == null)
            return;
        Data.RikkaData.VisitorManager.DeleteVisitor(SelectedVisitor.Index);
        RefreshVisitorList(0);
    }

    public void CreateVisitor()
    {
        Data.RikkaData.VisitorManager.CreateCharacter();
        RefreshVisitorList(0);
    }

    private void RefreshVisitorList(int selectIndex)
    {
        foreach (var visitor in Visitors)
            visitor.Reload();
        RefreshHeader();
        SelectedVisitor = Visitors[Math.Min(selectIndex, Visitors.Count - 1)];
    }

    private void RefreshHeader()
    {
        Grade = Data.RikkaData.Grade.ToString();
        Uid = Data.RikkaData.UID.ToString();
        VisitorCount = Data.RikkaData.VisitorManager.VisitorCount.Value.ToString();
    }

    private void LoadDetail()
    {
        SuppressWrites(() =>
        {
            if (SelectedVisitor == null || !EngineGuard.Ready)
                return;
            var visitor = SelectedVisitor.Data;
            Name = visitor.Name.Value;
            Level = visitor.Level.ToString();
            Place = visitor.Place.ToString();
            HaveTreasureMap = visitor.HaveTreasureMap;
            LodgingYear = visitor.LodgingDay_Year.ToString();
            LodgingMonth = visitor.LodgingDay_Month.ToString();
            LodgingDay = visitor.LodgingDay_Day.ToString();
            MonsterCompRate = visitor.MonsterCompRate.ToString();
            ItemCompRate = visitor.ItemCompRate.ToString();
            FashionCompRate = visitor.FashionCompRate.ToString();
            AlchemyCompRate = visitor.AlchemyCompRate.ToString();
            _selectedJob = visitor.Job < Jobs.Count ? Jobs[visitor.Job] : Jobs.FirstOrDefault();
            Raise(nameof(SelectedJob));
        });
    }

    /// <inheritdoc />
    public override string Header => "Rikka's Inn";

    protected override void OnRefresh()
    {
        foreach (var visitor in Visitors)
            visitor.Reload();
        RefreshHeader();
        LoadDetail();
    }
}

/// <summary>One guest slot in the inn list.</summary>
public sealed class VisitorRow : ObservableObject
{
    private string _display;

    public VisitorRow(int index)
    {
        Index = index;
        _display = BuildDisplay(index);
    }

    public int Index { get; }

    internal DQ9_Cheat.DataManager.VisitorData Data =>
        SaveDataManager.Instance.SaveData.RikkaData.VisitorManager.VisitorData[Index];

    public string Display
    {
        get => _display;
        private set => Set(ref _display, value);
    }

    public void Reload() => Display = BuildDisplay(Index);

    private static string BuildDisplay(int index)
    {
        if (!EngineGuard.Ready)
            return $"#{index + 1}: (no file)";
        var visitor = SaveDataManager.Instance.SaveData.RikkaData.VisitorManager.VisitorData[index];
        var name = visitor.Name.Value;
        if (string.IsNullOrEmpty(name))
            return $"#{index + 1}: (empty)";
        var job = visitor.Job < JobDataList.List.Count ? JobDataList.List[visitor.Job].Name : "?";
        return $"#{index + 1}: {name} - Lv{visitor.Level} {job}";
    }
}
