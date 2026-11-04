using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DQ9_Cheat.GameData;
using DQ9_Cheat.MapSearch;

namespace DQ9.Avalonia.ViewModels.Screens;

/// <summary>
/// Treasure map search (WinForms MapSearch windows parity): filter conditions,
/// background cache generation with progress and cancellation, results list
/// with sorting and map-detail readout backed by the seed.
/// </summary>
public sealed class SearchScreenViewModel : EditorScreenViewModel
{
    private readonly Func<SearchDataFileService> _serviceFactory;
    private SearchDataFileService? _service;
    private CancellationTokenSource? _generationCts;
    private string _status = "Cache not loaded.";
    private int _progressPercent;
    private bool _isGenerating;
    private bool _isBusy;
    private string _cachePath = "";
    private string _resultCount = "";
    private SearchResultRow? _selectedResult;
    private string _resultDetails = "";
    private SortMode _selectedSortMode = SortMode.LevelDescending;

    public SearchScreenViewModel()
        : this(() => SearchDataFileService.CreateDefault())
    {
    }

    public SearchScreenViewModel(Func<SearchDataFileService> serviceFactory)
    {
        _serviceFactory = serviceFactory;
        Ranks = new ObservableCollection<RankOption>(
            SearchDataFileService.RankList.Select((rank, i) => new RankOption(i, rank)));
        Name1Options = new ObservableCollection<NameOption>(
            TreasureMapDataTable.TreasureMapName1_Table.Select((n, i) => new NameOption(i, n)));
        Name2Options = new ObservableCollection<NameOption>(
            TreasureMapDataTable.TreasureMapName2_Table.Select((n, i) => new NameOption(i, n)));
        Name3Options = new ObservableCollection<NameOption>(
            TreasureMapDataTable.TreasureMapName3_Table.Select((n, i) => new NameOption(i, n)));
        BossOptions = new ObservableCollection<NameOption>(
            Enumerable.Range(0, 25).Select(i => new NameOption(i, MonsterDataList.List[282 + i])));
        MapTypes = new ObservableCollection<MapTypeOption>(
            TreasureMapDataTable.TreasureMapMapTypeName_Table.Select((n, i) => new MapTypeOption(i + 1, n)));
        // UX-14: labelled rows for the sort modes and comparison operators.
        SortOptions = new ObservableCollection<EnumOption<SortMode>>
        {
            new(SortMode.LevelDescending, "Level (highest first)"),
            new(SortMode.LevelAscending, "Level (lowest first)"),
            new(SortMode.NameAscending, "Name (A–Z)"),
        };
        _selectedSortOption = SortOptions[0];
        ConditionTypeOptions = new ObservableCollection<EnumOption<SearchConditionType>>
        {
            new(SearchConditionType.Accord, SearchConditionType.Accord.ToDisplay()),
            new(SearchConditionType.Discord, SearchConditionType.Discord.ToDisplay()),
            new(SearchConditionType.AndOver, SearchConditionType.AndOver.ToDisplay()),
            new(SearchConditionType.AndLess, SearchConditionType.AndLess.ToDisplay()),
        };
        Results = new ObservableCollection<SearchResultRow>();

        SelectedConditionTypeOption = ConditionTypeOptions
            .First(o => o.Value == SearchConditionType.AndOver);
        RankIndex = 0;
        Name1Index = 0;
        Name2Index = 0;
        Name3Index = 0;
        BossIndex = 0;
        MapTypeValue = 1;
        MapLevel = 1;
        MonsterRank = 1;
        FloorCount = 1;
        BoxType = 0;
        BoxCount = 1;
    }

    public enum SortMode
    {
        LevelDescending,
        LevelAscending,
        NameAscending,
    }

    public ObservableCollection<RankOption> Ranks { get; }
    public ObservableCollection<NameOption> Name1Options { get; }
    public ObservableCollection<NameOption> Name2Options { get; }
    public ObservableCollection<NameOption> Name3Options { get; }
    public ObservableCollection<NameOption> BossOptions { get; }
    public ObservableCollection<MapTypeOption> MapTypes { get; }
    public ObservableCollection<EnumOption<SortMode>> SortOptions { get; }
    public ObservableCollection<EnumOption<SearchConditionType>> ConditionTypeOptions { get; }
    public ObservableCollection<SearchResultRow> Results { get; }

