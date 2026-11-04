using System;
using System.Collections.ObjectModel;
using System.Linq;
using DQ9_Cheat.DataManager;
using DQ9_Cheat.GameData;

namespace DQ9.Avalonia.ViewModels.Screens;

/// <summary>
/// Character editor (WinForms CharacterDataControl parity): the 13 character
/// slots, name / sex / job, HP-MP, skill point, job levels, strategy, status
/// flags, equipment slots and appearance basics.
/// </summary>
public sealed class CharactersScreenViewModel : EditorScreenViewModel
{
    private CharacterSlotRow? _selectedCharacter;
    private string _name = "";
    private byte _sex;
    private JobData? _selectedJob;
    private string _nowHp = "0";
    private string _maxHp = "0";
    private string _nowMp = "0";
    private string _maxMp = "0";
    private string _skillPoint = "0";
    private Strategy? _selectedStrategy;
    private bool _die;
    private bool _poison;
    private bool _curse;
    private string _faceType = "0";
    private string _hairType = "0";
    private string _hairColor = "0";
    private string _skinColor = "0";
    private string _eyeColor = "0";
    private string _figureWidth = "0";
    private string _figureHeight = "0";
    private ItemOption? _weapon;
    private ItemOption? _shield;
    private ItemOption? _head;
    private ItemOption? _upperBody;
    private ItemOption? _arm;
    private ItemOption? _lowerBody;
    private ItemOption? _shoe;
    private ItemOption? _accessory;
    private JobData? _selectedEditJob;
    private string _transmigration = "0";
    private SkillData? _selectedSkill;
    private bool _learnRuler;
    private bool _learnCheer;

    public CharactersScreenViewModel()
    {
        Characters = new ObservableCollection<CharacterSlotRow>(
            Enumerable.Range(0, CharacterManager.CharacterMax)
                .Select(i => new CharacterSlotRow(i)));
        Jobs = new ObservableCollection<JobData>(JobDataList.List);
        Strategies = new ObservableCollection<Strategy>(StrategyList.List);
        Weapons = ItemOptions(ItemType.Weapon);
        Shields = ItemOptions(ItemType.Shield);
        Heads = ItemOptions(ItemType.Head);
        UpperBodies = ItemOptions(ItemType.UpperBody);
        Arms = ItemOptions(ItemType.Arm);
        LowerBodies = ItemOptions(ItemType.LowerBody);
        Shoes = ItemOptions(ItemType.Shoe);
        Accessories = ItemOptions(ItemType.Accessory);
        SelectedCharacter = Characters.FirstOrDefault();
        SelectedJob = Jobs.FirstOrDefault();
        SelectedStrategy = Strategies.FirstOrDefault();
        SelectedEditJob = Jobs.FirstOrDefault();
        Skills = new ObservableCollection<SkillData>(SkillDataList.List);
        SelectedSkill = Skills.FirstOrDefault();
    }

    public ObservableCollection<CharacterSlotRow> Characters { get; }
    public ObservableCollection<JobData> Jobs { get; }
    public ObservableCollection<Strategy> Strategies { get; }
    public ObservableCollection<ItemOption> Weapons { get; }
    public ObservableCollection<ItemOption> Shields { get; }
    public ObservableCollection<ItemOption> Heads { get; }
    public ObservableCollection<ItemOption> UpperBodies { get; }
    public ObservableCollection<ItemOption> Arms { get; }
    public ObservableCollection<ItemOption> LowerBodies { get; }
    public ObservableCollection<ItemOption> Shoes { get; }
    public ObservableCollection<ItemOption> Accessories { get; }

    /// <summary>Level + experience of every job of the selected character,
    /// one row per vocation, addressed through the job's DataIndex (the
    /// WinForms status-edit group also showed both fields per job).</summary>
    public ObservableCollection<JobLevelRow> JobLevels { get; } = new();

    /// <summary>All skill / specialty tables (WinForms comboBox_SkillSpecialty).</summary>
    public ObservableCollection<SkillData> Skills { get; }

