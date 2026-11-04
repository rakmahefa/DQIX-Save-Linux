using DQ9_Cheat.DataManager;
using System.Collections.ObjectModel;
using System.Linq;
using DQ9_Cheat.GameData;

namespace DQ9.Avalonia.ViewModels.Screens;

/// <summary>
/// Inventory editor (WinForms ItemDataControl parity). Ten item stores plus
/// the four party tool pouches; a detail panel edits the selected slot.
/// Tool pouch slots carry no quantity in the save format, so the count field
/// only applies to the ten regular stores.
/// </summary>
public sealed class ItemsScreenViewModel : EditorScreenViewModel
{
    private static readonly (ItemType Type, string Label, int Count)[] StoreDefs =
    {
        (ItemType.Tool, "Bag (tools)", 152),
        (ItemType.important, "Important items", 94),
        (ItemType.Weapon, "Weapons", 272),
        (ItemType.Shield, "Shields", 48),
        (ItemType.Head, "Head", 144),
        (ItemType.UpperBody, "Upper body", 192),
        (ItemType.Arm, "Arms", 80),
        (ItemType.LowerBody, "Lower body", 96),
        (ItemType.Shoe, "Shoes", 112),
        (ItemType.Accessory, "Accessories", 64),
    };

    private StoreRow? _selectedStore;
    private ItemSlotRow? _selectedSlot;
    private ItemOption? _selectedItem;
    private string _count = "0";
    private bool _toolsMode;
    private int _selectedPartyIndex;

    public ItemsScreenViewModel()
    {
        Stores = new ObservableCollection<StoreRow>(StoreDefs.Select(s => new StoreRow(s.Type, s.Label, s.Count)));
        var allItems = new ObservableCollection<ItemOption> { ItemOption.None };
        foreach (var type in StoreDefs.Select(s => s.Type))
            foreach (var item in ItemDataList.GetList(type, ""))
                allItems.Add(new ItemOption(item));
        AllItems = allItems;
        Slots = new ObservableCollection<ItemSlotRow>();
        SelectedStore = Stores[0];
    }

    public ObservableCollection<StoreRow> Stores { get; }

    public ObservableCollection<ItemOption> AllItems { get; }

    public ObservableCollection<ItemSlotRow> Slots { get; }

    /// <summary>Tool-pouch rows of the selected party member (8 slots, no counts).</summary>
    public ObservableCollection<ItemSlotRow> ToolSlots { get; } = new();

    public StoreRow? SelectedStore
    {
        get => _selectedStore;
        set
        {
            if (!Set(ref _selectedStore, value))
                return;
            ToolsMode = value == null;
            if (value != null)
                LoadStore();
        }
    }

    public bool ToolsMode
    {
        get => _toolsMode;
        private set => Set(ref _toolsMode, value);
    }

    public int SelectedPartyIndex
    {
        get => _selectedPartyIndex;
        set
        {
            if (!Set(ref _selectedPartyIndex, value))
                return;
            if (ToolsMode)
                LoadToolPouch();
        }
    }

    public ItemSlotRow? SelectedSlot
    {
        get => _selectedSlot;
        set
        {
            if (!Set(ref _selectedSlot, value))
                return;
            LoadSlotDetail();
            Raise(nameof(SlotLabel));
        }
    }

    /// <summary>UX-03: the editor readout matches the list numbering —
    /// selecting “#1” in the list must not show “Slot: 0” here.</summary>
    public string SlotLabel => SelectedSlot == null
        ? "(no slot selected)"
        : $"Slot #{SelectedSlot.Index + 1} of {(ToolsMode ? ToolSlots.Count : Slots.Count)}";

