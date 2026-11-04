using System;
using System.Collections.ObjectModel;
using System.Linq;
using DQ9_Cheat.DataManager;
using DQ9_Cheat.GameData;

namespace DQ9.Avalonia.ViewModels.Screens;

/// <summary>
/// General data and player profile (WinForms BasisDataControl parity).
/// Editable: money, bank, medals, play times, battle statistics, save place,
/// profile block and gestures. Counts are plain engine writes; undo/redo comes
/// from the engine's DataValue layer.
/// <para>UX-01: numeric setters validate the text and report parse failures
/// through INotifyDataErrorInfo (red field border + status-bar warning)
/// instead of dropping the input silently.</para>
/// </summary>
public sealed class GeneralScreenViewModel : EditorScreenViewModel
{
    private string _money = "0";
    private string _bankMoney = "0";
    private string _medal = "0";
    private string _playHour = "0";
    private string _playMinute = "0";
    private string _playSecond = "0";
    private string _multiHour = "0";
    private string _multiMinute = "0";
    private string _multiSecond = "0";
    private string _battleCount = "0";
    private string _loseCount = "0";
    private string _escapeCount = "0";
    private string _escapeSuccessCount = "0";
    private string _victoryCount = "0";
    private string _alchemyCount = "0";
    private SavePlace? _selectedSavePlace;
    private string _handleDisplay = "";
    private string _profileMessage = "";
    private string _birthYear = "0";
    private string _birthMonth = "1";
    private string _birthDay = "1";
    private bool _profileSetting;
    private byte _profileSex;
    private bool _profileSecretAge;
    private bool _profileExplanation;
    private byte _profileTone;
    private ProfileAddress? _selectedAddress;
    private ProfileJob? _selectedHandle;

    private const string UintHint = "a whole number between 0 and 4 294 967 295";
    private const string UshortHint = "a whole number between 0 and 65 535";
    private const string ByteHint = "a whole number between 0 and 255";

    public GeneralScreenViewModel()
    {
        var savePlaces = SavePlaceList.GetList(SavePlaceType.All, "").ToList();
        SavePlaces = new ObservableCollection<SavePlace>(savePlaces);
        var addresses = ProfileAddressList.DQAreaList
            .Concat(ProfileAddressList.PrefectureList)
            .Concat(ProfileAddressList.OtherAreaList)
            .ToList();
        Addresses = new ObservableCollection<ProfileAddress>(addresses);
        Handles = new ObservableCollection<ProfileJob>(ProfileJobList.List);
        Gestures = new ObservableCollection<Gesture>(GestureList.List);
        ToneOptions = new ObservableCollection<byte>(Enumerable.Range(0, 8).Select(i => (byte)i));
        // Gesture direction pairs (WinForms: the seven combo boxes of the
        // groupBox_Gesture) plus the learned party tricks checklist.
        GestureDirections = new ObservableCollection<GestureRow>
        {
            new("Up", () => Basis.GestureUp, v => Basis.GestureUp = v, Gestures),
            new("Left", () => Basis.GestureLeft, v => Basis.GestureLeft = v, Gestures),
            new("Right", () => Basis.GestureRight, v => Basis.GestureRight = v, Gestures),
            new("Down 1", () => Basis.GestureDown1, v => Basis.GestureDown1 = v, Gestures),
            new("Down 2", () => Basis.GestureDown2, v => Basis.GestureDown2 = v, Gestures),
            new("Down 3", () => Basis.GestureDown3, v => Basis.GestureDown3 = v, Gestures),
            new("Down 4", () => Basis.GestureDown4, v => Basis.GestureDown4 = v, Gestures),
        };
        GestureLearn = new ObservableCollection<CheckRow>(
            Enumerable.Range(0, GestureList.List.Count)
                .Select(i => new CheckRow(GestureList.List[i].Name,
                    () => Data.BasisData.IsGestureLearn(i),
                    v => Data.BasisData.SetGestureLearn(i, v))));
    }

    public ObservableCollection<SavePlace> SavePlaces { get; }
    public ObservableCollection<ProfileAddress> Addresses { get; }
    public ObservableCollection<ProfileJob> Handles { get; }
    public ObservableCollection<Gesture> Gestures { get; }

    /// <summary>UX-20: coded values become choices (0 = male, 1 = female).</summary>
    public ObservableCollection<byte> ToneOptions { get; }

    /// <summary>The seven gesture direction pairs (up / left / right / down 1-4).</summary>
    public ObservableCollection<GestureRow> GestureDirections { get; }

