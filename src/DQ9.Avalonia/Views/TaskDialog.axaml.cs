using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace DQ9.Avalonia.Views;

/// <summary>
/// Small modal task dialog (Avalonia ships no MessageBox). Used for the
/// unsaved-changes guard (UX-04) and the destructive / bulk-action
/// confirmations (UX-16). No new packages; ~60 lines like the plan suggests.
/// </summary>
public partial class TaskDialog : Window
{
    /// <summary>One button of the dialog; <see cref="IsCancel"/> marks the
    /// button that also reacts to Escape and is the safe default focus.</summary>
    public sealed class Choice
    {
        public Choice(string label, object result, bool isDefault = false, bool isCancel = false, bool danger = false)
        {
            Label = label;
            Result = result;
            IsDefault = isDefault;
            IsCancel = isCancel;
            Danger = danger;
        }

        public string Label { get; }
        public object Result { get; }
        public bool IsDefault { get; }
        public bool IsCancel { get; }
        public bool Danger { get; }
    }

    private readonly IReadOnlyList<Choice> _choices;
    private object? _result;

    /// <summary>XAML compiler requires a public parameterless constructor;
    /// real instantiation goes through <see cref="ShowAsync"/>.</summary>
    public TaskDialog()
    {
        InitializeComponent();
        _choices = [];
    }

    private TaskDialog(string title, string message, IReadOnlyList<Choice> choices)
    {
        InitializeComponent();
        Title = title;
        DataContext = new { Title = title, Message = message };
        _choices = choices;
        foreach (var choice in choices)
        {
            var button = new Button
            {
                Content = choice.Label,
                MinWidth = 88,
                MinHeight = 30,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                IsDefault = choice.IsDefault,
                IsCancel = choice.IsCancel,
            };
            if (choice.Danger)
                button.Classes.Add("danger");
            button.Click += (_, _) =>
            {
                _result = choice.Result;
                Close();
            };
            ButtonsPanel.Children.Add(button);
        }
    }

    /// <summary>Shows the dialog modally and returns the chosen result, or
    /// null when the window was closed through the OS close button.</summary>
    public static async Task<object?> ShowAsync(Window owner, string title, string message,
        params Choice[] choices)
    {
        var dialog = new TaskDialog(title, message, choices);
        await dialog.ShowDialog(owner);
        return dialog._result;
    }
}
