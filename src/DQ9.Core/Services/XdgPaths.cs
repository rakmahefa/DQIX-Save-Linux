using System;
using System.IO;
using System.Runtime.InteropServices;

namespace DQ9_Cheat.Services;

/// <summary>
/// Portable application-path service following the XDG Base Directory
/// specification. Replaces every direct write next to the executable so the
/// application stays read-only-friendly (installed directories, AppImage
/// mounts...).
///
/// Fallbacks follow the XDG spec:
///   config : $XDG_CONFIG_HOME or ~/.config
///   data   : $XDG_DATA_HOME   or ~/.local/share
///   cache  : $XDG_CACHE_HOME  or ~/.cache
/// </summary>
public static class XdgPaths
{
    /// <summary>Application folder name used under the XDG base directories.</summary>
    public const string AppFolder = "dq9-save-editor";

    /// <summary>XDG base dir for user configuration.</summary>
    public static string ConfigHome => GetBaseDir("XDG_CONFIG_HOME", ".config");

    /// <summary>XDG base dir for durable user data.</summary>
    public static string DataHome => GetBaseDir("XDG_DATA_HOME", ".local/share");

    /// <summary>XDG base dir for regenerable cache data.</summary>
    public static string CacheHome => GetBaseDir("XDG_CACHE_HOME", ".cache");

    /// <summary>Directory for persistent configuration files.</summary>
    public static string ConfigDirectory(string appName = AppFolder)
    {
        return Path.Combine(ConfigHome, appName);
    }

    /// <summary>Directory for durable user data files.</summary>
    public static string DataDirectory(string appName = AppFolder)
    {
        return Path.Combine(DataHome, appName);
    }

    /// <summary>Directory for regenerable cache files.</summary>
    public static string CacheDirectory(string appName = AppFolder)
    {
        return Path.Combine(CacheHome, appName);
    }

    /// <summary>Full path of a cache file (e.g. MapSearch.dat), without creating it.</summary>
    public static string GetCacheFilePath(string fileName, string appName = AppFolder)
    {
        return Path.Combine(CacheDirectory(appName), fileName);
    }

    /// <summary>Creates <paramref name="directory"/> if missing; returns the path.</summary>
    public static string EnsureDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        return directory;
    }

    private static string GetBaseDir(string variable, string fallbackRelative)
    {
        var value = Environment.GetEnvironmentVariable(variable);
        if (!string.IsNullOrWhiteSpace(value) && Path.IsPathRooted(value))
            return value;
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(profile, "AppData", "Local", fallbackRelative)
            : Path.Combine(profile, fallbackRelative);
    }
}
