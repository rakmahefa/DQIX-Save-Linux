using System;
using System.IO;
using DQ9_Cheat.Services;
using Xunit;

namespace DQ9.Core.Tests;

/// <summary>
/// P2 tests for the portable path service: XDG variables honoured when set,
/// documented fallbacks otherwise, isolated from the developer machine by
/// pointing HOME at a temporary directory.
/// </summary>
public class XdgPathsTests : IDisposable
{
    private readonly string _home;

    public XdgPathsTests()
    {
        _home = Path.Combine(Path.GetTempPath(), "dq9-xdg-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_home);
        Environment.SetEnvironmentVariable("HOME", _home);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", null);
        Environment.SetEnvironmentVariable("XDG_DATA_HOME", null);
        Environment.SetEnvironmentVariable("XDG_CACHE_HOME", null);
        Directory.Delete(_home, true);
    }

    [Fact]
    public void UnsetVariables_FallBackToHomeDirectories()
    {
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", null);
        Environment.SetEnvironmentVariable("XDG_DATA_HOME", null);
        Environment.SetEnvironmentVariable("XDG_CACHE_HOME", null);
        Assert.Equal(Path.Combine(_home, ".config"), XdgPaths.ConfigHome);
        Assert.Equal(Path.Combine(_home, ".local", "share"), XdgPaths.DataHome);
        Assert.Equal(Path.Combine(_home, ".cache"), XdgPaths.CacheHome);
    }

    [Fact]
    public void EmptyVariables_FallBackLikeUnset()
    {
        Environment.SetEnvironmentVariable("XDG_CACHE_HOME", "");
        Assert.Equal(Path.Combine(_home, ".cache"), XdgPaths.CacheHome);
    }

    [Fact]
    public void SetVariables_AreHonouredVerbatim()
    {
        var custom = Path.Combine(_home, "custom-cache");
        Environment.SetEnvironmentVariable("XDG_CACHE_HOME", custom);
        Assert.Equal(custom, XdgPaths.CacheHome);
        Assert.Equal(Path.Combine(custom, XdgPaths.AppFolder), XdgPaths.CacheDirectory());
    }

    [Fact]
    public void RelativeVariables_AreIgnored()
    {
        // The XDG spec requires an absolute path; a relative value must be
        // treated as unset.
        Environment.SetEnvironmentVariable("XDG_CACHE_HOME", "relative/cache");
        Assert.Equal(Path.Combine(_home, ".cache"), XdgPaths.CacheHome);
    }

    [Fact]
    public void GetCacheFilePath_LandsUnderTheCacheDirectory()
    {
        Environment.SetEnvironmentVariable("XDG_CACHE_HOME", null);
        var path = XdgPaths.GetCacheFilePath("MapSearch.dat");
        Assert.Equal(Path.Combine(_home, ".cache", XdgPaths.AppFolder, "MapSearch.dat"), path);
    }

    [Fact]
    public void EnsureDirectory_CreatesMissingFolders()
    {
        var directory = Path.Combine(_home, "nested", "dir");
        Assert.Equal(directory, XdgPaths.EnsureDirectory(directory));
        Assert.True(Directory.Exists(directory));
    }
}
