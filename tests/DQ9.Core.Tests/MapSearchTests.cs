using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DQ9_Cheat.MapSearch;
using JS_Framework;
using Xunit;

namespace DQ9.Core.Tests;

/// <summary>
/// P0 tests for the portable map-search engine: deterministic baselines,
/// cache file handling (size validation, atomic write) and cancellation.
/// The full-table parallelism baseline is opt-in through
/// DQ9_SEARCH_FULL_BASELINE=1 because a complete pass takes about a minute.
/// </summary>
public class MapSearchTests : IDisposable
{
    private readonly string _directory;

    public MapSearchTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "dq9-core-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }

    private SearchDataFileService CreateService(int workers = 1, string subDirectory = "source")
    {
        var path = Path.Combine(_directory, subDirectory, "MapSearch.dat");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return new SearchDataFileService(workers, path);
    }

    [Fact]
    public void TotalCount_MatchesSeedTableTimesRankList()
    {
        // 32466 valid seeds x 12 ranks — frozen from the reference tables.
        Assert.Equal(389592, CreateService().TotalCount);
        Assert.Equal(12, SearchDataFileService.RankList.Length);
    }

    [Fact]
    public void CalculateRange_MatchesTheFrozenBaseline()
    {
        var service = CreateService();
        service.CalculateRange(0, 24);
        var expected = Baseline.Rows;
        for (var i = 0; i < 24; ++i)
        {
            var fields = expected[i];
            var data = service.DungeonData[i];
            Assert.True(data.Seed == fields.Seed && data.Rank == fields.Rank,
                $"row {i}: seed/rank mismatch");
            Assert.True(data.MapName1Index == fields.Name1 && data.MapName2Index == fields.Name2 &&
                        data.MapName3Index == fields.Name3 && data.MapType == fields.Type,
                $"row {i}: name/type mismatch");
            Assert.True(data.MapLevel == fields.Level && data.MonsterRank == fields.MonsterRank &&
                        data.Boss == fields.Boss && data.Depth == fields.Depth,
                $"row {i}: level/monster/boss/depth mismatch");
            Assert.True(data.SBoxCount == fields.S && data.ABoxCount == fields.A &&
                        data.BBoxCount == fields.B && data.CBoxCount == fields.C &&
                        data.DBoxCount == fields.D && data.EBoxCount == fields.E &&
                        data.FBoxCount == fields.F && data.GBoxCount == fields.G &&
                        data.HBoxCount == fields.H && data.IBoxCount == fields.I,
                $"row {i}: chest counts mismatch");
        }
    }

    [Fact]
    public void CalculateRange_IsDeterministicAcrossIndependentRuns()
    {
        var first = CreateService();
        var second = CreateService();
        first.CalculateRange(0, 256);
        second.CalculateRange(0, 256);
        Assert.Equal(
            first.Data.AsSpan(0, 256 * SearchDungeonData.Size).ToArray(),
            second.Data.AsSpan(0, 256 * SearchDungeonData.Size).ToArray());
    }

    [Fact]
    public void WriteCacheAtomically_ThenLoad_RestoresThePayload()
    {
        var source = CreateService();
        source.CalculateRange(0, 32);
        source.WriteCacheAtomically();
        Assert.True(source.CacheFileExists);
        Assert.True(source.CacheFileHasValidSize);
        Assert.False(File.Exists(source.CacheFilePath + ".tmp")); // atomic replace leaves no residue

        var target = CreateService(subDirectory: "target");
        Assert.False(target.CacheFileHasValidSize);
        File.Copy(source.CacheFilePath, target.CacheFilePath); // deliver the cache
        Assert.True(target.CacheFileHasValidSize);
        target.LoadCache(); // size-valid -> loads
        Assert.Equal(
            source.Data.AsSpan(0, 32 * SearchDungeonData.Size).ToArray(),
            target.Data.AsSpan(0, 32 * SearchDungeonData.Size).ToArray());
    }

    [Fact]
    public void LoadCache_RejectsAFileWithUnexpectedSize()
    {
        var service = CreateService();
        File.WriteAllBytes(service.CacheFilePath, new byte[100]);
        Assert.Throws<InvalidDataException>(() => service.LoadCache());
    }

    [Fact]
    public void LoadCache_IsANoOpWhenTheFileIsMissing()
    {
        var service = CreateService();
        service.LoadCache(); // must not throw (legacy behaviour)
    }

