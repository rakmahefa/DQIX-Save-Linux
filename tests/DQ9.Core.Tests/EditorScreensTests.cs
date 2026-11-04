using System;
using System.Linq;
using DQ9.Avalonia.ViewModels.Screens;
using DQ9_Cheat.DataManager;

namespace DQ9.Core.Tests;

/// <summary>
/// P4 acceptance: every business screen must read the engine, write through
/// it (undo/redo included) and survive a save / reload round-trip. The ViewModels
/// are plain C# (no Avalonia runtime), so they run in the xUnit process.
/// </summary>
public class EditorScreensTests
{
    private static SaveDataManager OpenFixture()
    {
        var bytes = SaveFileBuilder.BuildSaveFile();
        var manager = SaveFileBuilder.OpenSave(bytes);
        manager.SelectedDataIndex = 0;
        // the screens share one SaveDataManager singleton per process; give
        // every test a clean undo history like a freshly opened file.
        manager.UndoRedoMgr.Initialize();
        return manager;
    }

    private static byte[] SaveAndReload(SaveDataManager manager)
    {
        var bytes = SaveFileBuilder.SaveToBytes(manager);
        return SaveFileBuilder.OpenSave(bytes).SaveData.Data;
    }

    // ---- General screen ----------------------------------------------------

    [Fact]
    public void GeneralScreen_EditsMoneyThroughEngine()
    {
        var manager = OpenFixture();
        var vm = new GeneralScreenViewModel();
        vm.Refresh();

        vm.Money = "1234567";
        Assert.Equal(1234567u, manager.SaveData.BasisData.Money.Value);

        // out-of-range input is ignored (engine clamps and the VM rejects junk)
        vm.Money = "not-a-number";
        Assert.Equal(1234567u, manager.SaveData.BasisData.Money.Value);
    }

    [Fact]
    public void GeneralScreen_UndoRestoresMoney()
    {
        var manager = OpenFixture();
        var vm = new GeneralScreenViewModel();
        vm.Refresh();
        var before = manager.SaveData.BasisData.Money.Value;

        vm.Money = "999999";
        Assert.Equal(999999u, manager.SaveData.BasisData.Money.Value);
        Assert.True(manager.UndoRedoMgr.Undo());
        Assert.Equal(before, manager.SaveData.BasisData.Money.Value);
        vm.Refresh();
        Assert.Equal(before.ToString(), vm.Money);
    }

    [Fact]
    public void GeneralScreen_ProfileMessageRoundTrips()
    {
        var manager = OpenFixture();
        var vm = new GeneralScreenViewModel();
        vm.Refresh();

        vm.ProfileMessage = "Hello Adventure!";
        Assert.Equal("Hello Adventure!", manager.SaveData.BasisData.ProfileMessage.Value);
        var reloaded = SaveAndReload(manager);
        // reopen through a fresh read
        var bytes = SaveFileBuilder.SaveToBytes(manager);
        SaveFileBuilder.OpenSave(bytes);
        Assert.Equal("Hello Adventure!", SaveDataManager.Instance.SaveData.BasisData.ProfileMessage.Value);
        _ = reloaded;
    }

    [Fact]
    public void GeneralScreen_RefreshPullsEngineState()
    {
        var manager = OpenFixture();
        var vm = new GeneralScreenViewModel();
        manager.SaveData.BasisData.Money.Value = 777;
        vm.Refresh();
        Assert.Equal("777", vm.Money);
    }

    // ---- Characters screen --------------------------------------------------

    [Fact]
    public void CharactersScreen_EditsNameAndStats()
    {
        var manager = OpenFixture();
        var vm = new CharactersScreenViewModel();
        vm.Refresh();

        vm.SelectedCharacter = vm.Characters[0];
        vm.Name = "Angelo";
        vm.MaxHp = "999";
        vm.NowHp = "999";
        Assert.Equal("Angelo", manager.SaveData.CharacterManager.CharacterData[0].Name.Value);
        Assert.Equal(999, manager.SaveData.CharacterManager.CharacterData[0].MaxHP);
        Assert.Equal(999, manager.SaveData.CharacterManager.CharacterData[0].NowHP);
    }

