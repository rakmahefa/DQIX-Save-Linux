using Avalonia;
using Avalonia.Markup.Xaml;

namespace DQ9.Avalonia;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
