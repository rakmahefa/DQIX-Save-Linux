using System;
using System.Threading;
using System.Threading.Tasks;
using DQ9_Cheat.GameData;
using JS_Framework;
using ProgressEventArgs = JS_Framework.ProgressEventArgs;
using ProgressEventHandler = JS_Framework.ProgressEventHandler;

namespace DQ9_Cheat.MapSearch;

/// <summary>
/// Compatibility shim over the portable <see cref="SearchDataFileService"/>
/// (DQ9.Core). Keeps the exact static surface the legacy WinForms screens
/// consume: <see cref="EventHandler"/>, <see cref="CreateSearchDataFile"/>,
/// <see cref="Cancel"/>, <see cref="LoadSearchDataFile"/>, <see cref="TotalCount"/>,
/// <see cref="DungeonData"/>, <see cref="RankList"/>, <see cref="FilePath"/>.
///
/// Events are still raised from a background thread; the consuming dialogs
/// already marshal with InvokeRequired. Generation uses
/// <see cref="Environment.ProcessorCount"/> workers and the cache lives in the
/// XDG user cache directory instead of the install directory.
/// </summary>
public static class SearchDataFile
{
    private static readonly SearchDataFileService Service = SearchDataFileService.CreateDefault();
    private static CancellationTokenSource _cancellationTokenSource;
    private static Task _generationTask;

    /// <summary>Map ranks covered by the search database.</summary>
    public static byte[] RankList => SearchDataFileService.RankList;

    /// <summary>Per-seed dungeon descriptors (shared payload buffer).</summary>
    public static SearchDungeonData[] DungeonData => Service.DungeonData;

    /// <summary>Cache file location (XDG user cache directory).</summary>
    public static string FilePath => Service.CacheFilePath;

    /// <summary>Total number of (seed, rank) entries.</summary>
    public static int TotalCount => Service.TotalCount;

    public static event ProgressEventHandler EventHandler;

    /// <summary>Loads the cache file when present and size-valid; silently skips otherwise.</summary>
    public static void LoadSearchDataFile()
    {
        if (!Service.CacheFileHasValidSize)
            return;
        Service.LoadCache();
    }

    /// <summary>Starts cache generation on a background thread (legacy contract).</summary>
    public static void CreateSearchDataFile()
    {
        if (_generationTask is { IsCompleted: false })
            return; // a generation is already running
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;
        _generationTask = Task.Run(() => Service.CreateCache(new LegacyProgressRelay(), token), token);
        // The service raises Complete through the relay; cancellation and
        // errors surface as task status — translate them to the legacy events.
        _generationTask.ContinueWith(t =>
        {
            if (t.IsFaulted)
                EventHandler?.Invoke(null, new ProgressEventArgs(ProgressState.InterruptByError,
                    TotalCount, TotalCount));
            else if (t.IsCanceled)
                EventHandler?.Invoke(null, new ProgressEventArgs(ProgressState.InterruptByUser,
                    TotalCount, TotalCount));
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    /// <summary>Requests cooperative cancellation of the running generation.</summary>
    public static void Cancel()
    {
        _cancellationTokenSource?.Cancel();
    }

    private sealed class LegacyProgressRelay : IProgress<SearchProgress>
    {
        public void Report(SearchProgress value)
        {
            if (value.State == ProgressState.Preparing)
                return; // legacy never raised a Preparing event
            EventHandler?.Invoke(null, new ProgressEventArgs(value.State, value.TotalCount, value.ProcessedCount));
        }
    }
}