    [Fact]
    public void CharactersScreen_SetsAndClearsEquipment()
    {
        var manager = OpenFixture();
        var vm = new CharactersScreenViewModel();
        vm.Refresh();
        vm.SelectedCharacter = vm.Characters[0];

        var weapon = vm.Weapons.First(o => o.Item != null);
        vm.Weapon = weapon;
        Assert.Equal(weapon.Item.Value, manager.SaveData.CharacterManager.CharacterData[0].EquipmentWeapon.Value);

        vm.Weapon = ItemOption.None;
        Assert.Null(manager.SaveData.CharacterManager.CharacterData[0].EquipmentWeapon);
    }

    [Fact]
    public void CharactersScreen_JobLevelEditAndUndo()
    {
        var manager = OpenFixture();
        var vm = new CharactersScreenViewModel();
        vm.Refresh();
        vm.SelectedCharacter = vm.Characters[1];

        var row = vm.JobLevels[0];
        var before = manager.SaveData.CharacterManager.CharacterData[1].JobLevel[0].Value;
        row.Level.Value = 99;
        Assert.Equal(99, manager.SaveData.CharacterManager.CharacterData[1].JobLevel[0].Value);
        Assert.True(manager.UndoRedoMgr.Undo());
        Assert.Equal(before, manager.SaveData.CharacterManager.CharacterData[1].JobLevel[0].Value);
    }

    [Fact]
    public void CharactersScreen_JobExperienceEditAndUndo()
    {
        var manager = OpenFixture();
        var vm = new CharactersScreenViewModel();
        vm.Refresh();
        vm.SelectedCharacter = vm.Characters[1];

        // Paladin is list index 8 but save slot (DataIndex) 9.
        var row = vm.JobLevels.First(r => r.Name == "Paladin");
        var before = manager.SaveData.CharacterManager.CharacterData[1].JobExperience[9].Value;
        row.Experience.Value = 123456;
        Assert.Equal(123456u, manager.SaveData.CharacterManager.CharacterData[1].JobExperience[9].Value);
        Assert.True(manager.UndoRedoMgr.Undo());
        Assert.Equal(before, manager.SaveData.CharacterManager.CharacterData[1].JobExperience[9].Value);

        vm.Refresh();
        Assert.Equal(before, vm.JobLevels.First(r => r.Name == "Paladin").Experience.Value);
    }

    [Fact]
    public void CharactersScreen_JobRowsFollowDataIndexOrder()
    {
        var manager = OpenFixture();
        var vm = new CharactersScreenViewModel();
        vm.Refresh();
        vm.SelectedCharacter = vm.Characters[1];
        var character = manager.SaveData.CharacterManager.CharacterData[1];

        // JobDataList display order differs from the save slots for five jobs
        // (Paladin=9, Armamentalist=8, Ranger=12, Sage=10, Luminary=11); the
        // rows must address the DataIndex slot like the WinForms tool.
        var armamentalist = vm.JobLevels.First(r => r.Name == "Armamentalist");
        var beforeSlot8 = character.JobLevel[8].Value;
        armamentalist.Level.Value = 77;
        Assert.Equal(77, character.JobLevel[8].Value);
        Assert.True(manager.UndoRedoMgr.Undo());
        Assert.Equal(beforeSlot8, character.JobLevel[8].Value);

        // Ranger is list index 10, DataIndex 12.
        var ranger = vm.JobLevels.First(r => r.Name == "Ranger");
        ranger.Experience.Value = 987654;
        Assert.Equal(987654u, character.JobExperience[12].Value);

        // one row per save slot, each job exactly once
        Assert.Equal(13, vm.JobLevels.Count);
        Assert.Equal(13, vm.JobLevels.Select(r => r.Name).Distinct().Count());
    }

    [Fact]
    public void CharactersScreen_SelectedJobResolvesDataIndex()
    {
        var manager = OpenFixture();
        var vm = new CharactersScreenViewModel();
        vm.Refresh();
        var character = manager.SaveData.CharacterManager.CharacterData[0];

        // the engine stores Paladin as 9; the combo must show Paladin, not
        // the list item at position 9 (Armamentalist).
        character.Job.Value = 9;
        vm.Refresh();
        Assert.Equal("Paladin", vm.SelectedJob?.Name);

        vm.Characters[0].Reload();
        Assert.Contains("Paladin", vm.Characters[0].Display);
    }

