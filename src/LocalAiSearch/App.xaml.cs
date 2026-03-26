using Microsoft.UI.Xaml;

namespace LocalAiSearch;

/// <summary>
/// Application entry point.
/// </summary>
public partial class App : Application
{
    protected Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new Window { Title = "Local AI Search" };
        MainWindow.Content = new MainPage();
        MainWindow.Activate();
    }
}
