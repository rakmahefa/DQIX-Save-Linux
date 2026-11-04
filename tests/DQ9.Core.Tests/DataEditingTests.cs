using System;
using DQ9_Cheat.DataManager;
using Xunit;

namespace DQ9.Core.Tests;

/// <summary>
/// P0 reference tests for field edition through the DataValue layer and the
/// undo / redo engine: modify a field, save, reopen; undo/redo round-trips;
/// EditFlag transitions.
/// </summary>
public class DataEditingTests
{
    private static SaveDataManager OpenDefaultSave()
    {
        var manager = SaveFileBuilder.OpenSave(SaveFileBuilder.BuildSaveFile());
        manager.SelectedDataIndex = 0; // normalize singleton state across tests
        return manager;
    }

    [Fact]
    public void ModifiedMoney_SurvivesSaveAndReload()
    {
        OpenDefaultSave();
        var money = SaveDataManager.Instance.SaveData.BasisData.Money;
        var target = money.Value == 123456u ? 654321u : 123456u;
        money.Value = target;
        Assert.Equal(target, money.Value);

        var saved = SaveFileBuilder.SaveToBytes(SaveDataManager.Instance);
        SaveFileBuilder.OpenSave(saved);
        Assert.Equal(target, SaveDataManager.Instance.SaveData.BasisData.Money.Value);
    }

    [Fact]
    public void ModifiedPlayTime_SurvivesSaveAndReload()
    {
        OpenDefaultSave();
        var basis = SaveDataManager.Instance.SaveData.BasisData;
        basis.PlayTimeHour.Value = 1111;
        basis.PlayTimeMinute.Value = 22;
        basis.PlayTimeSecond.Value = 33;

        var saved = SaveFileBuilder.SaveToBytes(SaveDataManager.Instance);
        SaveFileBuilder.OpenSave(saved);
        var reloaded = SaveDataManager.Instance.SaveData.BasisData;
        Assert.Equal((ushort)1111, reloaded.PlayTimeHour.Value);
        Assert.Equal((byte)22, reloaded.PlayTimeMinute.Value);
        Assert.Equal((byte)33, reloaded.PlayTimeSecond.Value);
    }

    [Fact]
    public void Money_IsClampedToItsDeclaredRange()
    {
        OpenDefaultSave();
        var money = SaveDataManager.Instance.SaveData.BasisData.Money;
        money.Value = 9999999u; // Max
        Assert.Equal(9999999u, money.Value);
        // 12 345 678 is above the legacy limit (9 999 999): the raw bytes
        // still hold the value but reads clamp to Max.
        SaveFileBuilder.WriteInt32Le(SaveDataManager.Instance.SaveData.Data, 11448, 12345678);
        Assert.Equal(9999999u, money.Value);
    }

    [Fact]
    public void UndoRestoresPreviousValue_AndRedoReappliesIt()
    {
        OpenDefaultSave();
        var manager = SaveDataManager.Instance;
        var money = manager.SaveData.BasisData.Money;
        var initial = money.Value;

        money.Value = initial == 777777u ? 777776u : 777777u;
        Assert.Equal(777777u, money.Value);
        Assert.True(manager.UndoRedoMgr.IsUndo);

        Assert.True(manager.UndoRedoMgr.Undo());
        Assert.Equal(initial, money.Value);
        Assert.True(manager.UndoRedoMgr.IsRedo);

        Assert.True(manager.UndoRedoMgr.Redo());
        Assert.Equal(initial == 777777u ? 777776u : 777777u, money.Value);
    }

    [Fact]
    public void EditFlag_FollowsTheUndoStack()
    {
        OpenDefaultSave();
        var manager = SaveDataManager.Instance;
        Assert.False(manager.UndoRedoMgr.EditFlag); // freshly loaded file

        var money = manager.SaveData.BasisData.Money;
        money.Value = money.Value == 12345u ? 12346u : 12345u;
        Assert.True(manager.UndoRedoMgr.EditFlag);

        manager.UndoRedoMgr.Undo();
        Assert.False(manager.UndoRedoMgr.EditFlag); // back to the saved state

        manager.UndoRedoMgr.Redo();
        Assert.True(manager.UndoRedoMgr.EditFlag);

        manager.UndoRedoMgr.OnSave(); // what WriteData does on save
        Assert.False(manager.UndoRedoMgr.EditFlag);
    }

    [Fact]
    public void UndoRedo_AreNoOpsWithoutEdits()
    {
        OpenDefaultSave();
        var manager = SaveDataManager.Instance;
        Assert.False(manager.UndoRedoMgr.Undo());
        Assert.False(manager.UndoRedoMgr.Redo());
        Assert.False(manager.UndoRedoMgr.IsUndo);
        Assert.False(manager.UndoRedoMgr.IsRedo);
    }

    [Fact]
    public void EditsDuringLoad_DoNotPolluteTheUndoStack()
    {
        // OnLoaded hooks (CharacterManager, AlchemyData, VisitorManager,
        // TreasureMapManager) run while the manager is in "loading" state;
        // they must not create undoable edits.
        var bytes = SaveFileBuilder.BuildSaveFile(intermissionBlock2: 1);
        SaveFileBuilder.OpenSave(bytes);
        var manager = SaveDataManager.Instance;
        Assert.False(manager.UndoRedoMgr.IsUndo);
        Assert.False(manager.UndoRedoMgr.EditFlag);
    }

    [Fact]
    public void UndoRedoManager_CanBeResetBetweenLoads()
    {
        OpenDefaultSave();
        var manager = SaveDataManager.Instance;
        var money = manager.SaveData.BasisData.Money;
        money.Value = money.Value == 1u ? 2u : 1u;
        Assert.True(manager.UndoRedoMgr.IsUndo);

        // Reopening a file clears the stacks (OnLoading / OnLoaded).
        OpenDefaultSave();
        Assert.False(manager.UndoRedoMgr.IsUndo);
        Assert.False(manager.UndoRedoMgr.IsRedo);
        Assert.False(manager.UndoRedoMgr.EditFlag);
    }
}