    [Fact]
    public void CharactersScreen_SlotLabelsRefreshAfterRename()
    {
        OpenFixture();
        var vm = new CharactersScreenViewModel();
        vm.Refresh();
        vm.SelectedCharacter = vm.Characters[2];
        vm.Name = "Terry";

        vm.Characters[2].Reload();
        Assert.Contains("Terry", vm.Characters[2].Display);
    }

    // ---- Items screen --------------------------------------------------------

    [Fact]
    public void ItemsScreen_SetsBagItemAndCount()
    {
        var manager = OpenFixture();
        var vm = new ItemsScreenViewModel();
        vm.Refresh();
        vm.SelectedStore = vm.Stores[0]; // bag
        vm.SelectedSlot = vm.Slots[0];

        var item = vm.AllItems.First(o => o.Item != null);
        vm.SelectedItem = item;
        vm.Count = "42";
        Assert.Equal(item.Item.Value, manager.SaveData.ItemData.GetItemData(DQ9_Cheat.GameData.ItemType.Tool, 0).Value);
        Assert.Equal(42, manager.SaveData.ItemData.GetItemNum(DQ9_Cheat.GameData.ItemType.Tool, 0).Value);
    }

    [Fact]
    public void ItemsScreen_StoreRowCountsRefresh()
    {
        OpenFixture();
        var vm = new ItemsScreenViewModel();
        vm.Refresh();
        vm.SelectedStore = vm.Stores[0];
        vm.SelectedSlot = vm.Slots[0];
        vm.SelectedItem = vm.AllItems.First(o => o.Item != null);

        vm.Stores[0].Reload();
        Assert.Equal(1, vm.Stores[0].Filled);
    }

    [Fact]
    public void ItemsScreen_ToolPouchEdits()
    {
        var manager = OpenFixture();
        var vm = new ItemsScreenViewModel();
        vm.Refresh();

        vm.SelectedStore = null; // switch to tool pouch mode
        Assert.True(vm.ToolsMode);
        vm.SelectedPartyIndex = 2;
        vm.SelectedSlot = vm.ToolSlots[0];
        var item = vm.AllItems.First(o => o.Item != null);
        vm.SelectedItem = item;
        Assert.Equal(item.Item.Value, manager.SaveData.ItemData.GetTool(2, 0).Value);
    }

    // ---- Quests screen --------------------------------------------------------

    [Fact]
    public void QuestsScreen_StateTransitionsAndCounters()
    {
        var manager = OpenFixture();
        var vm = new QuestsScreenViewModel();
        vm.Refresh();
        vm.SelectedQuest = vm.Quests[0];

        vm.SelectedState = QuestState.Clear;
        Assert.Equal(QuestState.Clear, manager.SaveData.QuestDataManager.GetState(0));
        vm.ReviseClearCount();
        // the seeded fixture may already mark other quests clear
        Assert.True(manager.SaveData.QuestDataManager.ClearCount.Value >= 1);

        vm.SelectedState = QuestState.Order;
        Assert.Equal(QuestState.Order, manager.SaveData.QuestDataManager.GetState(0));
        vm.ReviseOrderCount();
        Assert.True(manager.SaveData.QuestDataManager.OrderCount >= 1);
    }

    [Fact]
    public void QuestsScreen_DateFieldsWrite()
    {
        var manager = OpenFixture();
        var vm = new QuestsScreenViewModel();
        vm.Refresh();
        vm.SelectedQuest = vm.Quests[0];

        vm.Year = "2010";
        vm.Month = "7";
        vm.Day = "11";
        Assert.Equal(2010, manager.SaveData.QuestDataManager.GetYear(0));
        Assert.Equal(7, manager.SaveData.QuestDataManager.GetMonth(0));
        Assert.Equal(11, manager.SaveData.QuestDataManager.GetDay(0));
    }

    // ---- Titles screen ---------------------------------------------------------