    /// <summary>UI-disabled flag while a generation or search is running.</summary>
    public bool IsNotBusy => !_isBusy;

    /// <summary>UX-18: the progress bar is only visible while work runs.</summary>
    public bool IsBusy => _isBusy;

    /// <summary>UX-18: file name only in the UI; the full path lives in the tooltip.</summary>
    public string CacheFileName => string.IsNullOrEmpty(_cachePath) ? string.Empty : System.IO.Path.GetFileName(_cachePath);

    /// <summary>UX-18: the results pane shows an empty-state note while empty.</summary>
    public bool HasResults => Results.Count > 0;

    public string CachePath
    {
        get => _cachePath;
        private set => Set(ref _cachePath, value);
    }

    public string Status
    {
        get => _status;
        private set => Set(ref _status, value);
    }

    public int ProgressPercent
    {
        get => _progressPercent;
        private set => Set(ref _progressPercent, value);
    }

    public bool IsGenerating
    {
        get => _isGenerating;
        private set
        {
            if (Set(ref _isGenerating, value))
                Raise(nameof(CanCancel));
        }
    }

    public bool CanCancel => IsGenerating;

    public string ResultCount
    {
        get => _resultCount;
        private set => Set(ref _resultCount, value);
    }

    public SearchResultRow? SelectedResult
    {
        get => _selectedResult;
        set
        {
            if (!Set(ref _selectedResult, value))
                return;
            if (value == null || _service == null)
            {
                _resultDetails = string.Empty;
            }
            else
            {
                var details = new DQ9_Cheat.GameData.TreasureMapDetailData();
                details.MapRank = value.Rank;
                details.MapSeed = value.Seed;
                details.CalculateDetail(false);
                _resultDetails = details.IsValidSeed && details.IsValidRank
                    ? $"Boss: {details.BossName}   Floors: {details.FloorCount}   "
                      + $"Monster rank: {details.MonsterRank}   Type: {details.MapTypeName}"
                    : "(detail unavailable for this seed/rank)";
            }
            Raise(nameof(ResultDetails));
        }
    }

    public string ResultDetails
    {
        get => _resultDetails;
        private set => Set(ref _resultDetails, value);
    }

    public SortMode SelectedSortMode
    {
        get => _selectedSortMode;
        set
        {
            if (Set(ref _selectedSortMode, value))
            {
                _selectedSortOption = SortOptions.FirstOrDefault(o => o.Value == value);
                Raise(nameof(SelectedSortOption));
            }
        }
    }

    private EnumOption<SortMode>? _selectedSortOption;

    public EnumOption<SortMode>? SelectedSortOption
    {
        get => _selectedSortOption;
        set
        {
            if (!Set(ref _selectedSortOption, value) || value == null)
                return;
            SelectedSortMode = value.Value;
        }
    }

    private EnumOption<SearchConditionType>? _selectedConditionTypeOption;

    public EnumOption<SearchConditionType>? SelectedConditionTypeOption
    {
        get => _selectedConditionTypeOption;
        set
        {
            if (!Set(ref _selectedConditionTypeOption, value) || value == null)
                return;
            ConditionType = value.Value;
        }
    }

    // ---- condition inputs (mirroring the WinForms search windows);
    //      UX-18: INPC properties so inactive filters can render disabled ----

    public SearchConditionType ConditionType { get; set; }

    public int RankIndex { get; set; }

    public int Name1Index { get; set; }

    public int Name2Index { get; set; }

    public int Name3Index { get; set; }

    public int BossIndex { get; set; }

    public int MapTypeValue { get; set; }

    public int MapLevel { get; set; }

    public int MonsterRank { get; set; }

    public int FloorCount { get; set; }

    public int BoxType { get; set; }