    /// <summary>Seed values (revise) of the selected edit job — WinForms groupBox_StatusRevise.</summary>
    public ObservableCollection<NumRow> ReviseRows { get; } = new();

    /// <summary>Skill levels of the selected edit job — WinForms groupBox_Skill.</summary>
    public ObservableCollection<NumRow> SkillLevelRows { get; } = new();

    /// <summary>Specialty bits of the selected skill — WinForms checkedListBox_Specialty.</summary>
    public ObservableCollection<CheckRow> SkillSpecialtyRows { get; } = new();

    /// <summary>Effect bits of the selected skill — WinForms checkedListBox_Skill.</summary>
    public ObservableCollection<CheckRow> SkillEffectRows { get; } = new();

    /// <summary>The job whose seeds / skill levels are edited below (WinForms
    /// "Vocation to Edit"); independent of the character's own job, like the
    /// original client.</summary>
    public JobData? SelectedEditJob
    {
        get => _selectedEditJob;
        set
        {
            if (!Set(ref _selectedEditJob, value))
                return;
            RebuildJobStatusRows();
        }
    }

    /// <summary>Revocations (0-10) of the selected edit job.</summary>
    public string Transmigration
    {
        get => _transmigration;
        set
        {
            if (!Set(ref _transmigration, value) || Current == null || SelectedEditJob == null || IsRefreshing)
                return;
            if (!ValidateParse<byte>(nameof(Transmigration), "Revocations", value, byte.TryParse,
                    "a whole number between 0 and 10", out var v))
                return;
            if (EngineGuard.Ready)
                Current.SetTransmigration(SelectedEditJob.DataIndex, Math.Min(v, (byte)10));
        }
    }

    public SkillData? SelectedSkill
    {
        get => _selectedSkill;
        set
        {
            if (!Set(ref _selectedSkill, value))
                return;
            RebuildSkillBitRows();
        }
    }

    /// <summary>"Zoom" specialty of the WinForms GroupBox_SkillSpecialty.</summary>
    public bool LearnRuler
    {
        get => _learnRuler;
        set
        {
            if (Set(ref _learnRuler, value) && Current != null && !IsRefreshing && EngineGuard.Ready)
                Current.SetLearnRular(value);
        }
    }

    /// <summary>"Egg On" specialty of the WinForms GroupBox_SkillSpecialty.</summary>
    public bool LearnCheer
    {
        get => _learnCheer;
        set
        {
            if (Set(ref _learnCheer, value) && Current != null && !IsRefreshing && EngineGuard.Ready)
                Current.SetLearnCheer(value);
        }
    }

