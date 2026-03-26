using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LocalAiSearch;

/// <summary>
/// Application entry point.
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
        MainWindow.Content = rootFrame;
        MainWindow.Activate();

        rootFrame.Navigate(typeof(MainPage), args.Arguments);
    }
}
