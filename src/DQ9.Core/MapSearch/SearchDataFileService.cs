using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DQ9_Cheat.GameData;
using JS_Framework;

namespace DQ9_Cheat.MapSearch;

/// <summary>Progress payload reported while generating the search cache.</summary>
public sealed class SearchProgress
{
    public SearchProgress(ProgressState state, int totalCount, int processedCount)
    {
        State = state;
        TotalCount = totalCount;
        ProcessedCount = processedCount;
    }

    public ProgressState State { get; }

    public int TotalCount { get; }

    public int ProcessedCount { get; }
}

/// <summary>
/// Portable, UI-agnostic replacement for the legacy static
/// <c>SearchDataFile</c> (WinForms + WMI + ThreadControl).
///
/// Differences with the legacy implementation, all documented in
/// docs/linux-port-notes.md:
///  - worker count comes from <c>Environment.ProcessorCount</c> instead of WMI
///    <c>Win32_Processor.NumberOfCores</c>;
///  - the cache path is injected instead of <c>Application.StartupPath</c>;
///  - workers are long-running tasks with cooperative cancellation
///    (CancellationToken) instead of STA threads polled through
///    <c>ThreadControl.IsStop</c>;
///  - shared counters are Interlocked-updated;
///  - the cache file is written through a temporary file and an atomic
///    replace, and its size is validated before any read.
/// </summary>
public sealed class SearchDataFileService
{
    /// <summary>Map ranks covered by the search database (legacy values).</summary>
    public static readonly byte[] RankList =
    [
        2,
        56,
        61,
        76,
        81,
        101,
        121,
        141,
        161,
        181,
        201,
        221
    ];

    private readonly byte[] _byteData;
    private long _processedCount;

    /// <param name="workerCount">Number of generation workers (clamped to &gt;= 1).</param>
    /// <param name="cacheFilePath">Full path of the MapSearch.dat cache file.</param>
    public SearchDataFileService(int workerCount, string cacheFilePath)
    {
        WorkerCount = Math.Max(1, workerCount);
        CacheFilePath = cacheFilePath;
        _byteData = new byte[TotalCount * SearchDungeonData.Size];
        DungeonData = new SearchDungeonData[TotalCount];
        var num1 = 0;
        var index1 = 0;
        for (ushort seed = 0; seed < 32768; ++seed)
            if (TreasureMapDataTable.GetReverseSeedTable(seed) != null)
                for (var index2 = 0; index2 < RankList.Length; ++index2)
                {
                    DungeonData[num1++] = new SearchDungeonData(index1, _byteData, seed, RankList[index2]);
                    index1 += SearchDungeonData.Size;
                }
    }

    /// <summary>Default service: one worker per logical processor, cache in the XDG user cache directory.</summary>
    public static SearchDataFileService CreateDefault()
    {
        return new SearchDataFileService(Environment.ProcessorCount,
            DQ9_Cheat.Services.XdgPaths.GetCacheFilePath("MapSearch.dat"));
    }

    /// <summary>Per-seed dungeon descriptors; values are backed by the shared <see cref="Data"/> buffer.</summary>
    public SearchDungeonData[] DungeonData { get; }

    /// <summary>The raw cache payload (legacy MapSearch.dat body format).</summary>
    public byte[] Data => _byteData;

    /// <summary>Injected cache file path.</summary>
    public string CacheFilePath { get; }

    /// <summary>Configured worker count used to split the index range.</summary>
    public int WorkerCount { get; }

    public int TotalCount { get; } = TreasureMapDataTable.SeedCount * RankList.Length;

    /// <summary>True when a cache file exists at <see cref="CacheFilePath"/>.</summary>
    public bool CacheFileExists => File.Exists(CacheFilePath);

    /// <summary>True when an existing cache file has exactly the expected byte length.</summary>
    public bool CacheFileHasValidSize
    {
        get
        {
            var path = CacheFilePath;
            if (!File.Exists(path))
                return false;
            var info = new FileInfo(path);
            return info.Length == (long)TotalCount * SearchDungeonData.Size;
        }
    }

