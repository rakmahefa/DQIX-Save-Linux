using System.Collections.ObjectModel;
using System.Linq;
using DQ9_Cheat.GameData;

namespace DQ9.Avalonia.ViewModels.Screens;

/// <summary>
/// Progression flags (WinForms ProcessFlagControl parity): ruler switch,
/// change-job allowances for the six special jobs, and the destination flags.
/// </summary>
public sealed class FlagsScreenViewModel : EditorScreenViewModel
{
    private bool _disableRuler;

    public FlagsScreenViewModel()
    {
        AllowChangeJob = new ObservableCollection<CheckRow>(
            Enumerable.Range(0, 6).Select(i => new CheckRow(
                JobDataList.List[i + 7].Name,
                () => Data.ProcessFlag.IsAllowChangeJob(i),
                v => Data.ProcessFlag.SetAllowChangeJob(i, v))));
        Destinations = new ObservableCollection<CheckRow>(
            DestinationList.List.Select(d => new CheckRow(
                d.Name,
                () => Data.ProcessFlag.IsAllowDestination(d.Index),
                v => Data.ProcessFlag.SetAllowDestination(d.Index, v))));
    }

    public ObservableCollection<CheckRow> AllowChangeJob { get; }

    public ObservableCollection<CheckRow> Destinations { get; }

    public bool DisableRuler
    {
        get => _disableRuler;
        set
        {
            if (Set(ref _disableRuler, value) && !IsRefreshing && EngineGuard.Ready)
                Data.ProcessFlag.DisableRuler = value;
        }
    }

    /// <inheritdoc />
    public override string Header => "Flags";

    protected override void OnRefresh()
    {
        DisableRuler = Data.ProcessFlag.DisableRuler;
        foreach (var row in AllowChangeJob)
            row.Reload();
        foreach (var row in Destinations)
            row.Reload();
    }
}
