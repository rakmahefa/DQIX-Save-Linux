using DQ9_Cheat.GameData;
using System.Collections.ObjectModel;
using System.Linq;
using DQ9_Cheat.DataManager;

namespace DQ9.Avalonia.ViewModels.Screens;

/// <summary>Quest list editor (WinForms QuestDataControl / QuestPanel parity).</summary>
public sealed class QuestsScreenViewModel : EditorScreenViewModel
{
    private QuestRow? _selectedQuest;
    private QuestState _selectedState;
    private string _year = "0";
    private string _month = "0";
    private string _day = "0";
    private string _hour = "0";
    private string _minute = "0";
    private string _clearCount = "0";
    private string _orderCount = "0";

    public QuestsScreenViewModel()
    {
        Quests = new ObservableCollection<QuestRow>(
            QuestDataList.List.Select((q, i) => new QuestRow(i, q)));
        // UX-14: display rows keep the engine enum out of the UI.
        States = new ObservableCollection<QuestState>
        {
            QuestState.NotRecieved,
            QuestState.AllowOrder,
            QuestState.Order,
            QuestState.Clear,
        };
        StateOptions = new ObservableCollection<EnumOption<QuestState>>(States
            .Select(s => new EnumOption<QuestState>(s, s.ToDisplay())));
        SelectedQuest = Quests.FirstOrDefault();
    }

    public ObservableCollection<QuestRow> Quests { get; }

    /// <summary>Directly settable quest states (BeforeOrder is derived by the game).</summary>
    public ObservableCollection<QuestState> States { get; }

    /// <summary>UX-14: the combo binds these labelled rows, not the raw enum.</summary>
    public ObservableCollection<EnumOption<QuestState>> StateOptions { get; }

    private EnumOption<QuestState>? _selectedStateOption;

    public EnumOption<QuestState>? SelectedStateOption
    {
        get => _selectedStateOption;
        set
        {
            if (!Set(ref _selectedStateOption, value) || value == null)
                return;
            SelectedState = value.Value;
        }
    }

    public QuestRow? SelectedQuest
    {
        get => _selectedQuest;
        set
        {
            if (!Set(ref _selectedQuest, value))
                return;
            LoadDetail();
        }
    }

    public QuestState SelectedState
    {
        get => _selectedState;
        set
        {
            if (!Set(ref _selectedState, value))
                return;
            if (SelectedQuest == null || IsRefreshing)
                return;
            var index = SelectedQuest.Index;
            switch (value)
            {
                case QuestState.NotRecieved:
                    Data.QuestDataManager.SetStateNone(index);
                    break;
                case QuestState.AllowOrder:
                    Data.QuestDataManager.SetStateAllowOrder(index);
                    break;
                case QuestState.Order:
                    Data.QuestDataManager.SetStateOrder(index);
                    break;
                case QuestState.Clear:
                    Data.QuestDataManager.SetStateClear(index);
                    break;
            }
            RefreshHeader();
            SelectedQuest.Reload();
        }
    }

    public string Year
    {
        get => _year;
        set
        {
            if (!Set(ref _year, value) || SelectedQuest == null || IsRefreshing)
                return;
            if (!ValidateParse<ushort>(nameof(Year), "Quest year", value, ushort.TryParse,
                    "a whole number between 0 and 65 535", out var v))
                return;
            if (EngineGuard.Ready)
                Data.QuestDataManager.SetYear(SelectedQuest.Index, v);
        }
    }

    public string Month
    {
        get => _month;
        set
        {
            if (!Set(ref _month, value) || SelectedQuest == null || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(Month), "Quest month", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                Data.QuestDataManager.SetMonth(SelectedQuest.Index, v);
        }
    }

    public string Day
    {
        get => _day;
        set
        {
            if (!Set(ref _day, value) || SelectedQuest == null || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(Day), "Quest day", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                Data.QuestDataManager.SetDay(SelectedQuest.Index, v);
        }
    }

    public string Hour
    {
        get => _hour;
        set
        {
            if (!Set(ref _hour, value) || SelectedQuest == null || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(Hour), "Quest hour", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                Data.QuestDataManager.SetHour(SelectedQuest.Index, v);
        }
    }

    public string Minute
    {
        get => _minute;
        set
        {
            if (!Set(ref _minute, value) || SelectedQuest == null || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(Minute), "Quest minute", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                Data.QuestDataManager.SetMinute(SelectedQuest.Index, v);
        }
    }

    public string ClearCount
    {
        get => _clearCount;
        private set => Set(ref _clearCount, value);
    }

    public string OrderCount
    {
        get => _orderCount;
        private set => Set(ref _orderCount, value);
    }

    public void ReviseClearCount()
    {
        Data.QuestDataManager.ReviseClearCount();
        RefreshHeader();
    }

    public void ReviseOrderCount()
    {
        Data.QuestDataManager.ReviseOrderCount();
        RefreshHeader();
    }

    private void LoadDetail()
    {
        SuppressWrites(() =>
        {
            if (SelectedQuest == null || !EngineGuard.Ready)
                return;
            var index = SelectedQuest.Index;
            _selectedState = Data.QuestDataManager.GetState(index);
            _selectedStateOption = StateOptions.FirstOrDefault(o => o.Value == _selectedState);
            Year = Data.QuestDataManager.GetYear(index).ToString();
            Month = Data.QuestDataManager.GetMonth(index).ToString();
            Day = Data.QuestDataManager.GetDay(index).ToString();
            Hour = Data.QuestDataManager.GetHour(index).ToString();
            Minute = Data.QuestDataManager.GetMinute(index).ToString();
            Raise(nameof(SelectedState));
            Raise(nameof(SelectedStateOption));
            RefreshHeader();
        });
    }

    private void RefreshHeader()
    {
        ClearCount = Data.QuestDataManager.ClearCount.Value.ToString();
        OrderCount = Data.QuestDataManager.OrderCount.ToString();
    }

    /// <inheritdoc />
    public override string Header => "Quests";

    protected override void OnRefresh()
    {
        foreach (var quest in Quests)
            quest.Reload();
        LoadDetail();
    }
}

/// <summary>One quest in the list.</summary>
public sealed class QuestRow : ObservableObject
{
    private string _display;

    public QuestRow(int index, DQ9_Cheat.GameData.QuestElement element)
    {
        Index = index;
        Element = element;
        _display = BuildDisplay(index, element);
    }

    public int Index { get; }

    public DQ9_Cheat.GameData.QuestElement Element { get; }

    public string Display
    {
        get => _display;
        private set => Set(ref _display, value);
    }

    public void Reload() => Display = BuildDisplay(Index, Element);

    private static string BuildDisplay(int index, DQ9_Cheat.GameData.QuestElement element)
    {
        if (!EngineGuard.Ready)
            return $"{element.QuestNo}. {element.QuestTitle}";
        var state = SaveDataManager.Instance.SaveData.QuestDataManager.GetState(index);
        return $"{element.QuestNo}. {element.QuestTitle}"
               + (element.AdditionalQuest ? " [+]" : "")
               + $" - {state.ToDisplay()}";
    }
}