    private void RebuildJobStatusRows()
    {
        ReviseRows.Clear();
        SkillLevelRows.Clear();
        var job = SelectedEditJob;
        if (job == null)
            return;
        var dataIndex = job.DataIndex;

        void AddRevise(string label, Func<CharacterData, ushort> get, Action<CharacterData, ushort> set)
        {
            ReviseRows.Add(new NumRow(label,
                () => EngineGuard.Ready && Current != null ? get(Current) : 0u,
                v =>
                {
                    if (Current != null && EngineGuard.Ready)
                        set(Current, (ushort)Math.Min(v, 999));
                }));
        }

        AddRevise("Max HP", c => c.GetReviseMaxHP(dataIndex), (c, v) => c.SetReviseMaxHP(dataIndex, v));
        AddRevise("Max MP", c => c.GetReviseMaxMP(dataIndex), (c, v) => c.SetReviseMaxMP(dataIndex, v));
        AddRevise("Strength", c => c.GetRevisePower(dataIndex), (c, v) => c.SetRevisePower(dataIndex, v));
        AddRevise("Agility", c => c.GetReviseQuick(dataIndex), (c, v) => c.SetReviseQuick(dataIndex, v));
        AddRevise("Resilience", c => c.GetReviseDefense(dataIndex), (c, v) => c.SetReviseDefense(dataIndex, v));
        AddRevise("Deftness", c => c.GetReviseFacility(dataIndex), (c, v) => c.SetReviseFacility(dataIndex, v));
        AddRevise("Magical Might", c => c.GetReviseSmart(dataIndex), (c, v) => c.SetReviseSmart(dataIndex, v));
        AddRevise("Magical Mend", c => c.GetReviseRevivalMagic(dataIndex), (c, v) => c.SetReviseRevivalMagic(dataIndex, v));
        AddRevise("Charm", c => c.GetReviseAttackMagic(dataIndex), (c, v) => c.SetReviseAttackMagic(dataIndex, v));

        if (job.SkillList == null)
        {
            // some jobs (e.g. Guardian) have no skill table; the WinForms
            // groupBox_Skill is disabled in that case
            Transmigration = EngineGuard.Ready && Current != null
                ? Current.GetTransmigration(dataIndex).ToString()
                : "0";
            return;
        }

        foreach (var skill in job.SkillList)
        {
            var skillIndex = skill.Index;
            SkillLevelRows.Add(new NumRow(skill.Name,
                () => EngineGuard.Ready && Current != null ? Current.JobSkillLevel[skillIndex].Value : 0u,
                v =>
                {
                    if (Current != null && EngineGuard.Ready)
                        Current.JobSkillLevel[skillIndex].Value = (byte)Math.Min(v, 100);
                }));
        }

        foreach (var row in ReviseRows)
            row.Reload();
        foreach (var row in SkillLevelRows)
            row.Reload();
        Transmigration = EngineGuard.Ready && Current != null
            ? Current.GetTransmigration(dataIndex).ToString()
            : "0";
    }

    private void RebuildSkillBitRows()
    {
        SkillSpecialtyRows.Clear();
        SkillEffectRows.Clear();
        var skill = SelectedSkill;
        if (skill == null)
            return;

        CheckRow MakeRow(SkillSpecialtyEffectData effect)
        {
            return new CheckRow(effect.Name,
                () => EngineGuard.Ready && Current != null && Current.IsLearnSkillSpecialtyEffect(effect.Index),
                v =>
                {
                    if (Current != null && EngineGuard.Ready)
                        Current.SetLearnSkillSpecialtyEffect(effect.Index, v);
                });
        }

        foreach (var effect in skill.SkillSpecialtyList)
            SkillSpecialtyRows.Add(MakeRow(effect));
        foreach (var effect in skill.SkillEffectList)
            SkillEffectRows.Add(MakeRow(effect));
        foreach (var row in SkillSpecialtyRows)
            row.Reload();
        foreach (var row in SkillEffectRows)
            row.Reload();
    }

    private static ObservableCollection<ItemOption> ItemOptions(ItemType type)
    {
        var list = new ObservableCollection<ItemOption> { ItemOption.None };
        foreach (var item in ItemDataList.GetList(type, ""))
            list.Add(new ItemOption(item));
        return list;
    }

    public CharacterSlotRow? SelectedCharacter
    {
        get => _selectedCharacter;
        set
        {
            if (!Set(ref _selectedCharacter, value))
                return;
            if (value != null)
                LoadCharacter((int)value.Index);
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (Set(ref _name, value) && Current != null && !IsRefreshing && EngineGuard.Ready && value.Length <= Current.Name.MaxLength)
                Current.Name.Value = value;
        }
    }

    public byte Sex
    {
        get => _sex;
        set { if (Set(ref _sex, value) && Current != null && !IsRefreshing && EngineGuard.Ready) Current.Sex = value; }
    }

    public JobData? SelectedJob
    {
        get => _selectedJob;
        set
        {
            if (!Set(ref _selectedJob, value))
                return;
            if (Current != null && value != null && !IsRefreshing && EngineGuard.Ready && value.DataIndex < byte.MaxValue)
                Current.Job.Value = (byte)value.DataIndex;
        }
    }

    public string NowHp
    {
        get => _nowHp;
        set
        {
            if (!Set(ref _nowHp, value) || Current == null || IsRefreshing)
                return;
            if (!ValidateParse<ushort>(nameof(NowHp), "Current HP", value, ushort.TryParse,
                    "a whole number between 0 and 65 535", out var v))
                return;
            if (EngineGuard.Ready)
                Current.NowHP = v;
        }
    }