    public ObservableCollection<CheckRow> GestureLearn { get; }

    /// <summary>Display of the current handle (job name or sex-dependent title).</summary>
    public string HandleDisplay
    {
        get => _handleDisplay;
        private set => Set(ref _handleDisplay, value);
    }

    /// <summary>Builds the searchable handle picker (occupations + accolades)
    /// of the WinForms HandleSelectWindow, filtered by the profile sex.</summary>
    public SearchPickerViewModel CreateHandlePicker()
    {
        var picker = SearchPickerViewModel.ForHandle(Basis.ProfileSex);
        picker.Rebuild();
        return picker;
    }

    /// <summary>Writes the handle picked in the modal (a ProfileJob or a
    /// TitleElement) through the engine.</summary>
    public void ApplyHandle(object? value)
    {
        if (!EngineGuard.Ready || value == null)
            return;
        if (value is ProfileJob job)
            Basis.Handle = new Handle(job);
        else if (value is TitleElement title)
            Basis.Handle = new Handle(title);
    }

    private void ReloadHandleDisplay()
    {
        var handle = Basis.Handle;
        if (handle == null)
        {
            HandleDisplay = string.Empty;
            return;
        }
        if (handle.Job != null)
            HandleDisplay = handle.Job.Name;
        else if (handle.Title != null)
            HandleDisplay = Basis.ProfileSex == 0
                ? handle.Title.MaleTitleName
                : handle.Title.LadyTitleName;
        else
            HandleDisplay = string.Empty;
    }

    private BasisData Basis => Data.BasisData;

    public string Money
    {
        get => _money;
        set
        {
            if (!Set(ref _money, value) || IsRefreshing)
                return;
            if (!ValidateParse<uint>(nameof(Money), "Money", value, uint.TryParse, UintHint, out var v))
                return;
            if (EngineGuard.Ready)
                Basis.Money.Value = v;
        }
    }

    public string BankMoney
    {
        get => _bankMoney;
        set
        {
            if (!Set(ref _bankMoney, value) || IsRefreshing)
                return;
            if (!ValidateParse<uint>(nameof(BankMoney), "Bank money", value, uint.TryParse, UintHint, out var v))
                return;
            if (EngineGuard.Ready)
                Basis.BankMoney.Value = v;
        }
    }

    public string Medal
    {
        get => _medal;
        set
        {
            if (!Set(ref _medal, value) || IsRefreshing)
                return;
            if (!ValidateParse<uint>(nameof(Medal), "Medals", value, uint.TryParse, UintHint, out var v))
                return;
            if (EngineGuard.Ready)
                Basis.Medal.Value = v;
        }
    }

    public string PlayHour
    {
        get => _playHour;
        set
        {
            if (!Set(ref _playHour, value) || IsRefreshing)
                return;
            if (!ValidateParse<ushort>(nameof(PlayHour), "Play time (hours)", value, ushort.TryParse, UshortHint, out var v))
                return;
            if (EngineGuard.Ready)
                Basis.PlayTimeHour.Value = v;
        }
    }

