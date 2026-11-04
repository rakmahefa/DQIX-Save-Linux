using System;
using System.IO;
using DQ9.Avalonia.ViewModels;
using DQ9.Avalonia.ViewModels.Screens;
using DQ9_Cheat.DataManager;
using Xunit;

namespace DQ9.Core.Tests;

/// <summary>
/// P6 acceptance: every screen edit must be byte-local — only the documented
/// bytes change, the CRCs are repaired and a save/reload round-trip is
/// byte-for-byte stable. Also covers the XDG path contract.
/// </summary>
public class ValidationTests
{
    private static SaveDataManager OpenFixture()
    {
        var bytes = SaveFileBuilder.BuildSaveFile();
        var manager = SaveFileBuilder.OpenSave(bytes);
        manager.SelectedDataIndex = 0;
        manager.UndoRedoMgr.Initialize();
        return manager;
    }

    /// <summary>Saves the current engine state and reopens it from bytes.</summary>
    private static SaveDataManager RoundTrip(SaveDataManager manager)
    {
        var bytes = SaveFileBuilder.SaveToBytes(manager);
        var reopened = SaveFileBuilder.OpenSave(bytes);
        reopened.UndoRedoMgr.Initialize();
        return reopened;
    }

    [Fact]
    public void MoneyEdit_ChangesOnlyItsDocumentedBytes()
    {
        var manager = OpenFixture();
        var before = (byte[])manager.SaveData.Data.Clone();
        var vm = new GeneralScreenViewModel();
        vm.Refresh();
        vm.Money = "123456";

        var after = manager.SaveData.Data;
        var changed = new System.Collections.Generic.List<int>();
        for (var i = 0; i < after.Length; i++)
            if (after[i] != before[i])
                changed.Add(i);

        // Money lives at 11448 (uint, little-endian) → 4 bytes
        Assert.Equal(new[] { 11448, 11449, 11450, 11451 }, changed);
    }

    [Fact]
    public void CharacterNameEdit_StaysWithinItsField()
    {
        var manager = OpenFixture();
        var vm = new CharactersScreenViewModel();
        vm.Refresh();
        vm.SelectedCharacter = vm.Characters[0];
        // snapshot after selection: the test isolates the rename, not the
        // selection-time initialisation writes.
        var before = (byte[])manager.SaveData.Data.Clone();
        vm.Name = "BEATRIX";

        var after = manager.SaveData.Data;
        var outside = new System.Collections.Generic.List<int>();
        for (var i = 0; i < after.Length; i++)
        {
            var inField = i is >= 456 and <= 466;  // name field: offset 456, 10 bytes + null
            if (!inField && after[i] != before[i])
                outside.Add(i);
        }
        Assert.True(outside.Count == 0,
            "unexpected edits outside the name field: " + string.Join(",", outside));
    }

    [Fact]
    public void SaveAfterEdits_ReproducesValidCrcs()
    {
        var manager = OpenFixture();
        var general = new GeneralScreenViewModel();
        general.Refresh();
        general.Money = "555555";

        var bytes = SaveFileBuilder.SaveToBytes(manager);
        // documented layout: CRC1 at [16..20) over [20..36); CRC2 at [132..136) over [136..28644)
        Assert.Equal(SaveFileBuilder.Crc32(bytes, 20, 16), (uint)SaveFileBuilder.ReadInt32Le(bytes, 16));
        Assert.Equal(SaveFileBuilder.Crc32(bytes, 136, 28508), (uint)SaveFileBuilder.ReadInt32Le(bytes, 132));
    }