    public int BoxCount { get; set; }

    private bool _useName1;
    public bool UseName1 { get => _useName1; set => Set(ref _useName1, value); }

    private bool _useName2;
    public bool UseName2 { get => _useName2; set => Set(ref _useName2, value); }

    private bool _useName3;
    public bool UseName3 { get => _useName3; set => Set(ref _useName3, value); }

    private bool _useBoss;
    public bool UseBoss { get => _useBoss; set => Set(ref _useBoss, value); }

    private bool _useMapType;
    public bool UseMapType { get => _useMapType; set => Set(ref _useMapType, value); }

    private bool _useMapLevel;
    public bool UseMapLevel { get => _useMapLevel; set => Set(ref _useMapLevel, value); }

    private bool _useMonsterRank;
    public bool UseMonsterRank { get => _useMonsterRank; set => Set(ref _useMonsterRank, value); }

    private bool _useFloorCount;
    public bool UseFloorCount { get => _useFloorCount; set => Set(ref _useFloorCount, value); }

    private bool _useBox;
    public bool UseBox { get => _useBox; set => Set(ref _useBox, value); }

    /// <summary>Builds the condition list from the current UI flags.</summary>
    public System.Collections.Generic.List<SearchConditionBase> BuildConditions()
    {
        var conditions = new System.Collections.Generic.List<SearchConditionBase>();
        if (UseName1)
            conditions.Add(new SearchMapName1Conditioin { Name1Index = Name1Index, ConditionType = ConditionType });
        if (UseName2)
            conditions.Add(new SearchMapName2Conditioin { Name2Index = Name2Index, ConditionType = ConditionType });
        if (UseName3)
            conditions.Add(new SearchMapName3Conditioin { Name3Index = Name3Index, ConditionType = ConditionType });
        if (UseBoss)
            conditions.Add(new SearchBossConditioin { BossIndex = BossIndex, ConditionType = ConditionType });
        if (UseMapType)
            conditions.Add(new SearchMapTypeConditioin { MapType = (byte)MapTypeValue, ConditionType = ConditionType });
        if (UseMapLevel)
            conditions.Add(new SearchMapLvConditioin { MapLevel = MapLevel, ConditionType = ConditionType });
        if (UseMonsterRank)
            conditions.Add(new SearchMonsterRankConditioin { MonsterRank = MonsterRank, ConditionType = ConditionType });
        if (UseFloorCount)
            conditions.Add(new SearchFloorCountConditioin { FloorCount = FloorCount, ConditionType = ConditionType });
        if (UseBox)
            conditions.Add(new SearchBoxConditioin { BoxType = BoxType, BoxCount = BoxCount, ConditionType = ConditionType });
        return conditions;
    }

    public void LoadOrPrepareService()
    {
        _service = _serviceFactory();
        CachePath = _service.CacheFilePath;
        if (_service.CacheFileExists && _service.CacheFileHasValidSize)
        {
            try
            {
                _service.LoadCache();
                Status = $"Cache loaded: {_service.TotalCount:N0} entries ({CachePath}).";
                return;
            }
            catch (InvalidDataException ex)
            {
                Status = ex.Message;
                return;
            }
        }

        Status = _service.CacheFileExists
            ? $"Cache file has an unexpected size; delete it and regenerate ({CachePath})."
            : $"No cache file at {CachePath}; click \"Generate cache\" first.";
    }

