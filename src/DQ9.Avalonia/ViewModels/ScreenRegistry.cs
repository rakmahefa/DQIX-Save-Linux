using System.Collections.Generic;
using DQ9.Avalonia.ViewModels.Screens;

namespace DQ9.Avalonia.ViewModels;

/// <summary>
/// Owns the P4 business screens, exposes them for the TabControl and
/// re-broadcasts engine refreshes to every screen.
/// </summary>
public sealed class ScreenRegistry : ObservableObject
{
    public ScreenRegistry()
    {
        General = new GeneralScreenViewModel();
        Characters = new CharactersScreenViewModel();
        Items = new ItemsScreenViewModel();
        Quests = new QuestsScreenViewModel();
        Titles = new TitlesScreenViewModel();
        Monsters = new MonstersScreenViewModel();
        Flags = new FlagsScreenViewModel();
        Collections = new CollectionsScreenViewModel();
        Inn = new InnScreenViewModel();
        Wifi = new WifiScreenViewModel();
        TreasureMaps = new TreasureMapsScreenViewModel();
        Search = new SearchScreenViewModel();
        Screens = new List<EditorScreenViewModel>
        {
            General,
            Characters,
            Items,
            Quests,
            Titles,
            Monsters,
            Flags,
            Collections,
            Inn,
            Wifi,
            TreasureMaps,
            Search,
        };
    }

    public GeneralScreenViewModel General { get; }
    public CharactersScreenViewModel Characters { get; }
    public ItemsScreenViewModel Items { get; }
    public QuestsScreenViewModel Quests { get; }
    public TitlesScreenViewModel Titles { get; }
    public MonstersScreenViewModel Monsters { get; }
    public FlagsScreenViewModel Flags { get; }
    public CollectionsScreenViewModel Collections { get; }
    public InnScreenViewModel Inn { get; }
    public WifiScreenViewModel Wifi { get; }
    public TreasureMapsScreenViewModel TreasureMaps { get; }
    public SearchScreenViewModel Search { get; }

    public IReadOnlyList<EditorScreenViewModel> Screens { get; }

    public void RefreshAll()
    {
        foreach (var screen in Screens)
            screen.Refresh();
    }
}