    [Fact]
    public void FullScreenPipeline_RoundTripIsByteStable()
    {
        var manager = OpenFixture();
        var general = new GeneralScreenViewModel();
        var characters = new CharactersScreenViewModel();
        var items = new ItemsScreenViewModel();
        var quests = new QuestsScreenViewModel();
        var titles = new TitlesScreenViewModel();
        var monsters = new MonstersScreenViewModel();
        var flags = new FlagsScreenViewModel();
        var collections = new CollectionsScreenViewModel();
        var inn = new InnScreenViewModel();
        var wifi = new WifiScreenViewModel();
        foreach (var screen in new EditorScreenViewModel[]
                 {
                     general, characters, items, quests, titles, monsters, flags, collections, inn, wifi,
                 })
            screen.Refresh();

        // a deterministic edit per screen
        general.Money = "111111";
        characters.SelectedCharacter = characters.Characters[0];
        characters.MaxHp = "777";
        items.SelectedStore = items.Stores[0];
        items.SelectedSlot = items.Slots[0];
        items.SelectedItem = items.AllItems.First(o => o.Item != null);
        quests.SelectedQuest = quests.Quests[0];
        quests.SelectedState = QuestState.Clear;
        titles.Titles[0].IsHold = true;
        monsters.SelectedMonster = monsters.Monsters[0];
        monsters.Victory = "42";
        flags.Destinations[0].IsChecked = true;
        var visitor = inn.Visitors[0];
        inn.SelectedVisitor = visitor;
        inn.Name = "VALIDATE";
        wifi.SelectedGoodsIndex = 0;
        wifi.ExpiresYear = "2012";

        var first = SaveFileBuilder.SaveToBytes(manager);
        var reopened = RoundTrip(manager);
        var second = SaveFileBuilder.SaveToBytes(reopened);
        Assert.Equal(first, second);
    }

    [Fact]
    public void EditsSurviveBlockSwitch()
    {
        // block 2 exists only when the intermission flag is set
        var bytes = SaveFileBuilder.BuildSaveFile(intermissionBlock2: 1);
        var manager = SaveFileBuilder.OpenSave(bytes);
        manager.SelectedDataIndex = 0;
        manager.UndoRedoMgr.Initialize();
        var vm = new GeneralScreenViewModel();
        vm.Refresh();
        vm.Money = "222222";

        // the edit is written into block 1; switching to block 2 must show
        // different bytes, and back to block 1 must restore the edit.
        Assert.Equal(222222u, manager.SaveData.BasisData.Money.Value);
        manager.SelectedDataIndex = 1;
        var block2Money = manager.SaveData.BasisData.Money.Value;
        manager.SelectedDataIndex = 0;
        Assert.Equal(222222u, manager.SaveData.BasisData.Money.Value);
        Assert.NotEqual(222222u, block2Money);
    }

    [Fact]
    public void TruncatedFile_IsRejectedByCheckFile()
    {
        var bytes = SaveFileBuilder.BuildSaveFile();
        using var input = new MemoryStream(bytes, 0, 4096); // too small
        using var reader = new BinaryReader(input);
        Assert.True(SaveDataManager.Instance.CheckFile(reader) < 0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void XdgPaths_HonoursCacheEnvVar(bool defined)
    {
        var root = Path.Combine(Path.GetTempPath(), "dq9-xdg-" + Guid.NewGuid().ToString("N"));
        var custom = Path.Combine(root, "custom-cache");
        var originalCache = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
        try
        {
            if (defined)
            {
                Directory.CreateDirectory(custom);
                Environment.SetEnvironmentVariable("XDG_CACHE_HOME", custom);
            }
            else
            {
                Directory.CreateDirectory(root);
                Environment.SetEnvironmentVariable("XDG_CACHE_HOME", "relative/not/rooted");
            }

            var path = DQ9_Cheat.Services.XdgPaths.GetCacheFilePath("MapSearch.dat");
            if (defined)
                Assert.StartsWith(custom, path);
            else
                // a relative XDG value is ignored; the profile fallback applies
                Assert.EndsWith(Path.Combine(".cache", "dq9-save-editor", "MapSearch.dat"), path);
        }
        finally
        {
            Environment.SetEnvironmentVariable("XDG_CACHE_HOME", originalCache);
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }
}
