using DQ9_Cheat.DataManager;
using System.Collections.ObjectModel;
using System.Linq;
using DQ9_Cheat.GameData;

namespace DQ9.Avalonia.ViewModels.Screens;

/// <summary>
/// DQVC Wi-Fi shopping data (WinForms WifiShoppingDataControl parity):
/// shop message, expiry / next-connect dates and the six goods slots.
/// </summary>
public sealed class WifiScreenViewModel : EditorScreenViewModel
{
    private string _message = "";
    private string _expiresYear = "0";
    private string _expiresMonth = "0";
    private string _expiresDay = "0";
    private string _expiresHour = "0";
    private string _nextConnectYear = "0";
    private string _nextConnectMonth = "0";
    private string _nextConnectDay = "0";
    private string _nextConnectHour = "0";
    private string _messageExpiresYear = "0";
    private string _messageExpiresMonth = "0";
    private string _messageExpiresDay = "0";
    private string _messageExpiresHour = "0";
    private int _selectedGoodsIndex;
    private ItemOption? _selectedGoodsItem;
    private string _goodsCount = "0";
    private string _goodsPrice = "0";

    public WifiScreenViewModel()
    {
        var allItems = new ObservableCollection<ItemOption> { ItemOption.None };
        foreach (var item in ItemDataList.GetList(ItemType.Tool, ""))
            allItems.Add(new ItemOption(item));
        GoodsItems = allItems;
        // UX-03: goods slots display 1-based ("Slot #1") matching the Items list.
        GoodsSlots = new ObservableCollection<EnumOption<int>>(
            Enumerable.Range(0, WifiShopping.WifiShopGoodsCount)
                .Select(i => new EnumOption<int>(i, $"Slot #{i + 1}")));
        _selectedGoodsItem = GoodsItems[0];
        _selectedGoodsSlot = GoodsSlots[0];
    }

    public ObservableCollection<ItemOption> GoodsItems { get; }

    /// <summary>UX-03: 1-based goods-slot rows (the save stores 0–5).</summary>
    public ObservableCollection<EnumOption<int>> GoodsSlots { get; }

    private EnumOption<int>? _selectedGoodsSlot;

    public EnumOption<int>? SelectedGoodsSlot
    {
        get => _selectedGoodsSlot;
        set
        {
            if (!Set(ref _selectedGoodsSlot, value) || value == null)
                return;
            SelectedGoodsIndex = value.Value;
        }
    }

    public string Message
    {
        get => _message;
        set
        {
            if (!Set(ref _message, value) || IsRefreshing)
                return;
            if (EngineGuard.Ready && value.Length > Data.WifiShopData.MessageMaxLength)
            {
                SetFieldError(nameof(Message),
                    $"Shop message is limited to {Data.WifiShopData.MessageMaxLength} characters.");
                return;
            }
            ClearFieldError(nameof(Message));
            if (EngineGuard.Ready)
                Data.WifiShopData.Message = value;
        }
    }

    public string ExpiresYear
    {
        get => _expiresYear;
        set
        {
            if (!Set(ref _expiresYear, value) || IsRefreshing)
                return;
            if (!ValidateParse<ushort>(nameof(ExpiresYear), "Goods expiry (year)", value, ushort.TryParse,
                    "a whole number between 0 and 65 535", out var v))
                return;
            if (EngineGuard.Ready)
                Data.WifiShopData.ExpiresYear = v;
        }
    }

