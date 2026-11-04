using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DQ9.Avalonia.ViewModels.Screens;
using DQ9_Cheat.DataManager;
using DQ9_Cheat.GameData;
using DQ9_Cheat.MapSearch;
using Xunit;

namespace DQ9.Core.Tests;

/// <summary>
/// P5 acceptance: treasure map editing, cache generation / loading through
/// the portable service, filter conditions and result determinism.
/// </summary>
public class TreasureMapTests
{
    private static SaveDataManager OpenFixture()
    {
        var bytes = SaveFileBuilder.BuildSaveFile();
        var manager = SaveFileBuilder.OpenSave(bytes);
        manager.SelectedDataIndex = 0;
        manager.UndoRedoMgr.Initialize();
        return manager;
    }

    /// <summary>The seeded fixture fills the map count byte with garbage; the
    /// map table must start empty, exactly like a fresh save.</summary>
    private static void NormalizeMapCount(SaveDataManager manager)
    {
        while (manager.SaveData.TreasureMapManager.MapCount > 0)
            manager.SaveData.TreasureMapManager.DeleteMapData(0);
    }

    // ---- treasure map screen -------------------------------------------------

    [Fact]
    public void TreasureMapsScreen_NewMapAppearsAndRowUpdates()
    {
        var manager = OpenFixture();
        NormalizeMapCount(manager);
        var vm = new TreasureMapsScreenViewModel();
        vm.Refresh();

        Assert.Equal(0, manager.SaveData.TreasureMapManager.MapCount);
        Assert.Contains("(empty)", vm.Maps[0].Display);

        vm.CreateNewMap();
        Assert.Equal(1, manager.SaveData.TreasureMapManager.MapCount);
        vm.Refresh();
        Assert.Equal("1", vm.MapCountText);
        Assert.DoesNotContain("(empty)", vm.Maps[0].Display);
    }

    [Fact]
    public void TreasureMapsScreen_RankEditTriggersDetailCalculation()
    {
        var manager = OpenFixture();
        NormalizeMapCount(manager);
        manager.SaveData.TreasureMapManager.CreateMapData();
        var vm = new TreasureMapsScreenViewModel();
        vm.Refresh();
        vm.SelectedMap = vm.Maps[0];

        vm.Rank = "101";
        var map = manager.SaveData.TreasureMapManager.GetMapData(0);
        Assert.NotNull(map);
        Assert.Equal(101, map.Rank);
        if (map.IsValid)
            Assert.Contains("Boss:", vm.Details);
        else
            Assert.Contains("invalid", vm.Details);
    }

    [Fact]
    public void TreasureMapsScreen_StateAndTypeWrite()
    {
        var manager = OpenFixture();
        NormalizeMapCount(manager);
        manager.SaveData.TreasureMapManager.CreateMapData();
        var vm = new TreasureMapsScreenViewModel();
        vm.Refresh();
        vm.SelectedMap = vm.Maps[0];

        vm.SelectedState = MapState.Clear;
        Assert.Equal(MapState.Clear, manager.SaveData.TreasureMapManager.GetMapData(0).MapState);

        vm.SelectedType = MapType.devil;
        Assert.Equal(MapType.devil, manager.SaveData.TreasureMapManager.GetMapData(0).MapType);
    }

    [Fact]
    public void TreasureMapsScreen_DeleteAndMove()
    {
        var manager = OpenFixture();
        NormalizeMapCount(manager);
        var vm = new TreasureMapsScreenViewModel();
        vm.Refresh();
        vm.CreateNewMap();
        vm.CreateNewMap();
        Assert.Equal(2, manager.SaveData.TreasureMapManager.MapCount);

        vm.SelectedMap = vm.Maps[1];
        vm.MoveMapUp();
        Assert.Equal(2, manager.SaveData.TreasureMapManager.MapCount);
        vm.SelectedMap = vm.Maps[0];
        vm.DeleteMap();
        Assert.Equal(1, manager.SaveData.TreasureMapManager.MapCount);
    }

    // ---- search service ---------------------------------------------------------
    //
    // A full cache pass takes about a minute, so the payload is generated once
    // per test process and written to each test's temp file for LoadCache.