    public string MaxHp
    {
        get => _maxHp;
        set
        {
            if (!Set(ref _maxHp, value) || Current == null || IsRefreshing)
                return;
            if (!ValidateParse<ushort>(nameof(MaxHp), "Max HP", value, ushort.TryParse,
                    "a whole number between 0 and 65 535", out var v))
                return;
            if (EngineGuard.Ready)
                Current.MaxHP = v;
        }
    }

    public string NowMp
    {
        get => _nowMp;
        set
        {
            if (!Set(ref _nowMp, value) || Current == null || IsRefreshing)
                return;
            if (!ValidateParse<ushort>(nameof(NowMp), "Current MP", value, ushort.TryParse,
                    "a whole number between 0 and 65 535", out var v))
                return;
            if (EngineGuard.Ready)
                Current.NowMP = v;
        }
    }

    public string MaxMp
    {
        get => _maxMp;
        set
        {
            if (!Set(ref _maxMp, value) || Current == null || IsRefreshing)
                return;
            if (!ValidateParse<ushort>(nameof(MaxMp), "Max MP", value, ushort.TryParse,
                    "a whole number between 0 and 65 535", out var v))
                return;
            if (EngineGuard.Ready)
                Current.MaxMP = v;
        }
    }

    public string SkillPoint
    {
        get => _skillPoint;
        set
        {
            if (!Set(ref _skillPoint, value) || Current == null || IsRefreshing)
                return;
            if (!ValidateParse<ushort>(nameof(SkillPoint), "Skill points", value, ushort.TryParse,
                    "a whole number between 0 and 65 535", out var v))
                return;
            if (EngineGuard.Ready)
                Current.SkillPoint.Value = v;
        }
    }

    public Strategy? SelectedStrategy
    {
        get => _selectedStrategy;
        set { if (Set(ref _selectedStrategy, value) && Current != null && value != null && !IsRefreshing && EngineGuard.Ready) Current.Strategy = value; }
    }

    public bool Die
    {
        get => _die;
        set { if (Set(ref _die, value) && Current != null && !IsRefreshing && EngineGuard.Ready) Current.Die = value; }
    }

    public bool Poison
    {
        get => _poison;
        set { if (Set(ref _poison, value) && Current != null && !IsRefreshing && EngineGuard.Ready) Current.Poison = value; }
    }

    public bool Curse
    {
        get => _curse;
        set { if (Set(ref _curse, value) && Current != null && !IsRefreshing && EngineGuard.Ready) Current.Curse = value; }
    }

    public string FaceType
    {
        get => _faceType;
        set { if (Set(ref _faceType, value) && Current != null && !IsRefreshing && EngineGuard.Ready && byte.TryParse(value, out var v)) Current.FaceType.Value = v; }
    }

    public string HairType
    {
        get => _hairType;
        set { if (Set(ref _hairType, value) && Current != null && !IsRefreshing && EngineGuard.Ready && byte.TryParse(value, out var v)) Current.HairType.Value = v; }
    }

    public string HairColor
    {
        get => _hairColor;
        set { if (Set(ref _hairColor, value) && Current != null && !IsRefreshing && EngineGuard.Ready && byte.TryParse(value, out var v)) Current.HairColor.Value = v; }
    }

    public string SkinColor
    {
        get => _skinColor;
        set { if (Set(ref _skinColor, value) && Current != null && !IsRefreshing && EngineGuard.Ready && byte.TryParse(value, out var v)) Current.SkinColor = v; }
    }

    public string EyeColor
    {
        get => _eyeColor;
        set { if (Set(ref _eyeColor, value) && Current != null && !IsRefreshing && EngineGuard.Ready && byte.TryParse(value, out var v)) Current.EyeColor = v; }
    }

