using DQ9_Cheat.DataManager;

namespace DQ9_Cheat;

/// <summary>
/// Applies the NumericUpDown ranges of the legacy screens.
///
/// This configuration used to run inside the DQ9_Cheat.DataManager
/// constructors (BasisData, FirstClearData, CharacterData, RikkaData) which
/// reached back into MainWindow.Instance. When the data layer moved to the
/// portable DQ9.Core assembly, all UI references were removed from it and the
/// configuration was transplanted here, called once from
/// <see cref="MainWindow.Initialize"/> right after the controls are created.
///
/// Values are the exact limits the legacy code set (mostly DataValue Min/Max).
/// </summary>
internal static class UiRangeConfig
{
    public static void Apply()
    {
        ApplyBasisDataRanges();
        ApplyFirstClearDataRanges();
        ApplyCharacterDataRanges();
        ApplyRikkaDataRanges();
    }

    private static void ApplyBasisDataRanges()
    {
        var basisDataControl = MainWindow.Instance.BasisDataControl;
        basisDataControl.BeginUpdate();
        basisDataControl.NumericUpDown_ProfileBirthYear.Maximum = 4096M;
        basisDataControl.NumericUpDown_ProfileBirthYear.Minimum = 0M;
        basisDataControl.NumericUpDown_ProfileBirthMonth.Maximum = 12M;
        basisDataControl.NumericUpDown_ProfileBirthMonth.Minimum = 1M;
        basisDataControl.NumericUpDown_ProfileBirthDay.Maximum = 31M;
        basisDataControl.NumericUpDown_ProfileBirthDay.Minimum = 1M;
        // PlayTimeHour  : DataValue<ushort> 0..65535
        basisDataControl.NumericUpDown_Hour.Maximum = 65535M;
        basisDataControl.NumericUpDown_Hour.Minimum = 0M;
        // PlayTimeMinute / Second : DataValue<byte> 0..59
        basisDataControl.NumericUpDown_Minute.Maximum = 59M;
        basisDataControl.NumericUpDown_Minute.Minimum = 0M;
        basisDataControl.NumericUpDown_Second.Maximum = 59M;
        basisDataControl.NumericUpDown_Second.Minimum = 0M;
        basisDataControl.NumericUpDown_MultiPlayTimeHour.Maximum = 65535M;
        basisDataControl.NumericUpDown_MultiPlayTimeHour.Minimum = 0M;
        basisDataControl.NumericUpDown_MultiPlayTimeMinute.Maximum = 59M;
        basisDataControl.NumericUpDown_MultiPlayTimeMinute.Minimum = 0M;
        basisDataControl.NumericUpDown_MultiPlayTimeSecond.Maximum = 59M;
        basisDataControl.NumericUpDown_MultiPlayTimeSecond.Minimum = 0M;
        basisDataControl.NumericUpDown_Money.Maximum = 9999999M;
        basisDataControl.NumericUpDown_Money.Minimum = 0M;
        basisDataControl.NumericUpDown_BankMoney.Maximum = 1000000000M;
        basisDataControl.NumericUpDown_BankMoney.Minimum = 0M;
        basisDataControl.NumericUpDown_Medal.Maximum = 500M;
        basisDataControl.NumericUpDown_Medal.Minimum = 0M;
        basisDataControl.NumericUpDown_BattleCount.Minimum = 0M;
        basisDataControl.NumericUpDown_BattleCount.Maximum = 16777215M;
        basisDataControl.NumericUpDown_LoseCount.Minimum = 0M;
        basisDataControl.NumericUpDown_LoseCount.Maximum = 16777215M;
        basisDataControl.NumericUpDown_EscapeCount.Minimum = 0M;
        basisDataControl.NumericUpDown_EscapeCount.Maximum = 16777215M;
        basisDataControl.NumericUpDown_EscapeSuccessCount.Minimum = 0M;
        basisDataControl.NumericUpDown_EscapeSuccessCount.Maximum = 16777215M;
        basisDataControl.NumericUpDown_VictoryCount.Minimum = 0M;
        basisDataControl.NumericUpDown_VictoryCount.Maximum = 16777215M;
        basisDataControl.NumericUpDown_AlchemyCount.Minimum = 0M;
        basisDataControl.NumericUpDown_AlchemyCount.Maximum = 2147483647M;
        basisDataControl.EndUpdate();
    }

