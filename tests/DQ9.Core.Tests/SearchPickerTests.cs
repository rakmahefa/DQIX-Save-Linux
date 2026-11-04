using System;
using System.Linq;
using DQ9.Avalonia.ViewModels;
using DQ9.Avalonia.ViewModels.Screens;
using DQ9_Cheat.DataManager;
using DQ9_Cheat.GameData;
using Xunit;

namespace DQ9.Core.Tests;

/// <summary>
/// P4 completion: the searchable modal pickers that replace the WinForms
/// ItemSelectWindow (items) and HandleSelectWindow (profile handles).
/// The Avalonia window is a thin shell; filtering, type switching and
/// selection mapping are all exercised here headlessly.
/// </summary>
public class SearchPickerTests
{
    [Fact]
    public void ItemPicker_ListsEmptyRowFirstAndFiltersBySearch()
    {
        var picker = SearchPickerViewModel.ForItemTypes(new[] { (ItemType.Tool, "Items") });
        picker.Rebuild();

        Assert.False(picker.TypeSelectorVisible);
        Assert.Equal("Items", picker.Types.Single());
        // UX-09: the pseudo row says what it does…
        Assert.Equal("(Empty — clears slot)", picker.Rows[0].Display);
        Assert.Null(picker.Rows[0].Value);
        Assert.True(picker.Rows.Count > 100); // the full tool table is listed
        // …and is NOT preselected — a stray OK cannot wipe the slot.
        Assert.Equal(picker.Rows.First(r => r.Value != null), picker.SelectedRow);
        Assert.True(picker.ConfirmEnabled);

        picker.Search = "^Medic";
        Assert.True(picker.Rows.Count >= 2);
        Assert.Equal("(Empty — clears slot)", picker.Rows[0].Display); // the empty row always stays first
        Assert.All(picker.Rows.Skip(1),
            r => Assert.Matches("(?i)^Medic", r.Display));
        Assert.Contains(picker.Rows, r => r.Display == "Medicinal herb");

        // an invalid regex must not throw (WinForms crashed on it)
        picker.Search = "(unbalanced";
        Assert.True(picker.Rows.Count > 0);
    }

    [Fact]
    public void ItemPicker_ResultMapsToItemAndEmptyRowClears()
    {
        var manager = SaveFileBuilder.OpenSave(SaveFileBuilder.BuildSaveFile());
        manager.SelectedDataIndex = 0;
        manager.UndoRedoMgr.Initialize();
        var vm = new ItemsScreenViewModel();
        vm.Refresh();
        vm.SelectedSlot = vm.Slots[0];

        var picker = vm.CreateItemPicker();
        picker.Search = "^Medic";
        var row = picker.Rows.First(r => r.Value != null);
        vm.ApplyPickedItem(row.Value);
        var stored = manager.SaveData.ItemData.GetItemData(ItemType.Tool, 0);
        Assert.NotNull(stored);
        Assert.Equal(((ItemDataBase)row.Value).Value, stored.Value);

        // picking the "(Empty)" row clears the slot
        vm.ApplyPickedItem(null);
        Assert.Null(manager.SaveData.ItemData.GetItemData(ItemType.Tool, 0));
    }

    [Fact]
    public void HandlePicker_TypesAndSexFilter()
    {
        var picker = SearchPickerViewModel.ForHandle(0);
        picker.Rebuild();

        Assert.True(picker.TypeSelectorVisible);
        Assert.Equal("Occupations", picker.Types[0]);
        Assert.Equal("Accolades", picker.Types[1]);

        // type 0 lists every occupation
        Assert.Equal(ProfileJobList.List.Count, picker.Rows.Count);
        Assert.All(picker.Rows, r => Assert.IsType<ProfileJob>(r.Value));

        // type 1 lists accolades; with sex 0 only entries with a male name
        picker.SelectedTypeIndex = 1;
        Assert.True(picker.Rows.Count > 0);
        Assert.All(picker.Rows, r => Assert.IsType<TitleElement>(r.Value));
        Assert.All(picker.Rows,
            r => Assert.False(string.IsNullOrEmpty(((TitleElement)r.Value).MaleTitleName)));

        var female = SearchPickerViewModel.ForHandle(1);
        female.SelectedTypeIndex = 1;
        Assert.All(female.Rows,
            r => Assert.False(string.IsNullOrEmpty(((TitleElement)r.Value).LadyTitleName)));
    }

    [Fact]
    public void HandlePicker_SearchFiltersOccupations()
    {
        // the WinForms search is an unanchored case-insensitive regex, so a
        // plain letter matches anywhere in the name
        var picker = SearchPickerViewModel.ForHandle(0);
        picker.Search = "C";
        Assert.True(picker.Rows.Count > 0);
        // the pseudo-row keeps its fixed label; real occupations match the search
        Assert.All(picker.Rows.Where(r => r.Display != "(Keep current vocation)"),
            r => Assert.Matches("(?i)c", r.Display));

        // an anchored pattern narrows the list like in the original client
        picker.Search = "^C";
        Assert.True(picker.Rows.Count > 0);
        Assert.All(picker.Rows.Where(r => r.Display != "(Keep current vocation)"),
            r => Assert.Matches("(?i)^C", r.Display));
        // UX-23: the engine pseudo-entry is labelled so it cannot be mistaken
        // for one of the 59 real occupations
        Assert.Contains(picker.Rows, r => r.Display == "(Keep current vocation)");
    }

    [Fact]
    public void Picker_CancelMeansNoResult()
    {
        var picker = SearchPickerViewModel.ForHandle(0);
        picker.Rebuild();
        picker.SelectedRow = picker.Rows[0];
        // no Confirm() → Result stays null even with a selection
        Assert.False(picker.Confirmed);
        Assert.Null(picker.Result);

        picker.Confirm();
        Assert.True(picker.Confirmed);
        Assert.Same(picker.Rows[0].Value, picker.Result);
    }
}