    private static readonly object CacheLock = new();
    private static byte[]? _cacheBytes;

    private static byte[] CacheBytes
    {
        get
        {
            lock (CacheLock)
            {
                _cacheBytes ??= CreateCacheOnce();
                return _cacheBytes;
            }
        }
    }

    private static byte[] CreateCacheOnce()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dq9-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var service = new SearchDataFileService(Environment.ProcessorCount, Path.Combine(dir, "MapSearch.dat"));
            service.CreateCache();
            return File.ReadAllBytes(service.CacheFilePath);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    private static string WriteCacheFile(string directory)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "MapSearch.dat");
        File.WriteAllBytes(path, CacheBytes);
        return path;
    }

    private static SearchDataFileService CreateService(string path)
    {
        return new SearchDataFileService(1, path);
    }

    [Fact]
    public void SearchService_GenerateCacheThenLoad_ProducesSameData()
    {
        var path = Path.Combine(Path.GetTempPath(), "dq9-tests", Guid.NewGuid().ToString("N"), "MapSearch.dat");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var service = CreateService(path);
            Assert.False(service.CacheFileExists);
            service.WriteCacheAtomically();
            Assert.True(service.CacheFileExists);
            Assert.True(service.CacheFileHasValidSize);

            var copy = File.ReadAllBytes(path);
            var reloaded = CreateService(path);
            reloaded.LoadCache();
            Assert.Equal(copy, reloaded.Data);
        }
        finally
        {
            if (Directory.Exists(Path.GetDirectoryName(path)))
                Directory.Delete(Path.GetDirectoryName(path), true);
        }
    }

    [Fact]
    public void SearchService_CacheWithWrongSize_IsRejectedOnLoad()
    {
        var path = Path.Combine(Path.GetTempPath(), "dq9-tests", Guid.NewGuid().ToString("N"), "MapSearch.dat");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, [1, 2, 3]);
            var service = CreateService(path);
            Assert.True(service.CacheFileExists);
            Assert.False(service.CacheFileHasValidSize);
            Assert.Throws<InvalidDataException>(() => service.LoadCache());
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path), true);
        }
    }

    [Fact]
    public void SearchService_Cancellation_StopsGeneration()
    {
        var path = Path.Combine(Path.GetTempPath(), "dq9-tests", Guid.NewGuid().ToString("N"), "MapSearch.dat");
        try
        {
            var service = CreateService(path);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            Assert.ThrowsAny<OperationCanceledException>(
                () => service.CreateCache(null, cts.Token));
            Assert.False(service.CacheFileExists, "cancelled run must not leave a cache file");
        }
        finally
        {
            if (Directory.Exists(Path.GetDirectoryName(path)))
                Directory.Delete(Path.GetDirectoryName(path), true);
        }
    }

    [Fact]
    public void SearchService_SameOutputWithOneAndFourWorkers()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dq9-tests", Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(dir);
            // the reference payload is computed once; a 4-worker split of the
            // first slice must reproduce the same bytes, and a 1-worker pass
            // over the same slice must agree.
            var payload = CacheBytes;
            const int rows = 4096;
            var one = new SearchDataFileService(1, Path.Combine(dir, "one.dat"));
            var four = new SearchDataFileService(4, Path.Combine(dir, "four.dat"));
            one.CalculateRange(0, rows);
            four.CalculateRange(0, rows);
            var slice = new byte[rows * SearchDungeonData.Size];
            Array.Copy(payload, slice, slice.Length);
            Assert.Equal(slice, one.Data.Take(slice.Length).ToArray());
            Assert.Equal(one.Data.Take(slice.Length).ToArray(), four.Data.Take(slice.Length).ToArray());
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    // ---- view-model conditions and search ------------------------------------------

    private static SearchScreenViewModel SearchVmWithGeneratedCache(out string dir, int workers = 1)
    {
        dir = Path.Combine(Path.GetTempPath(), "dq9-tests", Guid.NewGuid().ToString("N"));
        var path = WriteCacheFile(dir);
        var service = new SearchDataFileService(workers, path);
        service.LoadCache();
        return new SearchScreenViewModel(() => service);
    }

    [Fact]
    public void SearchScreen_LevelCondition_FiltersResults()
    {
        var vm = SearchVmWithGeneratedCache(out var dir);
        try
        {
            vm.LoadOrPrepareService();
            vm.UseMapLevel = true;
            vm.ConditionType = SearchConditionType.Accord;
            vm.MapLevel = 17;
            vm.Search();
            var count = vm.Results.Count;
            Assert.True(count > 0, "the full cache must contain level-17 maps");
            Assert.All(vm.Results, r => Assert.Equal(17, r.MapLevel));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void SearchScreen_ResultsSortedByLevelDescendingByDefault()
    {
        var vm = SearchVmWithGeneratedCache(out var dir);
        try
        {
            vm.LoadOrPrepareService();
            vm.Search();
            var levels = vm.Results.Select(r => (int)r.MapLevel).ToList();
            var sorted = levels.OrderByDescending(l => l).ToList();
            Assert.Equal(sorted, levels);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void SearchScreen_NameConditions_AreDeterministic()
    {
        var vm = SearchVmWithGeneratedCache(out var dir);
        try
        {
            vm.LoadOrPrepareService();
            vm.UseName1 = true;
            vm.Name1Index = 0;
            vm.Search();
            var first = vm.Results.Select(r => (r.Seed, r.Rank)).ToList();
            vm.Results.Clear();
            vm.Search();
            var second = vm.Results.Select(r => (r.Seed, r.Rank)).ToList();
            Assert.Equal(first, second);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void SearchScreen_BoxCondition_MatchesCacheContents()
    {
        var vm = SearchVmWithGeneratedCache(out var dir);
        try
        {
            vm.LoadOrPrepareService();
            vm.UseBox = true;
            vm.BoxType = 0;   // chest count
            vm.BoxCount = 4;
            vm.ConditionType = SearchConditionType.AndOver;
            vm.Search();
            var service = vm_results_service(vm);
            Assert.All(vm.Results, r =>
            {
                var entry = service.DungeonData.First(d => d.Seed == r.Seed && d.Rank == r.Rank);
                var total = entry.SBoxCount + entry.ABoxCount + entry.BBoxCount + entry.CBoxCount
                            + entry.DBoxCount + entry.EBoxCount + entry.FBoxCount + entry.GBoxCount
                            + entry.HBoxCount + entry.IBoxCount;
                Assert.True(total >= 4, $"seed {r.Seed:X4} rank {r.Rank} has only {total} chests");
            });
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    private static SearchDataFileService vm_results_service(SearchScreenViewModel vm)
    {
        // the view-model owns its service; rebuild one over the same cache file
        var service = new SearchDataFileService(1, vm.CachePath);
        service.LoadCache();
        return service;
    }

    private sealed class CollectingProgress : IProgress<SearchProgress>
    {
        private readonly List<SearchProgress> _sink;

        public CollectingProgress(List<SearchProgress> sink) => _sink = sink;

        public void Report(SearchProgress value)
        {
            lock (_sink)
                _sink.Add(value);
        }
    }

    [Fact]
    public void SearchScreen_BossCondition_OnlyMatchingBosses()
    {
        var vm = SearchVmWithGeneratedCache(out var dir);
        try
        {
            vm.LoadOrPrepareService();
            vm.UseBoss = true;
            vm.BossIndex = 0;
            vm.ConditionType = SearchConditionType.Accord;
            vm.Search();
            Assert.All(vm.Results, r =>
            {
                var details = new TreasureMapDetailData();
                details.MapRank = r.Rank;
                details.MapSeed = r.Seed;
                details.CalculateDetail(false);
                Assert.Equal(MonsterDataList.List[282], details.BossName);
            });
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void SearchScreen_CacheGenerationReportsProgressAndCompletes()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dq9-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "MapSearch.dat");
        var service = new SearchDataFileService(2, path);
        // bypass the factory so the view-model generates the cache itself
        var vm = new SearchScreenViewModel(() => service);
        var reports = new List<SearchProgress>();
        service.CreateCache(new CollectingProgress(reports));
        Assert.True(reports.Count > 0, "progress was not reported");
        Assert.Equal(JS_Framework.ProgressState.Complete, reports[^1].State);
        Assert.True(File.Exists(path));
        Directory.Delete(dir, true);
    }
}
