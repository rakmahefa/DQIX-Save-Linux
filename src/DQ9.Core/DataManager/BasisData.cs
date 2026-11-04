// Decompiled with JetBrains decompiler
// Type: DQ9_Cheat.DataManager.BasisData
// Assembly: DQ9_Cheat, Version=0.7.0.57, Culture=neutral, PublicKeyToken=null
// MVID: 9E5BE672-CBE6-45FB-AC35-96531044560E
// Assembly location: dq9_save_editor_0.7\DQCheat.Patched.0.7.exe

using DQ9_Cheat.GameData;

namespace DQ9_Cheat.DataManager;

internal class BasisData
{
    private readonly DataValue<ushort> _address;
    private readonly DataValue<byte> _afterEnding;
    private readonly DataValue<uint> _battleCount;
    private readonly DataValue<uint> _escapeCount;
    private readonly DataValue<uint> _escapeSuccessCount;
    private readonly DataValue<byte> _gestureDown1;
    private readonly DataValue<byte> _gestureDown2;
    private readonly DataValue<byte> _gestureDown3;
    private readonly DataValue<byte> _gestureDown4;
    private readonly DataValue<byte> _gestureLearn1;
    private readonly DataValue<byte> _gestureLearn2;
    private readonly DataValue<byte> _gestureLearn3;
    private readonly DataValue<byte> _gestureLeft;
    private readonly DataValue<byte> _gestureRight;
    private readonly DataValue<byte> _gestureUp;
    private readonly DataValue<ushort> _handle;
    private readonly DataValue<ushort> _intermissionPlace;
    private readonly DataValue<uint> _loseCount;
    private readonly DataValue<byte> _profileBirthDay;
    private readonly DataValue<ushort> _profileBirthYearMonth;
    private readonly DataValue<byte> _profileExplanation;
    private readonly DataValue<byte> _profileSecretAge;
    private readonly DataValue<byte> _profileSetting_Sex;
    private readonly DataValue<ushort> _profileTone;
    private readonly DataValue<ushort> _savePlace;
    private readonly DataValue<uint> _victoryCount;

    public BasisData(SaveData owner)
    {
        _intermissionPlace = new DataValue<ushort>(owner, 27104U, 0, ushort.MaxValue);
        IntermissionX = new DataValue<int>(owner, 27108U, int.MinValue, int.MaxValue);
        IntermissionY = new DataValue<int>(owner, 27112U, int.MinValue, int.MaxValue);
        IntermissionZ = new DataValue<int>(owner, 27116U, int.MinValue, int.MaxValue);
        _afterEnding = new DataValue<byte>(owner, 12276U, 0, byte.MaxValue);
        _savePlace = new DataValue<ushort>(owner, 11476U, 0, ushort.MaxValue);
        _profileExplanation = new DataValue<byte>(owner, 26987U, 0, byte.MaxValue);
        _profileSetting_Sex = new DataValue<byte>(owner, 26983U, 0, byte.MaxValue);
        _profileTone = new DataValue<ushort>(owner, 26982U, 0, ushort.MaxValue);
        _profileBirthYearMonth = new DataValue<ushort>(owner, 26980U, 0, ushort.MaxValue);
        _profileBirthDay = new DataValue<byte>(owner, 26982U, 0, byte.MaxValue);
        _address = new DataValue<ushort>(owner, 26985U, 0, ushort.MaxValue);
        _handle = new DataValue<ushort>(owner, 26986U, 0, ushort.MaxValue);
        var fillData = new byte[2] { 129, 69 };
        ProfileMessage = new DataValueString(owner, 26988U, 114, false, fillData);
        _profileSecretAge = new DataValue<byte>(owner, 26983U, 0, byte.MaxValue);
        PlayTimeHour = new DataValue<ushort>(owner, 16024U, 0, ushort.MaxValue);
        PlayTimeMinute = new DataValue<byte>(owner, 16026U, 0, 59);
        PlayTimeSecond = new DataValue<byte>(owner, 16027U, 0, 59);
        MultiPlayTimeHour = new DataValue<ushort>(owner, 16028U, 0, ushort.MaxValue);
        MultiPlayTimeMinute = new DataValue<byte>(owner, 16030U, 0, 59);
        MultiPlayTimeSecond = new DataValue<byte>(owner, 16031U, 0, 59);
        Money = new DataValue<uint>(owner, 11448U, 0U, 9999999U);
        BankMoney = new DataValue<uint>(owner, 11452U, 0U, 1000000000U);
        Medal = new DataValue<uint>(owner, 11460U, 0U, 500U);
        _battleCount = new DataValue<uint>(owner, 16084U, 0U, uint.MaxValue);
        _loseCount = new DataValue<uint>(owner, 16088U, 0U, uint.MaxValue);
        _escapeCount = new DataValue<uint>(owner, 16076U, 0U, uint.MaxValue);
        _escapeSuccessCount = new DataValue<uint>(owner, 16080U, 0U, uint.MaxValue);
        _victoryCount = new DataValue<uint>(owner, 16032U, 0U, uint.MaxValue);
        AlchemyCount = new DataValue<int>(owner, 15016U, 0, int.MaxValue);
        _gestureLearn1 = new DataValue<byte>(owner, 12108U, 0, byte.MaxValue);
        _gestureLearn2 = new DataValue<byte>(owner, 12109U, 0, byte.MaxValue);
        _gestureLearn3 = new DataValue<byte>(owner, 12110U, 0, byte.MaxValue);
        _gestureUp = new DataValue<byte>(owner, 11468U, 0, byte.MaxValue);
        _gestureLeft = new DataValue<byte>(owner, 11469U, 0, byte.MaxValue);
        _gestureRight = new DataValue<byte>(owner, 11470U, 0, byte.MaxValue);
        _gestureDown1 = new DataValue<byte>(owner, 11471U, 0, byte.MaxValue);
        _gestureDown2 = new DataValue<byte>(owner, 11472U, 0, byte.MaxValue);
        _gestureDown3 = new DataValue<byte>(owner, 11473U, 0, byte.MaxValue);
        _gestureDown4 = new DataValue<byte>(owner, 11474U, 0, byte.MaxValue);
    }

