using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DQ9.Avalonia.ViewModels;

namespace DQ9.Avalonia.Infrastructure;

/// <summary>
/// Automated P3 smoke harness. Two modes:
///   DQ9_SMOKE=headless — engine pipeline without a display: generate a
///     synthetic save, open it through the view-model, edit money, save as,
///     reopen and verify. Exit code 0 on success.
///   DQ9_SMOKE_UI=1 — starts the real platform, shows the window three
///     seconds, then quits (used under xvfb-run in CI).
/// </summary>
public static class SmokeRunner
{
    public static int Run(AppBuilder builder, string mode)
    {
        try
        {
            builder.UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("SMOKE FAILED (platform init): " + ex.Message);
            return 2;
        }

        try
        {
            return RunEnginePipeline();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("SMOKE FAILED: " + ex);
            return 3;
        }
    }

    private static int RunEnginePipeline()
    {
        var directory = Path.Combine(Path.GetTempPath(), "dq9-smoke", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var source = Path.Combine(directory, "source.sav");
            var target = Path.Combine(directory, "target.sav");
            WriteSyntheticSave(source);

            string? pickedSavePath = null;
            var viewModel = new MainViewModel(
                () => Task.FromResult<string?>(source),
                () => Task.FromResult(pickedSavePath));

            if (!viewModel.OpenFile(source))
            {
                Console.Error.WriteLine("SMOKE FAILED: open rejected the synthetic save: " + viewModel.StatusText);
                return 4;
            }

            Check(viewModel, "money input filled from the engine", viewModel.MoneyInput.Length > 0);

            viewModel.MoneyInput = "123456";
            viewModel.ApplyMoneyCommand.Execute(null);
            Check(viewModel, "undo enabled after edit", viewModel.UndoCommand.CanExecute(null));

            // P4: business screens drive the same engine session.
            var characters = viewModel.Screens.Characters;
            characters.Refresh();
            characters.SelectedCharacter = characters.Characters[0];
            characters.MaxHp = "777";
            var quests = viewModel.Screens.Quests;
            quests.Refresh();
            quests.SelectedQuest = quests.Quests[0];
            quests.SelectedState = DQ9_Cheat.DataManager.QuestState.Clear;
            Check(viewModel, "character edit recorded (undo stack)", viewModel.UndoCommand.CanExecute(null));

            pickedSavePath = target;
            viewModel.SaveAsCommand.Execute(null);
            Check(viewModel, "save-as produced a same-size file",
                File.Exists(target) && new FileInfo(target).Length == new FileInfo(source).Length);

            // Fresh engine view over the saved file: the edit must persist.
            var reloaded = new MainViewModel(
                () => Task.FromResult<string?>(target),
                () => Task.FromResult<string?>(null));
            Check(reloaded, "reopen the saved file", reloaded.OpenFile(target));
            Check(reloaded, "money edit persisted", reloaded.MoneyInput == "123456");

            var reloadedCharacters = reloaded.Screens.Characters;
            reloadedCharacters.Refresh();
            reloadedCharacters.SelectedCharacter = reloadedCharacters.Characters[0];
            Check(reloaded, "character HP edit persisted", reloadedCharacters.MaxHp == "777");

            var reloadedQuests = reloaded.Screens.Quests;
            reloadedQuests.Refresh();
            reloadedQuests.SelectedQuest = reloadedQuests.Quests[0];
            Check(reloaded, "quest state persisted",
                reloadedQuests.SelectedState == DQ9_Cheat.DataManager.QuestState.Clear);

            Console.Out.WriteLine("SMOKE OK (headless): open -> edit -> save as -> reopen verified.");
            return 0;
        }
        finally
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch
            {
                // best effort cleanup
            }
        }
    }

    public static int RunUiSmoke(AppBuilder builder)
    {
        using var quit = new CancellationTokenSource();
        var result = 1;
        builder.AfterSetup(_ =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    var window = new Views.MainWindow();
                    window.Show();
                    Console.Out.WriteLine("window shown");
                    result = 0;
                    DispatcherTimer.RunOnce(() =>
                    {
                        window.Close();
                        quit.Cancel();
                    }, TimeSpan.FromSeconds(3));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("SMOKE UI FAILED: " + ex);
                    result = 1;
                    quit.Cancel();
                }
            });
        });
        builder.SetupWithoutStarting();
        Dispatcher.UIThread.MainLoop(quit.Token);
        Console.Out.WriteLine(result == 0 ? "SMOKE OK (ui): window shown and closed." : "SMOKE UI FAILED.");
        return result;
    }

    private static void Check(MainViewModel viewModel, string what, bool condition)
    {
        if (!condition)
            throw new InvalidDataException($"smoke check failed: {what} (status: {viewModel.StatusText})");
        Console.Out.WriteLine($"  ok: {what}");
    }

    /// <summary>
    /// Screenshot harness (DQ9_SMOKE_SHOTS=&lt;directory&gt;): opens a synthetic
    /// save, renders every business tab and the two modal pickers to PNG files
    /// so the UI can be reviewed without a display server.
    /// </summary>
    public static int RunShots(AppBuilder builder, string directory)
    {
        try
        {
            builder.UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                // render synchronously on the UI thread so the frames are
                // deterministic and CaptureRenderedFrame always has output
                ShouldRenderOnUIThread = true,
                // use the real Skia backend, otherwise bitmaps come out blank
                UseHeadlessDrawing = false,
            }).SetupWithoutStarting();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("SHOTS FAILED (platform init): " + ex.Message);
            return 2;
        }

        try
        {
            return Dispatcher.UIThread.Invoke(() => RunShotsCore(directory));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("SHOTS FAILED: " + ex);
            return 3;
        }
    }

    private static int RunShotsCore(string directory)
    {
        Directory.CreateDirectory(directory);
        var work = Path.Combine(Path.GetTempPath(), "dq9-shots", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            var source = Path.Combine(work, "source.sav");
            WriteSyntheticSave(source);

            // UX-08 first-run state: capture the shell before any file is open.
            var emptyViewModel = new MainViewModel(
                () => Task.FromResult<string?>(null),
                () => Task.FromResult<string?>(null));
            var emptyWindow = new Views.MainWindow { DataContext = emptyViewModel };
            emptyWindow.Show();
            Pump();
            Save(emptyWindow, Path.Combine(directory, "empty-state-no-save.png"));
            emptyWindow.Close();

            var viewModel = new MainViewModel(
                () => Task.FromResult<string?>(source),
                () => Task.FromResult<string?>(null));
            // create the window BEFORE the file is open: the shell builds its
            // own view-model in the constructor, and with an already-loaded
            // engine the screens would re-read and rewrite engine values at
            // construction time (phantom dirty flag + validation warnings).
            var window = new Views.MainWindow();
            window.DataContext = viewModel;
            if (!viewModel.OpenFile(source))
            {
                Console.Error.WriteLine("SHOTS FAILED: open rejected the synthetic save: " + viewModel.StatusText);
                return 4;
            }
            viewModel.Screens.RefreshAll();

            window.Show();
            Pump();

            var tabControl = window.GetVisualDescendants().OfType<TabControl>().First();
            var screens = viewModel.Screens.Screens;
            for (var i = 0; i < screens.Count; i++)
            {
                tabControl.SelectedIndex = i;
                Pump();
                Save(window, Path.Combine(directory, $"tab-{i:00}-{Safe(screens[i].Header)}.png"));
            }

            // Characters: dedicated capture of the "Job levels & experience"
            // card — it sits beyond the first viewport width in the WrapPanel,
            // so render the card visual itself (all 13 rows, no scrolling).
            tabControl.SelectedIndex = 1;
            Pump();
            var jobsCard = window.GetVisualDescendants().OfType<Border>()
                .FirstOrDefault(b => b.Classes.Contains("card")
                                     && b.Child is StackPanel sp
                                     && sp.Children.OfType<TextBlock>()
                                         .Any(t => t.Classes.Contains("group")
                                                   && t.Text == "Job levels & experience"));
            if (jobsCard != null)
            {
                Pump();
                SaveVisual(jobsCard, Path.Combine(directory, "tab-01-Characters-jobs.png"));
            }

            // UX-01 demo: a failed numeric parse shows the Fluent error border
            // plus the status-bar warning (captured on the General tab).
            tabControl.SelectedIndex = 0;
            Pump();
            viewModel.Screens.General.Money = "12abc";
            Pump();
            Save(window, Path.Combine(directory, "validation-error-demo.png"));
            viewModel.Screens.General.Refresh();

            Shoot(new Views.SearchPickerWindow
            {
                DataContext = viewModel.Screens.Items.CreateItemPicker()
            }, directory, "modal-item-picker");
            Shoot(new Views.SearchPickerWindow
            {
                DataContext = viewModel.Screens.General.CreateHandlePicker()
            }, directory, "modal-handle-picker");

            window.Close();
            Console.Out.WriteLine("SHOTS OK: " + Directory.EnumerateFiles(directory, "*.png").Count()
                                  + " captures in " + directory);
            return 0;
        }
        finally
        {
            try { Directory.Delete(work, true); } catch { /* best effort */ }
        }
    }

    private static void Shoot(Window picker, string directory, string name)
    {
        picker.Show();
        Pump();
        Save(picker, Path.Combine(directory, name + ".png"));
        picker.Close();
    }

    private static void Save(TopLevel window, string path)
    {
        // explicit offscreen render: the headless frame buffer is not used,
        // so draw the window visual directly into a render-target bitmap
        var width = (int)Math.Ceiling(window.ClientSize.Width);
        var height = (int)Math.Ceiling(window.ClientSize.Height);
        if (width <= 0 || height <= 0)
            throw new InvalidDataException("window has no size for " + path);
        using var bitmap = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
        bitmap.Render(window);
        bitmap.Save(path, 90);
    }

    private static void SaveVisual(Visual visual, string path)
    {
        // renders a single control (e.g. one card of a scrolled WrapPanel)
        // at its full laid-out size, independent of any ancestor clipping
        var width = (int)Math.Ceiling(visual.Bounds.Width);
        var height = (int)Math.Ceiling(visual.Bounds.Height);
        if (width <= 0 || height <= 0)
            throw new InvalidDataException("visual has no size for " + path);
        using var bitmap = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
        bitmap.Render(visual);
        bitmap.Save(path, 90);
    }

    private static void Pump()
    {
        // run every queued dispatcher job (layout included) so the frame is complete
        Dispatcher.UIThread.Invoke(() => { }, DispatcherPriority.Background);
    }

    private static string Safe(string header)
    {
        var chars = header.Where(char.IsLetterOrDigit).ToArray();
        return chars.Length == 0 ? "tab" : new string(chars);
    }

    /// <summary>Synthetic save with the two-block layout (see docs/save-format.md).</summary>
    private static void WriteSyntheticSave(string path)
    {
        using var ms = new MemoryStream();
        ms.Write(Block(0x1234, 0));
        ms.Write(Block(0x5555 ^ 0x1234, 1));
        File.WriteAllBytes(path, ms.ToArray());
    }

    private static byte[] Block(ushort seed, byte intermission)
    {
        var block = new byte[32768];
        var signature = System.Text.Encoding.ASCII.GetBytes("DRAGON QUEST IX");
        Array.Copy(signature, block, signature.Length);
        uint state = seed == 0 ? 0x9E3779B9u : seed;
        for (var i = signature.Length; i < block.Length; ++i)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            block[i] = (byte)(state & 0xFF);
        }

        block[20] = intermission;
        WriteInt32(block, 16, (int)Crc32(block, 20, 16));
        WriteInt32(block, 132, (int)Crc32(block, 136, 28508));
        return block;
    }

    private static uint Crc32(byte[] data, int offset, int length)
    {
        uint crc = uint.MaxValue;
        for (uint i = 0; i < length; ++i)
        {
            crc ^= data[offset + i];
            for (var bit = 0; bit < 8; ++bit)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
        }

        return crc ^ uint.MaxValue;
    }

    private static void WriteInt32(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
    }
}