    public string PlayMinute
    {
        get => _playMinute;
        set
        {
            if (!Set(ref _playMinute, value) || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(PlayMinute), "Play time (minutes)", value, byte.TryParse, ByteHint, out var v))
                return;
            if (EngineGuard.Ready)
                Basis.PlayTimeMinute.Value = v;
        }
    }

    public string PlaySecond
    {
        get => _playSecond;
        set
        {
            if (!Set(ref _playSecond, value) || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(PlaySecond), "Play time (seconds)", value, byte.TryParse, ByteHint, out var v))
                return;
            if (EngineGuard.Ready)
                Basis.PlayTimeSecond.Value = v;
        }
    }

    public string MultiHour
    {
        get => _multiHour;
        set
        {
            if (!Set(ref _multiHour, value) || IsRefreshing)
                return;
            if (!ValidateParse<ushort>(nameof(MultiHour), "Multiplayer time (hours)", value, ushort.TryParse, UshortHint, out var v))
                return;
            if (EngineGuard.Ready)
                Basis.MultiPlayTimeHour.Value = v;
        }
    }

    public string MultiMinute
    {
        get => _multiMinute;
        set
        {
            if (!Set(ref _multiMinute, value) || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(MultiMinute), "Multiplayer time (minutes)", value, byte.TryParse, ByteHint, out var v))
                return;
            if (EngineGuard.Ready)
                Basis.MultiPlayTimeMinute.Value = v;
        }
    }

    public string MultiSecond
    {
        get => _multiSecond;
        set
        {
            if (!Set(ref _multiSecond, value) || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(MultiSecond), "Multiplayer time (seconds)", value, byte.TryParse, ByteHint, out var v))
                return;
            if (EngineGuard.Ready)
                Basis.MultiPlayTimeSecond.Value = v;
        }
    }

    public string BattleCount
    {
        get => _battleCount;
        set
        {
            if (!Set(ref _battleCount, value) || IsRefreshing)
                return;
            if (!ValidateParse<uint>(nameof(BattleCount), "Battles", value, uint.TryParse, UintHint, out var v))
                return;
            if (EngineGuard.Ready)
                Basis.BattleCount = v;
        }
    }

    public string LoseCount
    {
        get => _loseCount;
        set
        {
            if (!Set(ref _loseCount, value) || IsRefreshing)
                return;
            if (!ValidateParse<uint>(nameof(LoseCount), "Losses", value, uint.TryParse, UintHint, out var v))
                return;
            if (EngineGuard.Ready)
                Basis.LoseCount = v;
        }
    }

    public string EscapeCount
    {
        get => _escapeCount;
        set
        {
            if (!Set(ref _escapeCount, value) || IsRefreshing)
                return;
            if (!ValidateParse<uint>(nameof(EscapeCount), "Escapes", value, uint.TryParse, UintHint, out var v))
                return;
            if (EngineGuard.Ready)
                Basis.EscapeCount = v;
        }
    }

    public string EscapeSuccessCount
    {
        get => _escapeSuccessCount;
        set
        {
            if (!Set(ref _escapeSuccessCount, value) || IsRefreshing)
                return;
            if (!ValidateParse<uint>(nameof(EscapeSuccessCount), "Successful escapes", value, uint.TryParse, UintHint, out var v))
                return;
            if (EngineGuard.Ready)
                Basis.EscapeSuccessCount = v;
        }
    }

    public string VictoryCount
    {
        get => _victoryCount;
        set
        {
            if (!Set(ref _victoryCount, value) || IsRefreshing)
                return;
            if (!ValidateParse<uint>(nameof(VictoryCount), "Victories", value, uint.TryParse, UintHint, out var v))
                return;
            if (EngineGuard.Ready)
                Basis.VictoryCount = v;
        }
    }

    public string AlchemyCount
    {
        get => _alchemyCount;
        set
        {
            if (!Set(ref _alchemyCount, value) || IsRefreshing)
                return;
            if (!ValidateParse<int>(nameof(AlchemyCount), "Alchemy count", value, int.TryParse,
                    "a whole number", out var v))
                return;
            if (EngineGuard.Ready)
                Basis.AlchemyCount.Value = v;
        }
    }

    public SavePlace? SelectedSavePlace
    {
        get => _selectedSavePlace;
        set { if (Set(ref _selectedSavePlace, value) && value != null && !IsRefreshing && EngineGuard.Ready) Basis.SavePlace = value; }
    }

    public string ProfileMessage
    {
        get => _profileMessage;
        set
        {
            if (!Set(ref _profileMessage, value) || IsRefreshing)
                return;
            if (EngineGuard.Ready && value.Length > Basis.ProfileMessage.MaxLength)
            {
                SetFieldError(nameof(ProfileMessage),
                    $"Profile message is limited to {Basis.ProfileMessage.MaxLength} characters.");
                return;
            }
            ClearFieldError(nameof(ProfileMessage));
            if (EngineGuard.Ready)
                Basis.ProfileMessage.Value = value;
        }
    }

    public string BirthYear
    {
        get => _birthYear;
        set
        {
            if (!Set(ref _birthYear, value) || IsRefreshing)
                return;
            if (!ValidateParse<ushort>(nameof(BirthYear), "Birthday (year)", value, ushort.TryParse, UshortHint, out var v))
                return;
            if (EngineGuard.Ready)
                Basis.ProfileBirthYear = v;
        }
    }

    public string BirthMonth
    {
        get => _birthMonth;
        set
        {
            if (!Set(ref _birthMonth, value) || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(BirthMonth), "Birthday (month)", value, byte.TryParse, ByteHint, out var v))
                return;
            if (EngineGuard.Ready)
                Basis.ProfileBirthMonth = v;
        }
    }

    public string BirthDay
    {
        get => _birthDay;
        set
        {
            if (!Set(ref _birthDay, value) || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(BirthDay), "Birthday (day)", value, byte.TryParse, ByteHint, out var v))
                return;
            if (EngineGuard.Ready)
                Basis.ProfileBirthDay = v;
        }
    }

    public bool ProfileSetting
    {
        get => _profileSetting;
        set { if (Set(ref _profileSetting, value) && !IsRefreshing && EngineGuard.Ready) Basis.ProfileSetting = value; }
    }

    /// <summary>0 = male, 1 = female (combo-bound; UX-20).</summary>
    public byte ProfileSex
    {
        get => _profileSex;
        set { if (Set(ref _profileSex, value) && !IsRefreshing && EngineGuard.Ready) Basis.ProfileSex = value; }
    }

    public bool ProfileSecretAge
    {
        get => _profileSecretAge;
        set { if (Set(ref _profileSecretAge, value) && !IsRefreshing && EngineGuard.Ready) Basis.ProfileSecretAge = value; }
    }

    public bool ProfileExplanation
    {
        get => _profileExplanation;
        set { if (Set(ref _profileExplanation, value) && !IsRefreshing && EngineGuard.Ready) Basis.ProfileExplanation = value; }
    }

    /// <summary>Profile speech tone 0–7 (combo-bound; UX-20).</summary>
    public byte ProfileTone
    {
        get => _profileTone;
        set { if (Set(ref _profileTone, value) && !IsRefreshing && EngineGuard.Ready) Basis.ProfileTone = value; }
    }

    public ProfileAddress? SelectedAddress
    {
        get => _selectedAddress;
        set { if (Set(ref _selectedAddress, value) && value != null && !IsRefreshing && EngineGuard.Ready) Basis.Address = value; }
    }

    public ProfileJob? SelectedHandle
    {
        get => _selectedHandle;
        set { if (Set(ref _selectedHandle, value) && value != null && !IsRefreshing && EngineGuard.Ready) Basis.Handle = new Handle(value); }
    }

    /// <inheritdoc />
    public override string Header => "General";

    protected override void OnRefresh()
    {
        Money = Basis.Money.Value.ToString();
        BankMoney = Basis.BankMoney.Value.ToString();
        Medal = Basis.Medal.Value.ToString();
        PlayHour = Basis.PlayTimeHour.Value.ToString();
        PlayMinute = Basis.PlayTimeMinute.Value.ToString();
        PlaySecond = Basis.PlayTimeSecond.Value.ToString();
        MultiHour = Basis.MultiPlayTimeHour.Value.ToString();
        MultiMinute = Basis.MultiPlayTimeMinute.Value.ToString();
        MultiSecond = Basis.MultiPlayTimeSecond.Value.ToString();
        BattleCount = Basis.BattleCount.ToString();
        LoseCount = Basis.LoseCount.ToString();
        EscapeCount = Basis.EscapeCount.ToString();
        EscapeSuccessCount = Basis.EscapeSuccessCount.ToString();
        VictoryCount = Basis.VictoryCount.ToString();
        AlchemyCount = Basis.AlchemyCount.Value.ToString();
        var currentPlace = Basis.SavePlace;
        SelectedSavePlace = currentPlace != null
            ? SavePlaces.FirstOrDefault(p => p.Index == currentPlace.Index) ?? SavePlaces.FirstOrDefault()
            : SavePlaces.FirstOrDefault();
        ProfileMessage = Basis.ProfileMessage.Value;
        BirthYear = Basis.ProfileBirthYear.ToString();
        BirthMonth = Basis.ProfileBirthMonth.ToString();
        BirthDay = Basis.ProfileBirthDay.ToString();
        ProfileSetting = Basis.ProfileSetting;
        ProfileSex = Basis.ProfileSex;
        ProfileSecretAge = Basis.ProfileSecretAge;
        ProfileExplanation = Basis.ProfileExplanation;
        ProfileTone = Basis.ProfileTone;
        var currentAddress = Basis.Address;
        SelectedAddress = currentAddress != null
            ? Addresses.FirstOrDefault(a => a.Index == currentAddress.Index) ?? Addresses.FirstOrDefault()
            : Addresses.FirstOrDefault();
        var handleJob = Basis.Handle?.Job;
        SelectedHandle = handleJob != null
            ? Handles.FirstOrDefault(j => j.Index == handleJob.Index) ?? Handles.FirstOrDefault()
            : Handles.FirstOrDefault();
        ReloadHandleDisplay();
        foreach (var row in GestureDirections)
            row.Reload();
        foreach (var row in GestureLearn)
            row.Reload();
    }
}