    public SavePlace SavePlace
    {
        get => (_afterEnding.Value & 4) == 0
            ? SavePlaceList.GetSavePlace(_savePlace.Value)
            : SavePlaceList.GetSavePlace(ushort.MaxValue);
        set
        {
            if (value.Index == ushort.MaxValue)
            {
                _afterEnding.Value = (byte)((_afterEnding.Value & 251) | 4);
                _savePlace.Value = 109;
            }
            else
            {
                _afterEnding.Value &= 251;
                _savePlace.Value = value.Index;
            }
        }
    }

    public bool ProfileExplanation
    {
        get => (_profileExplanation.Value & 64) != 0;
        set => _profileExplanation.Value = (byte)((_profileExplanation.Value & 191) | (value ? 64 : 0));
    }

    public byte ProfileSex
    {
        get => (byte)((_profileSetting_Sex.Value & 2) >> 1);
        set => _profileSetting_Sex.Value = (byte)((_profileSetting_Sex.Value & 253) | (value != 0 ? 2 : 0));
    }

    public bool ProfileSetting
    {
        get => (_profileSetting_Sex.Value & 4) != 0;
        set => _profileSetting_Sex.Value = (byte)((_profileSetting_Sex.Value & 251) | (value ? 4 : 0));
    }

    public byte ProfileTone
    {
        get => (byte)((_profileTone.Value & 480) >> 5);
        set => _profileTone.Value = (ushort)((_profileTone.Value & 65055) | ((value << 5) & 480));
    }

    public bool ProfileToneSetting
    {
        get => (_profileTone.Value & 2048) != 0;
        set => _profileTone.Value = (ushort)((_profileTone.Value & 63487) | (value ? 2048 : 0));
    }

    public ushort ProfileBirthYear
    {
        get => (ushort)(_profileBirthYearMonth.Value & 4095U);
        set => _profileBirthYearMonth.Value = (ushort)((value & 4095) | (ProfileBirthMonth << 12));
    }

    public byte ProfileBirthMonth
    {
        get => (byte)((uint)_profileBirthYearMonth.Value >> 12);
        set => _profileBirthYearMonth.Value = (ushort)((ProfileBirthYear & 4095) | (value << 12));
    }

    public byte ProfileBirthDay
    {
        get => (byte)(_profileBirthDay.Value & 31U);
        set => _profileBirthDay.Value = (byte)((_profileBirthDay.Value & 224) | (value & 31));
    }

    public bool ProfileSecretAge
    {
        get => (_profileSecretAge.Value & 128) == 0;
        set => _profileSecretAge.Value =
            (byte)((byte)(_profileSecretAge.Value & (uint)sbyte.MaxValue) | (value ? 0 : 128));
    }

    public Handle Handle
    {
        get
        {
            var num = (ushort)((_handle.Value & 16376) >> 3);
            var jobFromIndex = ProfileJobList.GetJobFromIndex(num);
            if (jobFromIndex != null)
                return new Handle(jobFromIndex);
            var titleElement = TitleDataList.GetTitleElement(num);
            return titleElement != null ? new Handle(titleElement) : null;
        }
        set
        {
            if (value == null)
                return;
            ushort num;
            if (value.HandleType == HandleType.HandleJob && value.Job != null)
            {
                num = (ushort)((value.Job.Index << 3) & 16376);
            }
            else
            {
                if (value.HandleType != HandleType.HandleTitle || value.Title == null)
                    return;
                num = (ushort)((value.Title.DataIndex << 3) & 16376);
            }

            _handle.Value = (ushort)((_handle.Value & 49159U) | num);
        }
    }