    public string ExpiresMonth
    {
        get => _expiresMonth;
        set
        {
            if (!Set(ref _expiresMonth, value) || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(ExpiresMonth), "Goods expiry (month)", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                Data.WifiShopData.ExpiresMonth = v;
        }
    }

    public string ExpiresDay
    {
        get => _expiresDay;
        set
        {
            if (!Set(ref _expiresDay, value) || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(ExpiresDay), "Goods expiry (day)", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                Data.WifiShopData.ExpiresDay = v;
        }
    }

    public string ExpiresHour
    {
        get => _expiresHour;
        set
        {
            if (!Set(ref _expiresHour, value) || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(ExpiresHour), "Goods expiry (hour)", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                Data.WifiShopData.ExpiresHour = v;
        }
    }

    public string NextConnectYear
    {
        get => _nextConnectYear;
        set
        {
            if (!Set(ref _nextConnectYear, value) || IsRefreshing)
                return;
            if (!ValidateParse<ushort>(nameof(NextConnectYear), "Next connect (year)", value, ushort.TryParse,
                    "a whole number between 0 and 65 535", out var v))
                return;
            if (EngineGuard.Ready)
                Data.WifiShopData.AllowNextConnectYear = v;
        }
    }

    public string NextConnectMonth
    {
        get => _nextConnectMonth;
        set
        {
            if (!Set(ref _nextConnectMonth, value) || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(NextConnectMonth), "Next connect (month)", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                Data.WifiShopData.AllowNextConnectMonth = v;
        }
    }

    public string NextConnectDay
    {
        get => _nextConnectDay;
        set
        {
            if (!Set(ref _nextConnectDay, value) || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(NextConnectDay), "Next connect (day)", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                Data.WifiShopData.AllowNextConnectDay = v;
        }
    }

    public string NextConnectHour
    {
        get => _nextConnectHour;
        set
        {
            if (!Set(ref _nextConnectHour, value) || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(NextConnectHour), "Next connect (hour)", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                Data.WifiShopData.AllowNextConnectHour = v;
        }
    }

    public string MessageExpiresYear
    {
        get => _messageExpiresYear;
        set
        {
            if (!Set(ref _messageExpiresYear, value) || IsRefreshing)
                return;
            if (!ValidateParse<ushort>(nameof(MessageExpiresYear), "Message expiry (year)", value, ushort.TryParse,
                    "a whole number between 0 and 65 535", out var v))
                return;
            if (EngineGuard.Ready)
                Data.WifiShopData.MessageExpiresYear = v;
        }
    }

    public string MessageExpiresMonth
    {
        get => _messageExpiresMonth;
        set
        {
            if (!Set(ref _messageExpiresMonth, value) || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(MessageExpiresMonth), "Message expiry (month)", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                Data.WifiShopData.MessageExpiresMonth = v;
        }
    }

    public string MessageExpiresDay
    {
        get => _messageExpiresDay;
        set
        {
            if (!Set(ref _messageExpiresDay, value) || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(MessageExpiresDay), "Message expiry (day)", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                Data.WifiShopData.MessageExpiresDay = v;
        }
    }

    public string MessageExpiresHour
    {
        get => _messageExpiresHour;
        set
        {
            if (!Set(ref _messageExpiresHour, value) || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(MessageExpiresHour), "Message expiry (hour)", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                Data.WifiShopData.MessageExpiresHour = v;
        }
    }

    public int SelectedGoodsIndex
    {
        get => _selectedGoodsIndex;
        set
        {
            if (!Set(ref _selectedGoodsIndex, value))
                return;
            _selectedGoodsSlot = GoodsSlots.FirstOrDefault(o => o.Value == value);
            Raise(nameof(SelectedGoodsSlot));
            LoadGoodsDetail();
        }
    }

    public ItemOption? SelectedGoodsItem
    {
        get => _selectedGoodsItem;
        set
        {
            if (!Set(ref _selectedGoodsItem, value))
                return;
            if (!IsRefreshing)
                Data.WifiShopData.WifiShopGoods[SelectedGoodsIndex].Item = value?.Item;
            LoadGoodsDetail();
        }
    }

    public string GoodsCount
    {
        get => _goodsCount;
        set
        {
            if (!Set(ref _goodsCount, value) || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(GoodsCount), "Goods count", value, byte.TryParse,
                    "a whole number between 0 and 255", out var v))
                return;
            if (EngineGuard.Ready)
                Data.WifiShopData.WifiShopGoods[SelectedGoodsIndex].ItemCount = v;
        }
    }

    public string GoodsPrice
    {
        get => _goodsPrice;
        private set => Set(ref _goodsPrice, value);
    }

    private void LoadGoodsDetail()
    {
        var goods = Data.WifiShopData.WifiShopGoods[SelectedGoodsIndex];
        var item = goods.Item;
        _selectedGoodsItem = item == null
            ? ItemOption.None
            : GoodsItems.FirstOrDefault(o => o.Item != null && o.Item.Value == item.Value) ?? ItemOption.None;
        _goodsCount = goods.ItemCount.ToString();
        _goodsPrice = goods.ItemPrice.ToString();
        Raise(nameof(SelectedGoodsItem));
        Raise(nameof(GoodsCount));
        Raise(nameof(GoodsPrice));
    }

    /// <inheritdoc />
    public override string Header => "Wi-Fi DQVC";

    protected override void OnRefresh()
    {
        var wifi = Data.WifiShopData;
        Message = wifi.Message;
        ExpiresYear = wifi.ExpiresYear.ToString();
        ExpiresMonth = wifi.ExpiresMonth.ToString();
        ExpiresDay = wifi.ExpiresDay.ToString();
        ExpiresHour = wifi.ExpiresHour.ToString();
        NextConnectYear = wifi.AllowNextConnectYear.ToString();
        NextConnectMonth = wifi.AllowNextConnectMonth.ToString();
        NextConnectDay = wifi.AllowNextConnectDay.ToString();
        NextConnectHour = wifi.AllowNextConnectHour.ToString();
        MessageExpiresYear = wifi.MessageExpiresYear.ToString();
        MessageExpiresMonth = wifi.MessageExpiresMonth.ToString();
        MessageExpiresDay = wifi.MessageExpiresDay.ToString();
        MessageExpiresHour = wifi.MessageExpiresHour.ToString();
        LoadGoodsDetail();
    }
}
