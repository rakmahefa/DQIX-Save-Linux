using DQ9_Cheat.DataManager;

namespace DQ9.Avalonia.ViewModels.Screens;

/// <summary>True when a save block is loaded in memory; rows read the engine
/// lazily during construction, which happens before any file is opened.</summary>
internal static class EngineGuard
{
    public static bool Ready => SaveDataManager.Instance.SaveData.Data != null;
}