    public ProfileAddress Address
    {
        get => ProfileAddressList.GetAddressFromIndex((ushort)((_address.Value & 2046) >> 1));
        set => _address.Value = (ushort)((_address.Value & 63489) | ((value.Index << 1) & 2046));
    }

    public DataValueString ProfileMessage { get; }

    public DataValue<ushort> PlayTimeHour { get; }

    public DataValue<byte> PlayTimeMinute { get; }

    public DataValue<byte> PlayTimeSecond { get; }

    public DataValue<ushort> MultiPlayTimeHour { get; }

    public DataValue<byte> MultiPlayTimeMinute { get; }

    public DataValue<byte> MultiPlayTimeSecond { get; }

    public DataValue<uint> Money { get; }

    public DataValue<uint> BankMoney { get; }

    public DataValue<uint> Medal { get; }

    public uint BattleCount
    {
        get => _battleCount.Value & 16777215U;
        set => _battleCount.Value = (uint)(((int)_battleCount.Value & -16777216) | ((int)value & 16777215));
    }

    public uint LoseCount
    {
        get => _loseCount.Value & 16777215U;
        set => _loseCount.Value = (uint)(((int)_loseCount.Value & -16777216) | ((int)value & 16777215));
    }

    public uint EscapeCount
    {
        get => _escapeCount.Value & 16777215U;
        set => _escapeCount.Value = (uint)(((int)_escapeCount.Value & -16777216) | ((int)value & 16777215));
    }

    public uint EscapeSuccessCount
    {
        get => _escapeSuccessCount.Value & 16777215U;
        set => _escapeSuccessCount.Value =
            (uint)(((int)_escapeSuccessCount.Value & -16777216) | ((int)value & 16777215));
    }

    public uint VictoryCount
    {
        get => _victoryCount.Value & 16777215U;
        set => _victoryCount.Value = (uint)(((int)_victoryCount.Value & -16777216) | ((int)value & 16777215));
    }

    public DataValue<int> AlchemyCount { get; }

    public byte GestureUp
    {
        get => _gestureUp.Value != byte.MaxValue ? _gestureUp.Value : (byte)0;
        set => _gestureUp.Value = value >= GestureList.List.Count ? byte.MaxValue : value;
    }

    public byte GestureLeft
    {
        get => _gestureLeft.Value != byte.MaxValue ? _gestureLeft.Value : (byte)0;
        set => _gestureLeft.Value = value >= GestureList.List.Count ? byte.MaxValue : value;
    }

    public byte GestureRight
    {
        get => _gestureRight.Value != byte.MaxValue ? _gestureRight.Value : (byte)0;
        set => _gestureRight.Value = value >= GestureList.List.Count ? byte.MaxValue : value;
    }

    public byte GestureDown1
    {
        get => _gestureDown1.Value != byte.MaxValue ? _gestureDown1.Value : (byte)0;
        set => _gestureDown1.Value = value >= GestureList.List.Count ? byte.MaxValue : value;
    }

    public byte GestureDown2
    {
        get => _gestureDown2.Value != byte.MaxValue ? _gestureDown2.Value : (byte)0;
        set => _gestureDown2.Value = value >= GestureList.List.Count ? byte.MaxValue : value;
    }

    public byte GestureDown3
    {
        get => _gestureDown3.Value != byte.MaxValue ? _gestureDown3.Value : (byte)0;
        set => _gestureDown3.Value = value >= GestureList.List.Count ? byte.MaxValue : value;
    }

    public byte GestureDown4
    {
        get => _gestureDown4.Value != byte.MaxValue ? _gestureDown4.Value : (byte)0;
        set => _gestureDown4.Value = value >= GestureList.List.Count ? byte.MaxValue : value;
    }

    public SavePlace IntermissionPlace
    {
        get => SavePlaceList.GetSavePlace(_intermissionPlace.Value);
        set
        {
            if (value.Index == ushort.MaxValue)
                _intermissionPlace.Value = 100;
            else
                _intermissionPlace.Value = value.Index;
        }
    }

    public DataValue<int> IntermissionX { get; }

    public DataValue<int> IntermissionY { get; }

    public DataValue<int> IntermissionZ { get; }

    public bool IsGestureLearn(int index)
    {
        if (index < 6)
            return (_gestureLearn1.Value & (1 << (index + 2))) != 0;
        return index < 14 ? (_gestureLearn2.Value & (1 << (index - 6))) != 0 : (_gestureLearn3.Value & 1) != 0;
    }

    public void SetGestureLearn(int index, bool value)
    {
        if (index < 6)
            _gestureLearn1.Value =
                (byte)((_gestureLearn1.Value & ~(1 << (index + 2))) | (value ? 1 << (index + 2) : 0));
        else if (index < 14)
            _gestureLearn2.Value =
                (byte)((_gestureLearn2.Value & ~(1 << (index - 6))) | (value ? 1 << (index - 6) : 0));
        else
            _gestureLearn3.Value = (byte)((_gestureLearn3.Value & 254) | (value ? 1 : 0));
    }
}