    [Fact]
    public void TitlesScreen_HoldFlagAndRevision()
    {
        var manager = OpenFixture();
        var vm = new TitlesScreenViewModel();
        vm.Refresh();
        vm.SelectedTitle = vm.Titles[0];

        manager.SaveData.TitleData.SetTitleHold(0, false); // normalize seeded fixture
        vm.Titles[0].Reload();
        Assert.False(manager.SaveData.TitleData.IsTitleHold(0));
        vm.Titles[0].IsHold = true;
        Assert.True(manager.SaveData.TitleData.IsTitleHold(0));
        vm.ReviseTitleCount();
        // the seeded fixture holds other titles too; flag 0 must be counted
        Assert.True(manager.SaveData.TitleData.TitleCount >= 1);
    }

    [Fact]
    public void TitlesScreen_FirstClearFields()
    {
        var manager = OpenFixture();
        var vm = new TitlesScreenViewModel();
        vm.Refresh();

        vm.VictoryCount = "123";
        Assert.Equal(123, manager.SaveData.FirstClearData.VictoryCount.Value);
    }

    // ---- Monsters screen ---------------------------------------------------------

    [Fact]
    public void MonstersScreen_VictoryCountAndLumpEdit()
    {
        var manager = OpenFixture();
        var vm = new MonstersScreenViewModel();
        vm.Refresh();
        vm.SelectedMonster = vm.Monsters[5];

        vm.Victory = "555";
        Assert.Equal(555, manager.SaveData.MonsterDataManager.GetVictoryCount(5));

        vm.LumpValue = "10";
        vm.ApplyLumpVictoryCount();
        Assert.All(Enumerable.Range(0, 307).Select(i => manager.SaveData.MonsterDataManager.GetVictoryCount(i)),
            c => Assert.Equal(10, c));

        // one undo reverts the whole lump edit
        Assert.True(manager.UndoRedoMgr.Undo());
        Assert.Equal(555, manager.SaveData.MonsterDataManager.GetVictoryCount(5));
    }

    [Fact]
    public void MonstersScreen_FindOutAndCompCount()
    {
        var manager = OpenFixture();
        for (var i = 0; i < MonsterDataManager.MonsterDataCount; i++)
            manager.SaveData.MonsterDataManager.SetVictoryCount(i, 0); // normalize seeded fixture
        var vm = new MonstersScreenViewModel();
        vm.Refresh();
        vm.SelectedMonster = vm.Monsters[0];
        vm.Victory = "1";
        vm.ReviseCompCount();
        Assert.Equal(1, manager.SaveData.MonsterDataManager.CompCount);
    }

    // ---- Flags screen --------------------------------------------------------------

    [Fact]
    public void FlagsScreen_ChangeJobAndDestination()
    {
        var manager = OpenFixture();
        var vm = new FlagsScreenViewModel();
        vm.Refresh();

        vm.AllowChangeJob[0].IsChecked = true;
        Assert.True(manager.SaveData.ProcessFlag.IsAllowChangeJob(0));

        vm.Destinations[0].IsChecked = true;
        Assert.True(manager.SaveData.ProcessFlag.IsAllowDestination(0));
    }

    // ---- Collections screen ----------------------------------------------------------

    [Fact]
    public void CollectionsScreen_AlchemyStateTransitions()
    {
        var manager = OpenFixture();
        // normalize the seeded fixture: the 471 alchemy flag words live at
        // block offset 15020 (AlchhemyData ctor) — zero them so the recipe
        // list starts empty like a fresh game.
        Array.Clear(manager.SaveData.Data, 15020, 471 * 2);
        var vm = new CollectionsScreenViewModel();
        vm.Refresh();
        vm.SelectedAlchemy = vm.Alchemy[0];

        vm.SelectedAlchemyState = AlchemyState.Created;
        Assert.Equal(AlchemyState.Created, manager.SaveData.AlchemyData.GetAlchemyState(vm.Alchemy[0].Index));

        vm.SelectedAlchemyState = AlchemyState.NotAppear;
        Assert.Equal(AlchemyState.NotAppear, manager.SaveData.AlchemyData.GetAlchemyState(vm.Alchemy[0].Index));
    }

    [Fact]
    public void CollectionsScreen_HoldFlags()
    {
        var manager = OpenFixture();
        var vm = new CollectionsScreenViewModel();
        vm.Refresh();

        var collectionRow = vm.Collection[0];
        collectionRow.IsChecked = true;
        Assert.True(manager.SaveData.ItemCollectionData.IsItemCollectionHold(collectionRow.DataIndex));

        var smartRow = vm.SmartItems[0];
        smartRow.IsChecked = true;
        Assert.True(manager.SaveData.SmartItemData.IsSmartItemHold(smartRow.DataIndex));
    }