    /// <summary>Generates the cache in the background, reporting progress.</summary>
    public Task GenerateCacheAsync()
    {
        if (IsGenerating)
            return Task.CompletedTask;
        _service ??= _serviceFactory();
        CachePath = _service.CacheFilePath;
        _generationCts = new CancellationTokenSource();
        var progress = new Progress<SearchProgress>(p =>
        {
            ProgressPercent = p.TotalCount == 0 ? 0 : (int)(100L * p.ProcessedCount / p.TotalCount);
            Status = $"{p.State}: {p.ProcessedCount:N0} / {p.TotalCount:N0}";
        });
        IsGenerating = true;
        _isBusy = true;
        Raise(nameof(IsBusy));
        Raise(nameof(IsNotBusy));
        var service = _service;
        var token = _generationCts.Token;
        return Task.Run(() =>
        {
            try
            {
                service.CreateCache(progress, token);
                Status = $"Cache generated: {service.TotalCount:N0} entries ({service.CacheFilePath}).";
            }
            catch (OperationCanceledException)
            {
                Status = "Cache generation cancelled.";
            }
            catch (Exception ex)
            {
                Status = "Cache generation failed: " + ex.Message;
            }
            finally
            {
                IsGenerating = false;
                _isBusy = false;
                Raise(nameof(IsBusy));
                Raise(nameof(IsNotBusy));
                ProgressPercent = 0;
            }
        }, CancellationToken.None);
    }

    public void CancelGeneration()
    {
        _generationCts?.Cancel();
        Status = "Cancelling cache generation...";
    }

    /// <summary>Runs the configured conditions over the loaded cache.</summary>
    public void Search()
    {
        if (_service == null)
        {
            LoadOrPrepareService();
            if (_service == null || !_service.CacheFileExists || !_service.CacheFileHasValidSize)
                return;
        }

        var service = _service;
        if (!service.CacheFileExists || !service.CacheFileHasValidSize)
        {
            Status = "Cache is missing or invalid; generate it first.";
            return;
        }

        try
        {
            service.LoadCache();
        }
        catch (InvalidDataException ex)
        {
            Status = ex.Message;
            return;
        }

        var conditions = BuildConditions();
        var hits = service.DungeonData
            .Where(d => conditions.Count == 0 || conditions.All(c => c.IsHit(d)))
            .ToList();
        hits = SelectedSortMode switch
        {
            SortMode.LevelAscending => hits.OrderBy(d => d.MapLevel).ThenBy(d => d.MapName).ToList(),
            SortMode.NameAscending => hits.OrderBy(d => d.MapName).ThenBy(d => d.MapLevel).ToList(),
            _ => hits.OrderByDescending(d => d.MapLevel).ThenBy(d => d.MapName).ToList(),
        };

        Results.Clear();
        foreach (var hit in hits.Take(5000))
            Results.Add(new SearchResultRow(hit.Seed, hit.Rank, hit.MapName, hit.MapLevel));
        ResultCount = $"{Results.Count:N0} result(s)" + (hits.Count > 5000 ? $" of {hits.Count:N0}" : "");
        Raise(nameof(HasResults));
        Status = $"Search done: {Results.Count:N0} shown / {hits.Count:N0} matched.";
    }

    /// <inheritdoc />
    public override string Header => "Map search";

    protected override void OnRefresh()
    {
        // the search screen has no per-block state; refresh the cache status only
        if (_service == null)
            LoadOrPrepareService();
    }
}

/// <summary>One search result (a seed × rank dungeon).</summary>
public sealed class SearchResultRow : ObservableObject
{
    public SearchResultRow(ushort seed, byte rank, string mapName, byte mapLevel)
    {
        Seed = seed;
        Rank = rank;
        MapName = mapName;
        MapLevel = mapLevel;
    }

    public ushort Seed { get; }

    public byte Rank { get; }

    public string MapName { get; }

    public byte MapLevel { get; }

    public string Display => $"{MapName}  [rank {Rank}, seed {Seed:X4}]";
}

public sealed class RankOption
{
    public RankOption(int index, byte rank)
    {
        Index = index;
        Rank = rank;
    }

    public int Index { get; }

    public byte Rank { get; }

    public override string ToString() => $"Rank {Rank}";
}

public sealed class NameOption
{
    public NameOption(int index, string name)
    {
        Index = index;
        Name = name;
    }

    public int Index { get; }

    public string Name { get; }

    public override string ToString() => Name;
}

public sealed class MapTypeOption
{
    public MapTypeOption(int value, string name)
    {
        Value = value;
        Name = name;
    }

    public int Value { get; }

    public string Name { get; }

    public override string ToString() => Name;
}
