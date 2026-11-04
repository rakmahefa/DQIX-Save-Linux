// Decompiled with JetBrains decompiler
// Type: DQ9_Cheat.DataManager.FirstClearData
// Assembly: DQ9_Cheat, Version=0.7.0.57, Culture=neutral, PublicKeyToken=null
// MVID: 9E5BE672-CBE6-45FB-AC35-96531044560E
// Assembly location: dq9_save_editor_0.7\DQCheat.Patched.0.7.exe

using DQ9_Cheat.GameData;

namespace DQ9_Cheat.DataManager;

internal class FirstClearData
{
    private readonly DataValue<ushort> _title;
    private readonly DataValue<ushort> _titleCount;
    private readonly DataValue<ushort> _treasureMap;
    private readonly DataValue<ushort> _visitorCount;

    public FirstClearData(SaveData owner)
    {
        PlayTimeHour = new DataValue<ushort>(owner, 16168U, 0, ushort.MaxValue);
        PlayTimeMinute = new DataValue<byte>(owner, 16170U, 0, 59);
        PlayTimeSecond = new DataValue<byte>(owner, 16171U, 0, 59);
        MultiPlayTimeHour = new DataValue<ushort>(owner, 16172U, 0, ushort.MaxValue);
        MultiPlayTimeMinute = new DataValue<byte>(owner, 16174U, 0, 59);
        MultiPlayTimeSecond = new DataValue<byte>(owner, 16175U, 0, 59);
        VictoryCount = new DataValue<ushort>(owner, 16176U, 0, ushort.MaxValue);
        AlchemyCount = new DataValue<ushort>(owner, 16180U, 0, ushort.MaxValue);
        _titleCount = new DataValue<ushort>(owner, 16184U, 0, ushort.MaxValue);
        QuestCount = new DataValue<byte>(owner, 16188U, 0, byte.MaxValue);
        _visitorCount = new DataValue<ushort>(owner, 16189U, 0, ushort.MaxValue);
        _treasureMap = new DataValue<ushort>(owner, 16185U, 0, ushort.MaxValue);
        _title = new DataValue<ushort>(owner, 16186U, 0, ushort.MaxValue);
    }

    public DataValue<ushort> PlayTimeHour { get; }

    public DataValue<byte> PlayTimeMinute { get; }

    public DataValue<byte> PlayTimeSecond { get; }

    public DataValue<ushort> MultiPlayTimeHour { get; }

    public DataValue<byte> MultiPlayTimeMinute { get; }

    public DataValue<byte> MultiPlayTimeSecond { get; }

    public DataValue<ushort> VictoryCount { get; }

    public DataValue<ushort> AlchemyCount { get; }

    public ushort TitleCount
    {
        get => (ushort)(_titleCount.Value & 511U);
        set => _titleCount.Value = (ushort)((_titleCount.Value & 65024) | (value & 511));
    }

    public DataValue<byte> QuestCount { get; }

    public ushort VisitorCount
    {
        get => (ushort)(_visitorCount.Value & 16383U);
        set => _visitorCount.Value = (ushort)((_visitorCount.Value & 49152) | (value & 16383));
    }

    public ushort TreasureMap
    {
        get => (ushort)((_treasureMap.Value & 32766) >> 1);
        set => _treasureMap.Value = (ushort)((_treasureMap.Value & 32769) | (value << 1));
    }

    public TitleElement Title
    {
        get => TitleDataList.GetTitleElement((_title.Value & 65408) >> 7);
        set => _title.Value = (ushort)((_title.Value & sbyte.MaxValue) | ((value.DataIndex << 7) & 65408));
    }
}