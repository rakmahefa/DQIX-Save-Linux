using System;
using Avalonia;
using DQ9.Avalonia.Infrastructure;

namespace DQ9.Avalonia;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var smokeMode = Environment.GetEnvironmentVariable("DQ9_SMOKE");
        if (smokeMode != null)
        {
            // Automated pipeline: headless engine run (open -> edit -> save as
            // -> reopen), no display required. Exit code 0 = smoke passed.
            Environment.Exit(SmokeRunner.Run(BuildAvaloniaApp(), smokeMode));
            return;
        }

        if (Environment.GetEnvironmentVariable("DQ9_SMOKE_UI") == "1")
        {
            // Display smoke test: start the real platform, show the window
            // for three seconds, then quit. Used under xvfb in CI.
            Environment.Exit(SmokeRunner.RunUiSmoke(BuildAvaloniaApp()));
            return;
        }

        var shotsDir = Environment.GetEnvironmentVariable("DQ9_SMOKE_SHOTS");
        if (shotsDir != null)
        {
            // Screenshot harness: renders every tab + modal pickers to PNG.
            Environment.Exit(SmokeRunner.RunShots(BuildAvaloniaApp(), shotsDir));
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
