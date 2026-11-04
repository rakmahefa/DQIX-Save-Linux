using System;
using System.Collections.ObjectModel;
using System.Linq;
using DQ9_Cheat.DataManager;
using DQ9_Cheat.GameData;

namespace DQ9.Avalonia.ViewModels.Screens;

/// <summary>
/// Treasure map editor (WinForms TreasureMapDataControl parity): the 99 map
/// slots, map state / rank / type, grotto details and box contents.
/// </summary>
public sealed class TreasureMapsScreenViewModel : EditorScreenViewModel
{
    private TreasureMapRow? _selectedMap;
    private string _mapCount = "0";
    private MapState _selectedState;
    private string _rank = "0";
    private MapType _selectedType;
    private string _devilLevel = "0";
    private string _detector = "";
    private string _renewer = "";
    private string _details = "";
    private bool _detailsIsWarning;
    private string _boxes = "";

    public TreasureMapsScreenViewModel()
    {
        Maps = new ObservableCollection<TreasureMapRow>(
            Enumerable.Range(0, TreasureMapManager.MapCountMax).Select(i => new TreasureMapRow(i)));
        States = new ObservableCollection<MapState>(
            [MapState.NotDiscover, MapState.Discover, MapState.Clear]);
        Types = new ObservableCollection<MapType>([MapType.Normal, MapType.devil]);
        // UX-14: labelled rows keep the engine enums (NotDiscover, devil) out of the UI.
        StateOptions = new ObservableCollection<EnumOption<MapState>>(States
            .Select(s => new EnumOption<MapState>(s, s.ToDisplay())));
        TypeOptions = new ObservableCollection<EnumOption<MapType>>(Types
            .Select(t => new EnumOption<MapType>(t, t.ToDisplay())));
        SelectedMap = Maps.FirstOrDefault();
    }

    public ObservableCollection<TreasureMapRow> Maps { get; }

    public ObservableCollection<MapState> States { get; }

    public ObservableCollection<MapType> Types { get; }

    /// <summary>UX-14: the combos bind these labelled rows, not the raw enums.</summary>
    public ObservableCollection<EnumOption<MapState>> StateOptions { get; }

    public ObservableCollection<EnumOption<MapType>> TypeOptions { get; }

    private EnumOption<MapState>? _selectedStateOption;

    public EnumOption<MapState>? SelectedStateOption
    {
        get => _selectedStateOption;
        set
        {
            if (!Set(ref _selectedStateOption, value) || value == null)
                return;
            SelectedState = value.Value;
        }
    }

    private EnumOption<MapType>? _selectedTypeOption;

    public EnumOption<MapType>? SelectedTypeOption
    {
        get => _selectedTypeOption;
        set
        {
            if (!Set(ref _selectedTypeOption, value) || value == null)
                return;
            SelectedType = value.Value;
        }
    }

    public string MapCountText
    {
        get => _mapCount;
        private set => Set(ref _mapCount, value);
    }

    public TreasureMapRow? SelectedMap
    {
        get => _selectedMap;
        set
        {
            if (!Set(ref _selectedMap, value))
                return;
            LoadDetail();
        }
    }

    public MapState SelectedState
    {
        get => _selectedState;
        set
        {
            if (!Set(ref _selectedState, value))
                return;
            if (SelectedMap?.Data != null && !IsRefreshing && EngineGuard.Ready)
                SelectedMap.Data.MapState = value;
        }
    }

