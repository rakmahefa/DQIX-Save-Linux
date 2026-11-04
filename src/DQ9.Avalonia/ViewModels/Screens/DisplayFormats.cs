using System;
using System.Collections.Generic;
using DQ9_Cheat.DataManager;
using DQ9_Cheat.MapSearch;

namespace DQ9.Avalonia.ViewModels.Screens;

/// <summary>
/// UX-14: display formatters at the ViewModel boundary so raw engine enums
/// (BeforeOrder, NotAppear, NotDiscover, AndOver…) never reach the UI. Lists
/// and combos both go through these labels.
/// </summary>
public static class DisplayFormats
{
    public static string ToDisplay(this QuestState state) => state switch
    {
        QuestState.NotRecieved => "Not received",
        QuestState.BeforeOrder => "On the notice board",
        QuestState.AllowOrder => "Available to take",
        QuestState.Order => "Accepted",
        QuestState.Clear => "Cleared",
        _ => state.ToString(),
    };

    public static string ToDisplay(this AlchemyState state) => state switch
    {
        AlchemyState.NotAppear => "Recipe not found",
        AlchemyState.Appear => "Recipe known",
        AlchemyState.Created => "Created",
        _ => state.ToString(),
    };

    public static string ToDisplay(this MapState state) => state switch
    {
        MapState.Unknown => "Unknown",
        MapState.NotDiscover => "Not discovered",
        MapState.Discover => "Discovered",
        MapState.Clear => "Cleared",
        _ => state.ToString(),
    };

    public static string ToDisplay(this MapType type) => type switch
    {
        MapType.Unknown => "Unknown",
        MapType.Normal => "Normal",
        MapType.devil => "Devil grotto",
        _ => type.ToString(),
    };

    public static string ToDisplay(this SearchConditionType condition) => condition switch
    {
        SearchConditionType.Accord => "Exactly (=)",
        SearchConditionType.Discord => "Excludes (≠)",
        SearchConditionType.AndOver => "At least (≥)",
        SearchConditionType.AndLess => "At most (≤)",
        _ => condition.ToString(),
    };
}

/// <summary>A combo/list row that wraps an engine value with a user-facing
/// label (UX-14 keeps engine enums out of the rendered text).</summary>
public sealed class EnumOption<T> where T : notnull
{
    public EnumOption(T value, string label)
    {
        Value = value;
        Label = label;
    }

    public T Value { get; }

    public string Label { get; }

    public override string ToString() => Label;
}