    // ---- Inn screen -------------------------------------------------------------------

    [Fact]
    public void InnScreen_GuestEditsAndMove()
    {
        var manager = OpenFixture();
        var vm = new InnScreenViewModel();
        vm.Refresh();

        vm.SelectedVisitor = vm.Visitors[0];
        vm.Name = "VisitorA";
        vm.Level = "99";
        Assert.Equal("VisitorA", manager.SaveData.RikkaData.VisitorManager.VisitorData[0].Name.Value);
        Assert.Equal(99, manager.SaveData.RikkaData.VisitorManager.VisitorData[0].Level);

        vm.MoveVisitorDown();
        Assert.Equal("VisitorA", manager.SaveData.RikkaData.VisitorManager.VisitorData[1].Name.Value);
        Assert.Equal(99, manager.SaveData.RikkaData.VisitorManager.VisitorData[1].Level);
    }

    [Fact]
    public void InnScreen_RikkaGrade()
    {
        var manager = OpenFixture();
        var vm = new InnScreenViewModel();
        vm.Refresh();

        vm.Grade = "5";
        Assert.Equal(5, manager.SaveData.RikkaData.Grade);
        vm.Grade = "9"; // clamped by the engine to 6
        Assert.Equal(6, manager.SaveData.RikkaData.Grade);
    }

    // ---- Wi-Fi screen -------------------------------------------------------------------

    [Fact]
    public void WifiScreen_MessageAndGoods()
    {
        var manager = OpenFixture();
        var vm = new WifiScreenViewModel();
        vm.Refresh();

        vm.Message = "DQVC special!";
        Assert.Equal("DQVC special!", manager.SaveData.WifiShopData.Message);

        vm.SelectedGoodsIndex = 0;
        var item = vm.GoodsItems.First(o => o.Item != null);
        vm.SelectedGoodsItem = item;
        Assert.Equal(item.Item.Value, manager.SaveData.WifiShopData.WifiShopGoods[0].Item.Value);
    }

    [Fact]
    public void WifiScreen_ExpiryDates()
    {
        var manager = OpenFixture();
        var vm = new WifiScreenViewModel();
        vm.Refresh();

        vm.ExpiresYear = "2011";
        vm.ExpiresMonth = "3";
        Assert.Equal(2011, manager.SaveData.WifiShopData.ExpiresYear);
        Assert.Equal(3, manager.SaveData.WifiShopData.ExpiresMonth);
    }

    // ---- Cross-screen integration ----------------------------------------------------------

    [Fact]
    public void Screens_SaveReloadRoundTripKeepsEdits()
    {
        var manager = OpenFixture();
        var general = new GeneralScreenViewModel();
        var characters = new CharactersScreenViewModel();
        var titles = new TitlesScreenViewModel();
        general.Refresh();
        characters.Refresh();
        titles.Refresh();

        general.Money = "654321";
        characters.SelectedCharacter = characters.Characters[0];
        characters.MaxHp = "888";
        titles.Titles[1].IsHold = true;

        var bytes = SaveFileBuilder.SaveToBytes(manager);
        SaveFileBuilder.OpenSave(bytes);
        var data = SaveDataManager.Instance.SaveData;
        Assert.Equal(654321u, data.BasisData.Money.Value);
        Assert.Equal(888, data.CharacterManager.CharacterData[0].MaxHP);
        Assert.True(data.TitleData.IsTitleHold(1));
    }

    [Fact]
    public void Screens_RegistryRefreshesAll()
    {
        OpenFixture();
        var registry = new DQ9.Avalonia.ViewModels.ScreenRegistry();
        registry.RefreshAll();
        Assert.Equal(12, registry.Screens.Count);
        Assert.Equal("General", registry.General.Header);
    }