    public string Rank
    {
        get => _rank;
        set
        {
            if (!Set(ref _rank, value) || SelectedMap?.Data == null || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(Rank), "Rank", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
            {
                SelectedMap.Data.Rank = v;
                LoadDetail();
            }
        }
    }

    public MapType SelectedType
    {
        get => _selectedType;
        set
        {
            if (!Set(ref _selectedType, value))
                return;
            if (SelectedMap?.Data != null && !IsRefreshing && EngineGuard.Ready)
                SelectedMap.Data.MapType = value;
        }
    }

    public string DevilLevel
    {
        get => _devilLevel;
        set
        {
            if (!Set(ref _devilLevel, value) || SelectedMap?.Data == null || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(DevilLevel), "Devil level", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
            {
                SelectedMap.Data.DevilLevel.Value = v;
                LoadDetail();
            }
        }
    }

    public string Detector
    {
        get => _detector;
        set
        {
            if (Set(ref _detector, value) && SelectedMap?.Data != null && !IsRefreshing && EngineGuard.Ready
                && value.Length <= SelectedMap.Data.Detector.MaxLength)
                SelectedMap.Data.Detector.Value = value;
        }
    }

    public string Renewer
    {
        get => _renewer;
        set
        {
            if (Set(ref _renewer, value) && SelectedMap?.Data != null && !IsRefreshing && EngineGuard.Ready
                && value.Length <= SelectedMap.Data.Renewer.MaxLength)
                SelectedMap.Data.Renewer.Value = value;
        }
    }

    public string Details
    {
        get => _details;
        private set => Set(ref _details, value);
    }

    /// <summary>UX-17: true while <see cref="Details"/> explains an invalid map,
    /// so the warning renders in the warning color instead of plain text.</summary>
    public bool DetailsIsWarning
    {
        get => _detailsIsWarning;
        private set => Set(ref _detailsIsWarning, value);
    }

    public string Boxes
    {
        get => _boxes;
        private set => Set(ref _boxes, value);
    }

    public void CreateNewMap()
    {
        Data.TreasureMapManager.CreateMapData();
        RefreshMaps();
    }

    public void DeleteMap()
    {
        if (SelectedMap == null)
            return;
        Data.TreasureMapManager.DeleteMapData(SelectedMap.Index);
        RefreshMaps();
    }

    public void MoveMapUp()
    {
        if (SelectedMap == null || SelectedMap.Index == 0)
            return;
        Data.TreasureMapManager.MoveTo(SelectedMap.Index, SelectedMap.Index - 1);
        RefreshMaps(SelectedMap.Index - 1);
    }

    public void MoveMapDown()
    {
        if (SelectedMap == null || SelectedMap.Index >= TreasureMapManager.MapCountMax - 1)
            return;
        Data.TreasureMapManager.MoveTo(SelectedMap.Index, SelectedMap.Index + 1);
        RefreshMaps(SelectedMap.Index + 1);
    }

    private void RefreshMaps(int selectIndex = -1)
    {
        foreach (var map in Maps)
            map.Reload();
        MapCountText = Data.TreasureMapManager.MapCount.ToString();
        SelectedMap = Maps[Math.Min(selectIndex < 0 ? SelectedMap?.Index ?? 0 : selectIndex, Maps.Count - 1)];
    }

    private void LoadDetail()
    {
        SuppressWrites(() =>
        {
            if (SelectedMap == null || !EngineGuard.Ready)
                return;
            var map = SelectedMap.Data;
            if (map == null)
            {
                Details = "(empty slot)";
                DetailsIsWarning = false;
                Boxes = string.Empty;
                return;
            }
            map.CalculateDetail();
            _selectedState = map.MapState;
            _rank = map.Rank.ToString();
            _selectedType = map.MapType;
            _devilLevel = map.DevilLevel.Value.ToString();
            _detector = map.Detector.Value;
            _renewer = map.Renewer.Value;
            _selectedStateOption = StateOptions.FirstOrDefault(o => o.Value == _selectedState);
            _selectedTypeOption = TypeOptions.FirstOrDefault(o => o.Value == _selectedType);
            Raise(nameof(SelectedState));
            Raise(nameof(Rank));
            Raise(nameof(SelectedType));
            Raise(nameof(DevilLevel));
            Raise(nameof(Detector));
            Raise(nameof(Renewer));
            Raise(nameof(SelectedStateOption));
            Raise(nameof(SelectedTypeOption));
            if (!map.IsValid)
            {
                // UX-17: name the controls that actually exist on this screen.
                Details = "This map's data is invalid — try adjusting the Rank or Devil level.";
                DetailsIsWarning = true;
                Boxes = "No treasure box data while the map is invalid.";
                return;
            }
            DetailsIsWarning = false;
            Details = $"Boss: {map.BossName}   Lv: {map.MapLevel}   Floors: {map.FloorCount}   "
                      + $"Monster rank: {map.MonsterRank}   Seed: {map.Seed:X4}";
            var lines = new System.Text.StringBuilder();
            foreach (var group in map.TreasureBoxInfoList)
            {
                if (group.Count == 0)
                    continue;
                lines.AppendLine($"Rank {group[0].Rank + 1}: {group.Count} box(es)");
                foreach (var box in group.Take(6))
                    lines.AppendLine($"  x={box.X} y={box.Y}");
                if (group.Count > 6)
                    lines.AppendLine("  ...");
            }
            Boxes = lines.Length == 0 ? "(no treasure boxes)" : lines.ToString().TrimEnd();
        });
    }

    /// <inheritdoc />
    public override string Header => "Treasure maps";

    protected override void OnRefresh()
    {
        foreach (var map in Maps)
            map.Reload();
        MapCountText = EngineGuard.Ready ? Data.TreasureMapManager.MapCount.ToString() : "0";
        LoadDetail();
    }
}

/// <summary>One treasure map slot in the list.</summary>
public sealed class TreasureMapRow : ObservableObject
{
    private string _display;

    public TreasureMapRow(int index)
    {
        Index = index;
        _display = BuildDisplay(index);
    }

    public int Index { get; }

    internal DQ9_Cheat.DataManager.TreasureMapData? Data =>
        SaveDataManager.Instance.SaveData.TreasureMapManager.GetMapData(Index);

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
        var manager = SaveDataManager.Instance.SaveData.TreasureMapManager;
        if (index >= manager.MapCount)
            return $"#{index + 1}: (empty)";
        var map = manager.GetMapData(index);
        if (map == null)
            return $"#{index + 1}: (empty)";
        return $"#{index + 1}: {map.MapName}{(map.IsValid ? "" : " [invalid]")}";
    }
}