    private static void ApplyFirstClearDataRanges()
    {
        var clearDataControl = MainWindow.Instance.FirstClearDataControl;
        clearDataControl.BeginUpdate();
        clearDataControl.NumericUpDown_Hour.Maximum = 65535M;
        clearDataControl.NumericUpDown_Hour.Minimum = 0M;
        clearDataControl.NumericUpDown_Minute.Maximum = 59M;
        clearDataControl.NumericUpDown_Minute.Minimum = 0M;
        clearDataControl.NumericUpDown_Second.Maximum = 59M;
        clearDataControl.NumericUpDown_Second.Minimum = 0M;
        clearDataControl.NumericUpDown_MultiPlayTimeHour.Maximum = 65535M;
        clearDataControl.NumericUpDown_MultiPlayTimeHour.Minimum = 0M;
        clearDataControl.NumericUpDown_MultiPlayTimeMinute.Maximum = 59M;
        clearDataControl.NumericUpDown_MultiPlayTimeMinute.Minimum = 0M;
        clearDataControl.NumericUpDown_MultiPlayTimeSecond.Maximum = 59M;
        clearDataControl.NumericUpDown_MultiPlayTimeSecond.Minimum = 0M;
        clearDataControl.NumericUpDown_VictoryCount.Minimum = 0M;
        clearDataControl.NumericUpDown_VictoryCount.Maximum = 65535M;
        clearDataControl.NumericUpDown_AlchemyCount.Minimum = 0M;
        clearDataControl.NumericUpDown_AlchemyCount.Maximum = 65535M;
        clearDataControl.NumericUpDown_TitleCount.Minimum = 0M;
        clearDataControl.NumericUpDown_TitleCount.Maximum = 511M;
        clearDataControl.NumericUpDown_QuestCount.Minimum = 0M;
        clearDataControl.NumericUpDown_QuestCount.Maximum = 255M;
        clearDataControl.NumericUpDown_VisitorCount.Minimum = 0M;
        clearDataControl.NumericUpDown_VisitorCount.Maximum = 16383M;
        clearDataControl.NumericUpDown_TreasureMap.Minimum = 0M;
        clearDataControl.NumericUpDown_TreasureMap.Maximum = 16383M;
        clearDataControl.EndUpdate();
    }

    private static void ApplyCharacterDataRanges()
    {
        var characterDataControl = MainWindow.Instance.CharacterDataControl;
        characterDataControl.BeginUpdate();
        // JobLevel[0]   : DataValue<byte> 0..99
        characterDataControl.NumericUpDown_Level.Minimum = 0M;
        characterDataControl.NumericUpDown_Level.Maximum = 99M;
        // JobExperience : DataValue<uint> 0..999999999
        characterDataControl.NumericUpDown_Experience.Minimum = 0M;
        characterDataControl.NumericUpDown_Experience.Maximum = 999999999M;
        characterDataControl.NumericUpDown_NowHP.Maximum = 999M;
        characterDataControl.NumericUpDown_NowHP.Minimum = 0M;
        characterDataControl.NumericUpDown_NowMP.Maximum = 999M;
        characterDataControl.NumericUpDown_NowMP.Minimum = 0M;
        // FigureWidth / Height : DataValue<ushort> 0..65535
        characterDataControl.NumericUpDown_FigureWidth.Maximum = 65535M;
        characterDataControl.NumericUpDown_FigureWidth.Minimum = 0M;
        characterDataControl.NumericUpDown_FigureHeight.Maximum = 65535M;
        characterDataControl.NumericUpDown_FigureHeight.Minimum = 0M;
        characterDataControl.NumericUpDown_Hair.Maximum = 255M;
        characterDataControl.NumericUpDown_Hair.Minimum = 0M;
        characterDataControl.NumericUpDown_HairColor.Maximum = 255M;
        characterDataControl.NumericUpDown_HairColor.Minimum = 0M;
        characterDataControl.NumericUpDown_Face.Maximum = 255M;
        characterDataControl.NumericUpDown_Face.Minimum = 0M;
        characterDataControl.NumericUpDown_SkinColor.Maximum = 7M;
        characterDataControl.NumericUpDown_SkinColor.Minimum = 0M;
        characterDataControl.NumericUpDown_EyeColor.Maximum = 15M;
        characterDataControl.NumericUpDown_EyeColor.Minimum = 0M;
        // SkillPoint : DataValue<ushort> 0..9999
        characterDataControl.NumericUpDown_SkillPoint.Maximum = 9999M;
        characterDataControl.NumericUpDown_SkillPoint.Minimum = 0M;
        characterDataControl.EndUpdate();
    }

    private static void ApplyRikkaDataRanges()
    {
        var rikkaInnDataControl = MainWindow.Instance.RikkaInnDataControl;
        rikkaInnDataControl.BeginUpdate();
        rikkaInnDataControl.NumericUpDownGrade.Maximum = 6M;
        rikkaInnDataControl.NumericUpDownGrade.Minimum = 1M;
        rikkaInnDataControl.NumericUpDownVisitorCount.Maximum = 2147483647M;
        rikkaInnDataControl.NumericUpDownVisitorCount.Minimum = 0M;
        rikkaInnDataControl.EndUpdate();
    }
}