    [Fact]
    public void Screens_GuardedWhileRefreshing()
    {
        var manager = OpenFixture();
        var vm = new GeneralScreenViewModel();
        // Refresh must not push values back (no undo entries from a pure read)
        var undoDepth = manager.UndoRedoMgr.UndoCount;
        vm.Refresh();
        Assert.Equal(undoDepth, manager.UndoRedoMgr.UndoCount);
    }

    // ---- P4 completion: previously deferred editors ----------------------------------

    [Fact]
    public void GeneralScreen_GestureDirectionsWriteThroughEngine()
    {
        var manager = OpenFixture();
        var vm = new GeneralScreenViewModel();
        vm.Refresh();

        var before = manager.SaveData.BasisData.GestureUp;
        vm.GestureDirections[0].Selected = vm.Gestures[5];
        Assert.Equal(5, manager.SaveData.BasisData.GestureUp);
        Assert.True(manager.UndoRedoMgr.Undo());
        Assert.Equal(before, manager.SaveData.BasisData.GestureUp);

        // refresh pulls the engine byte back into the row
        vm.GestureDirections[0].Selected = vm.Gestures[7];
        vm.Refresh();
        Assert.Equal(vm.Gestures[7], vm.GestureDirections[0].Selected);
    }

    [Fact]
    public void GeneralScreen_HandleFromTitlePickerRoundTrips()
    {
        var manager = OpenFixture();
        var vm = new GeneralScreenViewModel();
        vm.Refresh();

        var picker = vm.CreateHandlePicker();
        Assert.Equal(2, picker.Types.Length);
        Assert.Equal("Occupations", picker.Types[0]);
        picker.SelectedTypeIndex = 1; // Accolades
        Assert.True(picker.Rows.Count > 0);
        var titleRow = picker.Rows.First(r => r.Value is DQ9_Cheat.GameData.TitleElement);
        vm.ApplyHandle(titleRow.Value);

        var handle = manager.SaveData.BasisData.Handle;
        Assert.NotNull(handle);
        Assert.Equal(DQ9_Cheat.GameData.HandleType.HandleTitle, handle.HandleType);

        vm.Refresh();
        Assert.Equal(titleRow.Display, vm.HandleDisplay);

        // byte-level round-trip keeps the title handle
        SaveAndReload(manager);
        var reopened = SaveDataManager.Instance.SaveData.BasisData.Handle;
        Assert.NotNull(reopened);
        Assert.Equal(DQ9_Cheat.GameData.HandleType.HandleTitle, reopened.HandleType);
        Assert.Equal(((DQ9_Cheat.GameData.TitleElement)titleRow.Value).DataIndex, reopened.Title.DataIndex);
    }

    [Fact]
    public void CharactersScreen_ReviseGridWritesClampsAndUndoes()
    {
        var manager = OpenFixture();
        var vm = new CharactersScreenViewModel();
        vm.Refresh();
        vm.SelectedCharacter = vm.Characters[0];

        var job = vm.SelectedEditJob!;
        var character = manager.SaveData.CharacterManager.CharacterData[0];
        var before = character.GetReviseMaxHP(job.DataIndex);

        Assert.Equal(9, vm.ReviseRows.Count);
        vm.ReviseRows[0].Value = 777; // Max HP
        Assert.Equal(777, character.GetReviseMaxHP(job.DataIndex));
        vm.ReviseRows[0].Value = 5000; // clamped to 999 like the WinForms NumericUpDown
        Assert.Equal(999, character.GetReviseMaxHP(job.DataIndex));
        Assert.True(manager.UndoRedoMgr.Undo());
        Assert.Equal(777, character.GetReviseMaxHP(job.DataIndex));
        Assert.True(manager.UndoRedoMgr.Undo());
        Assert.Equal(before, character.GetReviseMaxHP(job.DataIndex));
    }

    [Fact]
    public void CharactersScreen_JobSkillLevelsWriteThroughEngine()
    {
        var manager = OpenFixture();
        var vm = new CharactersScreenViewModel();
        vm.Refresh();
        vm.SelectedCharacter = vm.Characters[0];

        vm.SelectedEditJob = vm.Jobs.First(j => j.SkillList != null); // Guardian has none
        var job = vm.SelectedEditJob!;
        var character = manager.SaveData.CharacterManager.CharacterData[0];
        Assert.Equal(job.SkillList!.Length, vm.SkillLevelRows.Count);

        var skillIndex = job.SkillList[0].Index;
        var before = character.JobSkillLevel[skillIndex].Value;
        vm.SkillLevelRows[0].Value = 100;
        Assert.Equal(100, character.JobSkillLevel[skillIndex].Value);
        Assert.True(manager.UndoRedoMgr.Undo());
        Assert.Equal(before, character.JobSkillLevel[skillIndex].Value);
    }