    public string FigureWidth
    {
        get => _figureWidth;
        set { if (Set(ref _figureWidth, value) && Current != null && !IsRefreshing && EngineGuard.Ready && ushort.TryParse(value, out var v)) Current.FigureWidth.Value = v; }
    }

    public string FigureHeight
    {
        get => _figureHeight;
        set { if (Set(ref _figureHeight, value) && Current != null && !IsRefreshing && EngineGuard.Ready && ushort.TryParse(value, out var v)) Current.FigureHeight.Value = v; }
    }

    public ItemOption? Weapon
    {
        get => _weapon;
        set { if (Set(ref _weapon, value) && Current != null && !IsRefreshing && EngineGuard.Ready) Current.EquipmentWeapon = value?.Item; }
    }

    public ItemOption? Shield
    {
        get => _shield;
        set { if (Set(ref _shield, value) && Current != null && !IsRefreshing && EngineGuard.Ready) Current.EquipmentShield = value?.Item; }
    }

    public ItemOption? Head
    {
        get => _head;
        set { if (Set(ref _head, value) && Current != null && !IsRefreshing && EngineGuard.Ready) Current.EquipmentHead = value?.Item; }
    }

    public ItemOption? UpperBody
    {
        get => _upperBody;
        set { if (Set(ref _upperBody, value) && Current != null && !IsRefreshing && EngineGuard.Ready) Current.EquipmentUpperBody = value?.Item; }
    }

    public ItemOption? Arm
    {
        get => _arm;
        set { if (Set(ref _arm, value) && Current != null && !IsRefreshing && EngineGuard.Ready) Current.EquipmentArm = value?.Item; }
    }

    public ItemOption? LowerBody
    {
        get => _lowerBody;
        set { if (Set(ref _lowerBody, value) && Current != null && !IsRefreshing && EngineGuard.Ready) Current.EquipmentLowerBody = value?.Item; }
    }

    public ItemOption? Shoe
    {
        get => _shoe;
        set { if (Set(ref _shoe, value) && Current != null && !IsRefreshing && EngineGuard.Ready) Current.EquipmentShoe = value?.Item; }
    }

    public ItemOption? Accessory
    {
        get => _accessory;
        set { if (Set(ref _accessory, value) && Current != null && !IsRefreshing && EngineGuard.Ready) Current.EquipmentAccessory = value?.Item; }
    }

    private CharacterData? Current => SelectedCharacter == null
        ? null
        : Data.CharacterManager.CharacterData[(int)SelectedCharacter.Index];

