using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LocalAiSearch;

/// <summary>
/// Application entry point. Detects system dark/light preference on startup.
/// </summary>
public partial class App : Application
{
    protected Window? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new Window { Title = "Local AI Search" };

        var rootFrame = new Frame();

        // Apply system theme to the root frame so ThemeDictionary resources resolve correctly.
        // Explicit ElementTheme is required on GTK Skia to prevent silent resource lookup failures.
        rootFrame.RequestedTheme = RequestedTheme == ApplicationTheme.Dark
            ? ElementTheme.Dark
            : ElementTheme.Light;

        MainWindow.Content = rootFrame;
        MainWindow.Activate();

        rootFrame.Navigate(typeof(MainPage), args.Arguments);
    }
}