    public ItemOption? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (!Set(ref _selectedItem, value))
                return;
            if (SelectedSlot == null || IsRefreshing)
                return;
            WriteItem(value?.Item);
        }
    }

    /// <summary>Writes a picked item into the selected slot (shared by the
    /// ComboBox and the searchable modal of the WinForms ItemSelectWindow).</summary>
    private void WriteItem(ItemDataBase? item)
    {
        if (SelectedSlot == null || IsRefreshing || !EngineGuard.Ready)
            return;
        if (SelectedStore != null)
        {
            Data.ItemData.SetItemData(SelectedStore.Type, SelectedSlot.Index, item);
            RefreshSlot(SelectedSlot.Index);
        }
        else if (ToolsMode)
        {
            Data.ItemData.SetTool(SelectedPartyIndex, SelectedSlot.Index, item);
            RefreshToolSlot(SelectedSlot.Index);
        }
    }

    /// <summary>Searchable item picker of the WinForms ItemSelectWindow for the
    /// current context (the selected store's type, or tool in pouch mode).</summary>
    public SearchPickerViewModel CreateItemPicker()
    {
        var type = SelectedStore?.Type ?? ItemType.Tool;
        var picker = SearchPickerViewModel.ForItemTypes(new[] { (type, StoreDefs.First(s => s.Type == type).Label) });
        picker.Rebuild();
        return picker;
    }

    /// <summary>Applies the item picked in the modal (null clears the slot).</summary>
    public void ApplyPickedItem(object? value)
    {
        if (SelectedSlot == null)
            return;
        WriteItem(value as ItemDataBase);
        var item = value as ItemDataBase;
        _selectedItem = item == null ? ItemOption.None : FindOption(item) ?? ItemOption.None;
        Raise(nameof(SelectedItem));
    }

    public string Count
    {
        get => _count;
        set
        {
            if (!Set(ref _count, value))
                return;
            if (SelectedSlot == null || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(Count), "Count", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (SelectedStore == null)
                return;
            var num = Data.ItemData.GetItemNum(SelectedStore.Type, SelectedSlot.Index);
            if (num != null)
                num.Value = v;
            RefreshSlot(SelectedSlot.Index);
        }
    }

    private void LoadStore()
    {
        if (!EngineGuard.Ready)
            return;
        var store = SelectedStore!;
        Slots.Clear();
        for (var i = 0; i < store.Count; i++)
        {
            var item = Data.ItemData.GetItemData(store.Type, i);
            var num = Data.ItemData.GetItemNum(store.Type, i);
            Slots.Add(new ItemSlotRow(i, item?.Name ?? "(empty)", (num?.Value ?? 0).ToString()));
        }
        SelectedSlot = Slots.FirstOrDefault();
    }

    private void LoadToolPouch()
    {
        if (!EngineGuard.Ready)
            return;
        ToolSlots.Clear();
        for (var i = 0; i < 8; i++)
        {
            var item = Data.ItemData.GetTool(SelectedPartyIndex, i);
            ToolSlots.Add(new ItemSlotRow(i, item?.Name ?? "(empty)", "-"));
        }
        SelectedSlot = ToolSlots.FirstOrDefault();
    }

    private void LoadSlotDetail()
    {
        if (SelectedSlot == null)
            return;
        var index = SelectedSlot.Index;
        if (SelectedStore != null)
        {
            var item = Data.ItemData.GetItemData(SelectedStore.Type, index);
            _selectedItem = item == null ? ItemOption.None : FindOption(item);
            _count = (Data.ItemData.GetItemNum(SelectedStore.Type, index)?.Value ?? 0).ToString();
        }
        else
        {
            var item = Data.ItemData.GetTool(SelectedPartyIndex, index);
            _selectedItem = item == null ? ItemOption.None : FindOption(item);
            _count = "-";
        }
        Raise(nameof(SelectedItem));
        Raise(nameof(Count));
    }

    private ItemOption FindOption(ItemDataBase item)
    {
        return AllItems.FirstOrDefault(o => o.Item != null && o.Item.Value == item.Value) ?? ItemOption.None;
    }

    private void RefreshSlot(int index)
    {
        if (SelectedStore == null)
            return;
        var item = Data.ItemData.GetItemData(SelectedStore.Type, index);
        var num = Data.ItemData.GetItemNum(SelectedStore.Type, index);
        var row = Slots.FirstOrDefault(r => r.Index == index);
        if (row != null)
        {
            row.Name = item?.Name ?? "(empty)";
            row.Count = (num?.Value ?? 0).ToString();
        }
    }

    private void RefreshToolSlot(int index)
    {
        var item = Data.ItemData.GetTool(SelectedPartyIndex, index);
        var row = ToolSlots.FirstOrDefault(r => r.Index == index);
        if (row != null)
            row.Name = item?.Name ?? "(empty)";
    }

    /// <inheritdoc />
    public override string Header => "Items";

    protected override void OnRefresh()
    {
        foreach (var store in Stores)
            store.Reload();
        if (ToolsMode)
            LoadToolPouch();
        else if (SelectedStore != null)
            LoadStore();
    }
}

/// <summary>One selectable item store (bag, weapons...).</summary>
public sealed class StoreRow : ObservableObject
{
    private int _filled;

    public StoreRow(ItemType type, string label, int count)
    {
        Type = type;
        Label = label;
        Count = count;
        _filled = CountFilled();
    }

    public ItemType Type { get; }
    public string Label { get; }
    public int Count { get; }

    public int Filled
    {
        get => _filled;
        private set => Set(ref _filled, value);
    }

    public string Display => $"{Label} ({Filled}/{Count})";

    public void Reload()
    {
        Filled = CountFilled();
        Raise(nameof(Display));
    }

    private int CountFilled()
    {
        if (!EngineGuard.Ready)
            return 0;
        var data = SaveDataManager.Instance.SaveData.ItemData;
        var filled = 0;
        for (var i = 0; i < Count; i++)
            if (data.GetItemData(Type, i) != null)
                filled++;
        return filled;
    }
}