    private void LoadCharacter(int index)
    {
        SuppressWrites(() =>
        {
            if (!EngineGuard.Ready)
                return;
            var character = Data.CharacterManager.CharacterData[index];
            Name = character.Name.Value;
            Sex = character.Sex;
            NowHp = character.NowHP.ToString();
            MaxHp = character.MaxHP.ToString();
            NowMp = character.NowMP.ToString();
            MaxMp = character.MaxMP.ToString();
            SkillPoint = character.SkillPoint.Value.ToString();
            Die = character.Die;
            Poison = character.Poison;
            Curse = character.Curse;
            FaceType = character.FaceType.Value.ToString();
            HairType = character.HairType.Value.ToString();
            HairColor = character.HairColor.Value.ToString();
            SkinColor = character.SkinColor.ToString();
            EyeColor = character.EyeColor.ToString();
            FigureWidth = character.FigureWidth.Value.ToString();
            FigureHeight = character.FigureHeight.Value.ToString();
            // the engine stores the job's DataIndex, not the list position
            // (list order ≠ DataIndex order for Paladin/Armamentalist/Ranger/
            // Sage/Luminary) — resolve the option by DataIndex like WinForms.
            var jobValue = character.Job.Value;
            _selectedJob = Jobs.FirstOrDefault(j => j.DataIndex == jobValue) ?? Jobs.FirstOrDefault();
            Raise(nameof(SelectedJob));
            var strategy = character.Strategy;
            _selectedStrategy = strategy != null
                ? Strategies.FirstOrDefault(s => s.Value == strategy.Value)
                : Strategies.FirstOrDefault();
            Raise(nameof(SelectedStrategy));
            _weapon = FindOption(Weapons, character.EquipmentWeapon);
            _shield = FindOption(Shields, character.EquipmentShield);
            _head = FindOption(Heads, character.EquipmentHead);
            _upperBody = FindOption(UpperBodies, character.EquipmentUpperBody);
            _arm = FindOption(Arms, character.EquipmentArm);
            _lowerBody = FindOption(LowerBodies, character.EquipmentLowerBody);
            _shoe = FindOption(Shoes, character.EquipmentShoe);
            _accessory = FindOption(Accessories, character.EquipmentAccessory);
            Raise(nameof(Weapon));
            Raise(nameof(Shield));
            Raise(nameof(Head));
            Raise(nameof(UpperBody));
            Raise(nameof(Arm));
            Raise(nameof(LowerBody));
            Raise(nameof(Shoe));
            Raise(nameof(Accessory));
            // one row per job, keyed by the job's DataIndex: JobLevel and
            // JobExperience are saved in DataIndex order, which differs from
            // the job list order for Paladin (9), Armamentalist (8), Ranger
            // (12), Sage (10) and Luminary (11).
            JobLevels.Clear();
            foreach (var job in Jobs)
            {
                var dataIndex = job.DataIndex;
                if (dataIndex >= character.JobLevel.Length)
                    continue;
                JobLevels.Add(new JobLevelRow(job.Name,
                    new NumRow(job.Name,
                        () => Data.CharacterManager.CharacterData[index].JobLevel[dataIndex].Value,
                        v => Data.CharacterManager.CharacterData[index].JobLevel[dataIndex].Value =
                            (byte)Math.Min(v, 99)),
                    new NumRow(job.Name,
                        () => Data.CharacterManager.CharacterData[index].JobExperience[dataIndex].Value,
                        v => Data.CharacterManager.CharacterData[index].JobExperience[dataIndex].Value =
                            Math.Min(v, 999999999u))));
            }
            foreach (var row in JobLevels)
                row.Reload();
        });
    }

    /// <summary>Resolves an equipped item into a combo option; null keeps the
    /// engine value untouched when the item is not part of the option list.</summary>
    private static ItemOption? FindOption(ObservableCollection<ItemOption> options, ItemDataBase? item)
    {
        if (item == null)
            return options[0];
        return options.FirstOrDefault(o => o.Item != null && o.Item.Value == item.Value);
    }

    /// <inheritdoc />
    public override string Header => "Characters";

    protected override void OnRefresh()
    {
        foreach (var character in Characters)
            character.Reload();
        if (SelectedCharacter != null)
            LoadCharacter((int)SelectedCharacter.Index);
        RebuildJobStatusRows();
        RebuildSkillBitRows();
        var current = Current;
        LearnRuler = current != null && current.IsLearnRular();
        LearnCheer = current != null && current.IsLearnCheer();
    }
}

/// <summary>One selectable character slot in the list.</summary>
public sealed class CharacterSlotRow : ObservableObject
{
    private string _display;

    public CharacterSlotRow(int index)
    {
        Index = (uint)index;
        _display = BuildDisplay(index);
    }

    public uint Index { get; }

    public string Display
    {
        get => _display;
        private set => Set(ref _display, value);
    }

    public void Reload() => Display = BuildDisplay((int)Index);

    private static string BuildDisplay(int index)
    {
        if (!EngineGuard.Ready)
            return $"#{index + 1}: (no file)";
        var manager = SaveDataManager.Instance.SaveData.CharacterManager;
        var character = manager.CharacterData[index];
        var isParty = index < manager.PartyCharacterCount;
        var name = character.Name.Value;
        if (string.IsNullOrEmpty(name))
            name = "(empty)";
        // Job.Value is a DataIndex — the list order differs for five jobs.
        var job = JobDataList.GetJobData(character.Job.Value)?.Name ?? "?";
        return $"#{index + 1}: {name} - {job}{(isParty ? " [party]" : "")}";
    }
}