    [Fact]
    public void CharactersScreen_SkillEffectBitsToggleAndUndo()
    {
        var manager = OpenFixture();
        var vm = new CharactersScreenViewModel();
        vm.Refresh();
        vm.SelectedCharacter = vm.Characters[0];

        var skill = vm.SelectedSkill!;
        var character = manager.SaveData.CharacterManager.CharacterData[0];
        var effect = skill.SkillEffectList.First();
        var row = vm.SkillEffectRows.First(r => r.Name == effect.Name);

        var before = character.IsLearnSkillSpecialtyEffect(effect.Index);
        row.IsChecked = !before;
        Assert.Equal(!before, character.IsLearnSkillSpecialtyEffect(effect.Index));
        Assert.True(manager.UndoRedoMgr.Undo());
        Assert.Equal(before, character.IsLearnSkillSpecialtyEffect(effect.Index));

        vm.LearnCheer = true;
        Assert.True(character.IsLearnCheer());
        vm.LearnRuler = true;
        Assert.True(character.IsLearnRular());
    }

    [Fact]
    public void CharactersScreen_TransmigrationWritesAndClamps()
    {
        var manager = OpenFixture();
        var vm = new CharactersScreenViewModel();
        vm.Refresh();
        vm.SelectedCharacter = vm.Characters[0];

        var dataIndex = vm.SelectedEditJob!.DataIndex;
        var character = manager.SaveData.CharacterManager.CharacterData[0];

        vm.Transmigration = "3";
        // the engine only rewrites the low nibble and clamps reads above 10,
        // so a random fixture byte with a high nibble reads back as 10
        var afterWrite = character.GetTransmigration(dataIndex);
        Assert.True(afterWrite == 3 || afterWrite == 10);

        vm.Transmigration = "42"; // over-range: clamped to 10 like the WinForms numeric
        Assert.Equal(10, character.GetTransmigration(dataIndex));

        // undo restores the byte written by the first edit
        Assert.True(manager.UndoRedoMgr.Undo());
        Assert.Equal(afterWrite, character.GetTransmigration(dataIndex));
    }

    [Fact]
    public void InnScreen_CompRatesRoundTrip()
    {
        var manager = OpenFixture();
        var vm = new InnScreenViewModel();
        vm.Refresh();
        vm.SelectedVisitor = vm.Visitors[0];

        var visitor = manager.SaveData.RikkaData.VisitorManager.VisitorData[0];
        vm.MonsterCompRate = "100";
        vm.ItemCompRate = "90";
        vm.FashionCompRate = "80";
        vm.AlchemyCompRate = "70";
        Assert.Equal(100, visitor.MonsterCompRate);
        Assert.Equal(90, visitor.ItemCompRate);
        Assert.Equal(80, visitor.FashionCompRate);
        Assert.Equal(70, visitor.AlchemyCompRate);

        vm.MonsterCompRate = "128"; // clamped to the 7-bit maximum like the WinForms NumericUpDown
        Assert.Equal(127, visitor.MonsterCompRate);

        // the comp rates share packed words with the other fields — a save /
        // reload cycle must keep every one of them
        var questClearBefore = visitor.QuestClearCount;
        var visitorCountBefore = visitor.VisitorCount;
        var reloaded = SaveFileBuilder.OpenSave(SaveFileBuilder.SaveToBytes(manager))
            .SaveData.RikkaData.VisitorManager.VisitorData[0];
        Assert.Equal(127, reloaded.MonsterCompRate);
        Assert.Equal(90, reloaded.ItemCompRate);
        Assert.Equal(80, reloaded.FashionCompRate);
        Assert.Equal(70, reloaded.AlchemyCompRate);
        Assert.Equal(questClearBefore, reloaded.QuestClearCount);
        Assert.Equal(visitorCountBefore, reloaded.VisitorCount);
    }
}