    [Fact]
    public async Task CreateCache_CanBeCancelledAndWritesNothing()
    {
        var service = CreateService(2);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var progress = new Progress<SearchProgress>(p =>
        {
            if (p.State == ProgressState.Working)
                started.TrySetResult();
        });
        using var cts = new CancellationTokenSource();
        var task = Task.Run(() => service.CreateCache(progress, cts.Token), CancellationToken.None);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(60));
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.False(service.CacheFileExists); // a cancelled run must not produce a cache
    }

    [Fact]
    public async Task CreateCache_FullRun_ProducesIdenticalOutputWithOneAndFourWorkers()
    {
        if (Environment.GetEnvironmentVariable("DQ9_SEARCH_FULL_BASELINE") != "1")
            return; // opt-in: full generation takes ~1 minute per worker setup
        var single = CreateService(1);
        var quad = CreateService(4);
        single.CreateCache();
        quad.CreateCache();
        Assert.True(single.CacheFileHasValidSize);
        Assert.True(quad.CacheFileHasValidSize);
        var left = await File.ReadAllBytesAsync(single.CacheFilePath);
        var right = await File.ReadAllBytesAsync(quad.CacheFilePath);
        Assert.Equal(left, right);

        // Spot-check the frozen baseline inside the full output.
        quad.CalculateRange(0, 0); // no-op, keeps API surface exercised
        var service = CreateService();
        service.LoadCache();
        for (var i = 0; i < 24; ++i)
        {
            var data = service.DungeonData[i];
            var expected = Baseline.Rows[i];
            Assert.Equal(expected.Level, data.MapLevel);
            Assert.Equal(expected.Boss, data.Boss);
            Assert.Equal(expected.Depth, data.Depth);
        }
    }

    /// <summary>Frozen P0 baseline captured from the reference implementation.</summary>
    private static class Baseline
    {
        // index;seed;rank;name1;name2;name3;type;level;monsterRank;boss;
        // depth;S;A;B;C;D;E;F;G;H;I
        public static readonly Row[] Rows = Parse("""
            0;0;2;4;3;1;1;3;1;0;4;0;0;0;0;0;0;0;0;2;3
            1;0;56;4;3;2;1;12;2;0;6;0;0;0;0;0;0;0;1;2;5
            2;0;61;4;3;2;1;15;2;1;6;0;0;0;0;0;0;0;1;2;5
            3;0;76;7;3;3;1;27;3;1;9;0;0;0;0;0;0;3;3;6;3
            4;0;81;7;6;3;1;33;3;3;9;0;0;0;0;0;0;3;3;6;3
            5;0;101;7;6;9;1;42;4;3;11;0;0;0;0;1;4;4;5;4;1
            6;0;121;7;6;6;1;51;4;4;13;0;0;0;0;2;4;5;5;6;1
            7;0;141;10;9;9;1;51;5;6;10;0;0;0;0;2;3;3;5;3;0
            8;0;161;10;9;9;1;54;5;7;10;0;0;0;0;2;3;3;5;3;0
            9;0;181;15;9;6;1;78;9;8;13;0;4;2;3;6;4;2;0;0;2
            10;0;201;15;3;10;1;63;9;1;15;0;5;2;3;8;4;2;0;0;2
            11;0;221;15;3;6;1;66;9;1;16;0;5;2;3;9;4;2;0;0;2
            12;1;2;5;2;0;3;11;3;2;2;0;0;0;0;0;0;0;0;0;0
            13;1;56;5;2;17;3;20;4;2;4;0;0;0;0;0;0;2;2;1;1
            14;1;61;5;2;17;3;20;4;2;4;0;0;0;0;0;0;2;2;1;1
            15;1;76;10;2;0;3;29;5;2;6;0;0;0;0;1;2;2;2;2;0
            16;1;81;10;5;0;3;35;5;4;6;0;0;0;0;1;2;2;2;2;0
            17;1;101;10;5;16;3;44;6;4;8;0;0;0;1;2;4;4;3;1;0
            18;1;121;10;5;19;3;50;6;4;10;0;0;0;3;2;6;4;3;1;0
            19;1;141;8;5;20;3;74;7;5;16;0;1;5;3;7;4;8;2;0;0
            20;1;161;8;13;20;3;86;7;9;16;0;1;5;3;7;4;8;2;0;0
            21;1;181;8;8;19;3;68;7;8;11;0;1;3;2;4;4;6;2;0;0
            22;1;201;13;2;6;3;56;9;1;12;0;5;2;4;4;3;2;0;0;4
            23;1;221;13;2;20;3;62;9;1;14;0;6;2;4;6;3;2;0;0;4
            """);

        public record Row(byte Seed, byte Rank, byte Name1, byte Name2, byte Name3, byte Type,
            byte Level, byte MonsterRank, byte Boss, byte Depth,
            byte S, byte A, byte B, byte C, byte D, byte E, byte F, byte G, byte H, byte I);

        private static Row[] Parse(string text)
        {
            var rows = new Row[24];
            var index = 0;
            foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var f = line.Trim().Split(';');
                rows[index++] = new Row(
                    byte.Parse(f[1]), byte.Parse(f[2]), byte.Parse(f[3]), byte.Parse(f[4]),
                    byte.Parse(f[5]), byte.Parse(f[6]), byte.Parse(f[7]), byte.Parse(f[8]),
                    byte.Parse(f[9]), byte.Parse(f[10]), byte.Parse(f[11]), byte.Parse(f[12]),
                    byte.Parse(f[13]), byte.Parse(f[14]), byte.Parse(f[15]), byte.Parse(f[16]),
                    byte.Parse(f[17]), byte.Parse(f[18]), byte.Parse(f[19]), byte.Parse(f[20]));
            }

            return rows;
        }
    }
}