    /// <summary>
    /// Loads the cache payload if present and size-valid. A missing file is a
    /// no-op (legacy behaviour); an invalid-size file throws so callers can
    /// regenerate it.
    /// </summary>
    public void LoadCache()
    {
        if (!File.Exists(CacheFilePath))
            return;
        if (!CacheFileHasValidSize)
            throw new InvalidDataException(
                $"Cache file '{CacheFilePath}' has an unexpected size; delete it and regenerate.");
        using var input = new FileStream(CacheFilePath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(input);
        reader.Read(_byteData, 0, TotalCount * SearchDungeonData.Size);
    }

    /// <summary>
    /// Computes every dungeon entry, then writes the cache atomically.
    /// Blocks until completion; run it from a background thread.
    /// </summary>
    /// <param name="progress">Optional progress sink (marshalled by the caller's IProgress implementation).</param>
    /// <param name="cancellationToken">Cancels all workers; the cache is then left untouched.</param>
    public void CreateCache(IProgress<SearchProgress> progress = null, CancellationToken cancellationToken = default)
    {
        var total = TotalCount;
        var workers = WorkerCount;
        Interlocked.Exchange(ref _processedCount, 0);
        progress?.Report(new SearchProgress(ProgressState.Preparing, total, 0));

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var tasks = new Task[workers];
        for (var w = 0; w < workers; ++w)
        {
            // Legacy range split kept verbatim: identical index assignment.
            var start = total / workers * w;
            var end = w != workers - 1 ? total / workers * (w + 1) : total;
            tasks[w] = Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        RunRange(start, end, progress, total, linked.Token);
                    }
                    catch (OperationCanceledException) when (linked.IsCancellationRequested &&
                                                             !cancellationToken.IsCancellationRequested)
                    {
                        // Cancelled because a sibling worker failed; surface the real error.
                        throw;
                    }
                    catch (Exception)
                    {
                        linked.Cancel(); // stop sibling workers, like the legacy wrapper did
                        throw;
                    }
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        try
        {
            Task.WaitAll(tasks);
        }
        catch (AggregateException ex)
        {
            // Collapse a pure cancellation (any number of workers) into a
            // single OperationCanceledException; surface the first real
            // error otherwise.
            OperationCanceledException cancellation = null;
            foreach (var inner in ex.InnerExceptions)
            {
                if (inner is OperationCanceledException oce)
                    cancellation ??= oce;
                else
                    throw inner;
            }

            if (cancellation != null)
                throw new OperationCanceledException("Cache generation cancelled.", cancellation.CancellationToken);
            throw;
        }

        linked.Token.ThrowIfCancellationRequested();
        WriteCacheAtomically();
        progress?.Report(new SearchProgress(ProgressState.Complete, total, total));
    }

    private void RunRange(int start, int end, IProgress<SearchProgress> progress, int total,
        CancellationToken cancellationToken)
    {
        var detail = new TreasureMapDetailData();
        var sinceNotify = 0;
        for (var index = start; index < end; ++index)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DungeonData[index].Calculate(detail);
            Interlocked.Increment(ref _processedCount);
            if (++sinceNotify > 512)
            {
                sinceNotify = 0;
                progress?.Report(new SearchProgress(ProgressState.Working, total,
                    (int)Interlocked.Read(ref _processedCount)));
            }
        }
    }

    /// <summary>Writes the current payload to the cache via temp file + atomic replace.</summary>
    public void WriteCacheAtomically()
    {
        var directory = Path.GetDirectoryName(CacheFilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        var tempPath = CacheFilePath + ".tmp";
        using (var output = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
        {
            using var writer = new BinaryWriter(output);
            writer.Write(_byteData, 0, TotalCount * SearchDungeonData.Size);
        }

        File.Move(tempPath, CacheFilePath, true);
    }

    /// <summary>
    /// Computes entries in <paramref name="start"/>..<paramref name="end"/>-1
    /// into the shared buffer using a dedicated detail instance. Used by tests
    /// to capture deterministic baselines on a subset of the table.
    /// </summary>
    public void CalculateRange(int start, int end, CancellationToken cancellationToken = default)
    {
        var detail = new TreasureMapDetailData();
        for (var index = start; index < end; ++index)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DungeonData[index].Calculate(detail);
        }
    }
}
